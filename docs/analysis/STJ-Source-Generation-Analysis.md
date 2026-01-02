# System.Text.Json Source Generation Analysis for RemoteFactory

## Executive Summary

This document analyzes the feasibility of eliminating reflection from RemoteFactory's JSON serialization by incorporating System.Text.Json (STJ) source generation. The analysis identifies all reflection points, evaluates STJ source generation compatibility, proposes an architecture, identifies challenges, and estimates complexity.

**Key Finding**: Partial elimination of reflection is achievable and provides meaningful AOT improvements, but **complete elimination is not feasible** due to fundamental architectural constraints around interface polymorphism and dynamic type discovery.

---

## 1. Current Reflection Points Inventory

### 1.1 Type Discovery and Resolution

| File | Line | Reflection API | Purpose |
|------|------|----------------|---------|
| `ServiceAssemblies.cs` | 29 | `assembly.GetTypes()` | Discover all types in registered assemblies |
| `ServiceAssemblies.cs` | 62 | `Type.GetType(fullName)` | Find type by full name (fallback) |
| `NeatooJsonSerializer.cs` | 201 | `serviceAssemblies.FindType()` | Resolve delegate type from string name |
| `NeatooJsonSerializer.cs` | 235 | `serviceAssemblies.FindType()` | Resolve object type from `ObjectJson.AssemblyType` |
| `NeatooInterfaceJsonTypeConverter.cs` | 68 | `serviceAssemblies.FindType(typeName)` | Resolve concrete type from `$type` discriminator |

**Assessment**: These reflection points are fundamental to the polymorphic deserialization model. STJ source generation cannot help here because type names are only known at runtime from the JSON payload.

### 1.2 Generic Type Construction

| File | Line | Reflection API | Purpose |
|------|------|----------------|---------|
| `NeatooOrdinalConverterFactory.cs` | 34 | `typeof(NeatooOrdinalConverter<>).MakeGenericType(typeToConvert)` | Create typed ordinal converter |
| `NeatooInterfaceJsonConverterFactory.cs` | 37 | `typeof(NeatooInterfaceJsonTypeConverter<>).MakeGenericType(typeToConvert)` | Create typed interface converter |
| `HandleRemoteDelegateRequest.cs` | 43 | `returnType.GetGenericTypeDefinition()` | Check if return type is `Task<T>` |
| `HandleRemoteDelegateRequest.cs` | 45 | `returnType.GetGenericArguments()` | Extract `T` from `Task<T>` |
| `NeatooReferenceResolver.cs` | 42 | `type.GetGenericTypeDefinition()` | Check if type is `Dictionary<,>` |

**Assessment**: `MakeGenericType` calls are the most problematic for AOT. STJ source generation can eliminate these by pre-generating converters for known types.

### 1.3 Ordinal Converter Metadata Reflection

| File | Line | Reflection API | Purpose |
|------|------|----------------|---------|
| `NeatooOrdinalConverter.cs` | 59-60 | `typeof(T).GetInterfaces()` | Check for `IOrdinalSerializationMetadata` |
| `NeatooOrdinalConverter.cs` | 65 | `typeof(T).GetProperty("PropertyNames", ...)` | Get static property names array |
| `NeatooOrdinalConverter.cs` | 66 | `typeof(T).GetProperty("PropertyTypes", ...)` | Get static property types array |
| `NeatooOrdinalConverter.cs` | 67 | `typeof(T).GetMethod("FromOrdinalArray", ...)` | Get static factory method |
| `NeatooOrdinalConverter.cs` | 71-81 | `propertyX.GetValue(null)` / `method.Invoke()` | Read metadata values |

**Assessment**: This is the primary target for elimination. These reflection calls happen once per type (cached in static constructor), but still cause AOT trimming issues and startup costs.

### 1.4 Object Creation via DI

| File | Line | Reflection API | Purpose |
|------|------|----------------|---------|
| `NeatooOrdinalConverterFactory.cs` | 35 | `Activator.CreateInstance(converterType)` | Create ordinal converter instance |
| `NeatooJsonTypeInfoResolver.cs` | 32-35 | `ServiceProvider.GetRequiredService(type)` | Create objects via DI for deserialization |

**Assessment**: `Activator.CreateInstance` can be eliminated. DI-based object creation is already AOT-friendly when properly configured.

### 1.5 Delegate Invocation

| File | Line | Reflection API | Purpose |
|------|------|----------------|---------|
| `HandleRemoteDelegateRequest.cs` | 28 | `method.DynamicInvoke(...)` | Invoke factory delegate |
| `HandleRemoteDelegateRequest.cs` | 33 | `task.GetType().GetProperty(Result).GetValue(task)` | Extract result from Task |
| `MakeLocalSerializedDelegateRequest.cs` | 43 | `method.DynamicInvoke(...)` | Invoke factory delegate locally |
| `MakeLocalSerializedDelegateRequest.cs` | 48 | `task.GetType().GetProperty(Result).GetValue(task)` | Extract result from Task |
| `HandleRemoteDelegateRequest.cs` | 41 | `method.GetMethodInfo().ReturnType` | Get delegate return type |

**Assessment**: `DynamicInvoke` is used for the remote call dispatch mechanism. This is outside the scope of serialization changes but affects full AOT compatibility.

---

## 2. STJ Source Generation Compatibility Analysis

### 2.1 What STJ Source Generation Provides

STJ source generation (`[JsonSerializable]`) provides:
1. **Compile-time type info generation** - No reflection for property discovery
2. **Pre-compiled converters** - No `MakeGenericType` at runtime
3. **AOT-friendly serialization** - Works with NativeAOT trimming
4. **Faster startup** - No JIT compilation for serialization code

### 2.2 Compatibility Assessment

| Feature | STJ Source Gen Compatible? | Notes |
|---------|---------------------------|-------|
| Ordinal array serialization | **Partial** | Custom converter still needed, but can be generated |
| Named property serialization | **Yes** | Standard STJ behavior |
| `$type`/`$value` polymorphism | **No** | STJ requires known discriminators at compile time |
| `$id`/`$ref` reference handling | **Yes** | `ReferenceHandler.Preserve` works with source gen |
| Circular references | **Yes** | Works with `ReferenceHandler` |
| DI-based object creation | **Yes** | Can use custom `JsonTypeInfo.CreateObject` |
| Custom converters | **Partial** | Must be statically registered |

### 2.3 Critical Incompatibility: Interface Polymorphism

RemoteFactory's `NeatooInterfaceJsonTypeConverter<T>` handles this pattern:

```json
{
  "$type": "MyApp.Domain.Employee",
  "$value": ["John", 42, true]
}
```

**Problem**: The `$type` value is a runtime string that must be resolved to a .NET `Type`. STJ source generation requires all polymorphic types to be known at compile time via `[JsonDerivedType]`:

```csharp
// STJ approach - requires compile-time knowledge
[JsonDerivedType(typeof(Employee), typeDiscriminator: "employee")]
[JsonDerivedType(typeof(Manager), typeDiscriminator: "manager")]
public interface IPerson { }
```

RemoteFactory's approach is fundamentally different - it serializes the full type name and resolves it at runtime. This cannot be made AOT-compatible without changing the wire format.

---

## 3. Proposed Architecture

### 3.1 Phased Approach

Given the constraints, I propose a **phased approach** that progressively eliminates reflection while maintaining backward compatibility:

#### Phase 1: Eliminate Ordinal Converter Reflection (High Impact)

Replace reflection-based metadata access in `NeatooOrdinalConverter<T>` with generated code.

**Current Generated Code** (per `[Factory]` type):
```csharp
// Generated in Person.Ordinal.g.cs
partial class Person : IOrdinalSerializable, IOrdinalSerializationMetadata
{
    public static string[] PropertyNames { get; } = new[] { "Age", "Name" };
    public static Type[] PropertyTypes { get; } = new[] { typeof(int), typeof(string) };

    public object?[] ToOrdinalArray()
    {
        return new object?[] { this.Age, this.Name };
    }

    public static object FromOrdinalArray(object?[] values)
    {
        return new Person
        {
            Age = (int)values[0]!,
            Name = (string)values[1]!
        };
    }
}
```

**Proposed Generated Code** (add typed converter):
```csharp
// Generated in Person.Ordinal.g.cs
partial class Person : IOrdinalSerializable, IOrdinalSerializationMetadata
{
    // Existing members...

    /// <summary>
    /// Pre-compiled ordinal converter - eliminates MakeGenericType reflection.
    /// </summary>
    public static JsonConverter<Person> CreateOrdinalConverter()
    {
        return new PersonOrdinalConverter();
    }
}

// Strongly-typed converter - no reflection needed
internal sealed class PersonOrdinalConverter : JsonConverter<Person>
{
    public override Person? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Expected array, got {reader.TokenType}");

        reader.Read(); // Move past StartArray

        // Read Age (int) - position 0
        var age = JsonSerializer.Deserialize<int>(ref reader, options);
        reader.Read();

        // Read Name (string) - position 1
        var name = JsonSerializer.Deserialize<string>(ref reader, options);
        reader.Read();

        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Too many values in ordinal array");

        return new Person { Age = age, Name = name! };
    }

    public override void Write(Utf8JsonWriter writer, Person value, JsonSerializerOptions options)
    {
        if (value == null) { writer.WriteNullValue(); return; }

        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value.Age, options);
        JsonSerializer.Serialize(writer, value.Name, options);
        writer.WriteEndArray();
    }
}
```

**New Interface for Converter Registration**:
```csharp
// New interface - types implement this to provide their converter
public interface IOrdinalConverterProvider<TSelf> where TSelf : class
{
    static abstract JsonConverter<TSelf> CreateOrdinalConverter();
}
```

**Modified Converter Factory**:
```csharp
public class NeatooOrdinalConverterFactory : JsonConverterFactory
{
    // Cache of known converters - populated at startup
    private static readonly ConcurrentDictionary<Type, JsonConverter> _converterCache = new();

    public static void RegisterConverter<T>(JsonConverter<T> converter) where T : class
    {
        _converterCache.TryAdd(typeof(T), converter);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // Try cache first (AOT path)
        if (_converterCache.TryGetValue(typeToConvert, out var cached))
        {
            return cached;
        }

        // Fallback to reflection (non-AOT path)
        // Check if type has the new provider interface
        var providerInterface = typeToConvert.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IOrdinalConverterProvider<>));

        if (providerInterface != null)
        {
            // Call static abstract method - this still uses some reflection but is AOT-compatible
            var method = typeToConvert.GetMethod("CreateOrdinalConverter", BindingFlags.Public | BindingFlags.Static);
            var converter = (JsonConverter)method!.Invoke(null, null)!;
            _converterCache.TryAdd(typeToConvert, converter);
            return converter;
        }

        // Ultimate fallback for types without generated converters
        var converterType = typeof(NeatooOrdinalConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
```

**Generated Registration Code** (in factory):
```csharp
// Generated in PersonFactory.g.cs
public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
{
    // Existing registrations...

    // Register ordinal converters at startup (AOT-friendly)
    NeatooOrdinalConverterFactory.RegisterConverter(Person.CreateOrdinalConverter());
}
```

#### Phase 2: Generate JsonSerializerContext (Medium Impact)

For types that only need Named format serialization, generate a `JsonSerializerContext`:

```csharp
// Generated in assembly: PersonJsonContext.g.cs
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false)]
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(Employee))]
[JsonSerializable(typeof(RemoteRequestDto))]
[JsonSerializable(typeof(RemoteResponseDto))]
// ... all [Factory] types
internal partial class NeatooJsonContext : JsonSerializerContext
{
}
```

This enables:
- Zero-reflection serialization for known types
- AOT trimming compatibility
- Faster startup

**Challenge**: This must be generated per-assembly since source generators cannot access types from other assemblies.

#### Phase 3: Hybrid Resolver (Low Priority)

Create a `JsonTypeInfoResolver` that chains:
1. Generated `JsonSerializerContext` (AOT-friendly types)
2. Custom ordinal converters (registered statically)
3. Fallback `DefaultJsonTypeInfoResolver` (reflection, non-AOT)

```csharp
public class NeatooHybridTypeInfoResolver : IJsonTypeInfoResolver
{
    private readonly JsonSerializerContext[] _generatedContexts;
    private readonly DefaultJsonTypeInfoResolver _fallback;

    public NeatooHybridTypeInfoResolver(params JsonSerializerContext[] contexts)
    {
        _generatedContexts = contexts;
        _fallback = new DefaultJsonTypeInfoResolver();
    }

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        // Try generated contexts first
        foreach (var ctx in _generatedContexts)
        {
            var info = ctx.GetTypeInfo(type);
            if (info != null) return info;
        }

        // Fallback to reflection-based resolver
        return _fallback.GetTypeInfo(type, options);
    }
}
```

---

## 4. Challenges and Mitigations

### 4.1 Multiple Assemblies with [Factory] Types

**Challenge**: Each assembly with `[Factory]` types needs its own generated `JsonSerializerContext`. These must be combined at runtime.

**Mitigation**:
- Generate a partial `JsonSerializerContext` per assembly
- Generate a registration method that adds the context to a central resolver
- At startup, combine all contexts via `JsonTypeInfoResolver.Combine()`

```csharp
// Generated per assembly
public static class MyAssembly_JsonRegistration
{
    public static void Register(NeatooJsonOptions options)
    {
        options.AddContext(MyAssembly_JsonContext.Default);
    }
}
```

### 4.2 Dynamic Type Discovery at Runtime

**Challenge**: `IServiceAssemblies.FindType(string)` is fundamentally incompatible with AOT because type names come from JSON at runtime.

**Mitigation**:
- **Cannot be fully eliminated** - This is a design constraint of RemoteFactory's polymorphic model
- For AOT scenarios, users must pre-register all possible types:

```csharp
// Pre-registration for AOT
services.AddNeatooRemoteFactory(NeatooFactory.Server, opt =>
{
    opt.RegisterType<Employee>();
    opt.RegisterType<Manager>();
    // All polymorphic types must be registered
});
```

### 4.3 User-Provided Custom Converters

**Challenge**: Users may register custom `JsonConverter` instances that use reflection.

**Mitigation**:
- Document that custom converters must be AOT-compatible for NativeAOT scenarios
- Provide analyzer warning for reflection usage in converters
- Custom converters are user responsibility

### 4.4 Backward Compatibility with Named Format

**Challenge**: Named format serialization falls back to standard STJ which uses reflection.

**Mitigation**:
- Phase 2 addresses this by generating `JsonSerializerContext`
- For types without generated serialization, reflection remains the fallback
- Document that Named format has reduced AOT compatibility

### 4.5 Interface Polymorphism Cannot Be AOT-Compatible

**Challenge**: The `$type`/`$value` pattern fundamentally requires runtime type resolution.

**Mitigation**:
- **No solution** - This is an architectural constraint
- Document limitation clearly
- For full AOT, users must avoid interface-typed properties or pre-register all implementations

---

## 5. Complexity and Phasing Estimates

### Phase 1: Eliminate Ordinal Converter Reflection

**Scope**:
- Modify generator to emit `PersonOrdinalConverter` per type
- Add `IOrdinalConverterProvider<TSelf>` interface
- Modify `NeatooOrdinalConverterFactory` to use registration
- Generate registration calls in factory

**Estimated Effort**: 2-3 days

**Files to Modify**:
- `src/Generator/FactoryGenerator.cs` - Add converter generation
- `src/RemoteFactory/IOrdinalSerializable.cs` - Add new interface
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs` - Add registration

**Benefits**:
- Eliminates `MakeGenericType` for ordinal converters
- Eliminates reflection for property/method access in converters
- Reduces startup time
- Improves AOT trimming

### Phase 2: Generate JsonSerializerContext

**Scope**:
- Generate `JsonSerializerContext` per assembly
- Generate registration code
- Create hybrid type info resolver

**Estimated Effort**: 3-5 days

**Files to Modify**:
- `src/Generator/FactoryGenerator.cs` - Add context generation
- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` - Use hybrid resolver
- `src/RemoteFactory/AddRemoteFactoryServices.cs` - Wire up contexts

**Benefits**:
- Named format becomes AOT-compatible for known types
- Further reduces startup time
- Better trimming support

### Phase 3: Hybrid Resolver and Full Integration

**Scope**:
- Combine ordinal converters with generated contexts
- Handle multi-assembly scenarios
- Add configuration options for AOT mode

**Estimated Effort**: 2-3 days

### Minimum Viable Change

If only one change can be made, **Phase 1** provides the most value:
- Eliminates the most impactful reflection (`MakeGenericType`)
- Relatively low complexity
- No breaking changes
- Immediate performance and AOT benefits

---

## 6. Full AOT Requirements Summary

For full NativeAOT support, these additional requirements apply:

1. **Pre-register all polymorphic types** - No runtime type discovery
2. **Avoid interface-typed properties** - Or pre-register implementations
3. **Use Ordinal format only** - Named format has reflection fallback
4. **No custom reflection-based converters** - All converters must be AOT-compatible
5. **Delegate dispatch must be re-architected** - `DynamicInvoke` is not AOT-friendly (outside serialization scope)

---

## 7. Recommendations

### Immediate Actions (Phase 1)

1. Implement generated ordinal converters - Highest impact, lowest risk
2. Add converter registration mechanism
3. Update generator to emit strongly-typed converters

### Short-Term (Phase 2)

1. Generate `JsonSerializerContext` per assembly
2. Implement hybrid type info resolver
3. Add tests for AOT trimming scenarios

### Long-Term Considerations

1. Consider STJ polymorphism support (`[JsonDerivedType]`) for specific scenarios
2. Evaluate re-architecting interface polymorphism for full AOT
3. Document AOT limitations clearly

---

## 8. Example: Generated Code Comparison

### Current Generated Code

```csharp
// Person.Ordinal.g.cs
namespace MyApp.Domain
{
    partial class Person : IOrdinalSerializable, IOrdinalSerializationMetadata
    {
        public static string[] PropertyNames { get; } = new[] { "Age", "Name" };
        public static Type[] PropertyTypes { get; } = new[] { typeof(int), typeof(string) };

        public object?[] ToOrdinalArray()
        {
            return new object?[] { this.Age, this.Name };
        }

        public static object FromOrdinalArray(object?[] values)
        {
            return new Person
            {
                Age = (int)values[0]!,
                Name = (string)values[1]!
            };
        }
    }
}
```

### Proposed Generated Code (Phase 1)

```csharp
// Person.Ordinal.g.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using Neatoo.RemoteFactory;

namespace MyApp.Domain
{
    partial class Person : IOrdinalSerializable, IOrdinalSerializationMetadata, IOrdinalConverterProvider<Person>
    {
        public static string[] PropertyNames { get; } = new[] { "Age", "Name" };
        public static Type[] PropertyTypes { get; } = new[] { typeof(int), typeof(string) };

        public object?[] ToOrdinalArray()
        {
            return new object?[] { this.Age, this.Name };
        }

        public static object FromOrdinalArray(object?[] values)
        {
            return new Person
            {
                Age = (int)values[0]!,
                Name = (string)values[1]!
            };
        }

        /// <summary>
        /// Creates an AOT-compatible ordinal converter for this type.
        /// </summary>
        public static JsonConverter<Person> CreateOrdinalConverter() => new PersonOrdinalConverter();
    }

    /// <summary>
    /// Strongly-typed ordinal converter for Person. No reflection required.
    /// </summary>
    internal sealed class PersonOrdinalConverter : JsonConverter<Person>
    {
        private static readonly string[] _propertyNames = Person.PropertyNames;

        public override Person? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.StartObject)
                return ReadNamed(ref reader, options);

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"Expected StartArray or StartObject, got {reader.TokenType}");

            reader.Read(); // Past StartArray

            // Age (int) - position 0
            var prop0 = JsonSerializer.Deserialize<int>(ref reader, options);
            reader.Read();

            // Name (string) - position 1
            var prop1 = JsonSerializer.Deserialize<string>(ref reader, options);
            reader.Read();

            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException($"Too many values in ordinal array for Person. Expected 2, got more.");

            return new Person { Age = prop0, Name = prop1! };
        }

        private static Person? ReadNamed(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            // Fallback to standard STJ for named format
            using var doc = JsonDocument.ParseValue(ref reader);
            var json = doc.RootElement.GetRawText();

            // Create options without this converter to avoid recursion
            var fallbackOptions = new JsonSerializerOptions(options);
            fallbackOptions.Converters.Clear();
            return JsonSerializer.Deserialize<Person>(json, fallbackOptions);
        }

        public override void Write(Utf8JsonWriter writer, Person value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            JsonSerializer.Serialize(writer, value.Age, options);
            JsonSerializer.Serialize(writer, value.Name, options);
            writer.WriteEndArray();
        }
    }
}
```

### Factory Registration (Phase 1)

```csharp
// PersonFactory.g.cs - Add to FactoryServiceRegistrar
internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
{
    // Existing service registrations...

    // Register AOT-compatible ordinal converters
    NeatooOrdinalConverterFactory.RegisterConverter(Person.CreateOrdinalConverter());
}
```

---

## 9. Conclusion

Eliminating reflection from RemoteFactory's JSON serialization is **partially achievable** through STJ source generation. Phase 1 (generated ordinal converters) provides significant benefits with reasonable effort. However, **complete reflection elimination is not possible** due to the fundamental design of interface polymorphism using runtime type names.

For applications requiring full NativeAOT support, architectural constraints must be documented and workarounds provided (pre-registration of all types). For the majority of applications, the phased approach will significantly improve startup performance and AOT trimming without breaking changes.
