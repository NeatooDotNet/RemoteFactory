# FactoryGenerator Refactor: Code Model + Renderer Pattern

## Problem Statement

The current FactoryGenerator has several pain points:

1. **String interpolation soup** - Large `$@"..."` blocks with embedded variables are hard to read/maintain
2. **StringBuilder ceremony** - Returning `StringBuilder` from every method, appending them together
3. **Conditional logic explosion** - If/else for `FactoryMode`, `HasAuth`, `IsAsync`, `IsRemote`, etc. scattered throughout
4. **Code duplication** - E.g., `GenerateEventMethod()` vs `GenerateEventMethodForNonStatic()` are nearly identical
5. **Post-processing hacks** - `.Replace("[, ", "[")` and similar string fixups

## Solution: Code Model + Renderer Pattern

Separate "what to generate" (model) from "how to generate it" (renderer).

```
TypeInfo (existing)
    → FactoryModelBuilder
    → FactoryGenerationUnit (model)
    → FactoryRenderer
    → string (source code)
```

## Model Types

### Top-Level Container

```csharp
record FactoryGenerationUnit
{
    string Namespace { get; init; }
    List<string> Usings { get; init; }
    FactoryMode Mode { get; init; }
    string HintName { get; init; }
    List<DiagnosticInfo> Diagnostics { get; init; }

    // Exactly one of these is set
    ClassFactoryModel? ClassFactory { get; init; }
    StaticFactoryModel? StaticFactory { get; init; }
    InterfaceFactoryModel? InterfaceFactory { get; init; }
}
```

### Factory Models

```csharp
// For [Factory] on a class or record
record ClassFactoryModel
{
    string TypeName { get; init; }
    string ServiceTypeName { get; init; }
    string ImplementationTypeName { get; init; }
    bool IsPartial { get; init; }

    List<FactoryMethodModel> Methods { get; init; }
    List<EventMethodModel> Events { get; init; }
    OrdinalSerializationModel? OrdinalSerialization { get; init; }
}

// For [Factory] on a static class
record StaticFactoryModel
{
    string TypeName { get; init; }
    string SignatureText { get; init; }
    bool IsPartial { get; init; }

    List<ExecuteDelegateModel> Delegates { get; init; }
    List<EventMethodModel> Events { get; init; }
}

// For [Factory] on an interface
record InterfaceFactoryModel
{
    string ServiceTypeName { get; init; }
    string ImplementationTypeName { get; init; }

    List<InterfaceMethodModel> Methods { get; init; }
}
```

### Method Models (Polymorphic Hierarchy)

```csharp
// Base for all factory methods
abstract record FactoryMethodModel
{
    string Name { get; init; }
    string UniqueName { get; init; }
    string ReturnType { get; init; }
    FactoryOperation Operation { get; init; }

    bool IsRemote { get; init; }
    bool IsTask { get; init; }
    bool IsAsync { get; init; }
    bool IsNullable { get; init; }

    List<ParameterModel> Parameters { get; init; }
    AuthorizationModel? Authorization { get; init; }
}

// [Create], [Fetch] - returns new or existing instance
record ReadMethodModel : FactoryMethodModel
{
    bool IsConstructor { get; init; }
    bool IsStaticFactory { get; init; }
    bool IsBool { get; init; }
}

// [Insert], [Update], [Delete] - operates on target
record WriteMethodModel : FactoryMethodModel
{
    // Used internally by SaveMethodModel
}

// Combined Save (Insert + Update + Delete)
record SaveMethodModel : FactoryMethodModel
{
    WriteMethodModel? InsertMethod { get; init; }
    WriteMethodModel? UpdateMethod { get; init; }
    WriteMethodModel? DeleteMethod { get; init; }
    bool IsDefault { get; init; }
}

// CanXxx authorization check methods
record CanMethodModel : FactoryMethodModel
{
    // Returns Authorized, not the entity type
}

// For interface [Factory] methods
record InterfaceMethodModel : FactoryMethodModel
{
    // Delegates to injected service implementation
}
```

### Supporting Models

```csharp
record ParameterModel
{
    string Name { get; init; }
    string Type { get; init; }
    bool IsService { get; init; }
    bool IsTarget { get; init; }
    bool IsCancellationToken { get; init; }
    bool IsParams { get; init; }
}

record AuthorizationModel
{
    List<AuthMethodCall> AuthMethods { get; init; }
    List<AspAuthorizeCall> AspAuthorize { get; init; }
    bool AspForbid { get; init; }
}

record AuthMethodCall
{
    string ClassName { get; init; }
    string MethodName { get; init; }
    bool IsTask { get; init; }
    List<ParameterModel> Parameters { get; init; }
}

record AspAuthorizeCall
{
    List<string> ConstructorArgs { get; init; }
    List<string> NamedArgs { get; init; }
}

record EventMethodModel
{
    string Name { get; init; }
    string DelegateName { get; init; }
    bool IsAsync { get; init; }
    List<ParameterModel> Parameters { get; init; }
    List<ParameterModel> ServiceParameters { get; init; }
}

record ExecuteDelegateModel
{
    string Name { get; init; }
    string DelegateName { get; init; }
    string ReturnType { get; init; }
    bool IsNullable { get; init; }
    List<ParameterModel> Parameters { get; init; }
    List<ParameterModel> ServiceParameters { get; init; }
}

record OrdinalSerializationModel
{
    string TypeName { get; init; }
    string FullTypeName { get; init; }
    bool IsRecord { get; init; }
    bool HasPrimaryConstructor { get; init; }
    List<OrdinalPropertyModel> Properties { get; init; }
    List<string> ConstructorParameterNames { get; init; }
}

record OrdinalPropertyModel
{
    string Name { get; init; }
    string Type { get; init; }
    bool IsNullable { get; init; }
}
```

## Renderer Design

### CodeWriter (Indentation Helper)

```csharp
class CodeWriter
{
    int _indent = 0;
    StringBuilder _sb = new();

    public void Line(string text = "");
    public void OpenBrace();
    public void CloseBrace();
    public IDisposable Block(string header);
    public override string ToString();
}
```

### FactoryRenderer (Main Dispatcher)

```csharp
static class FactoryRenderer
{
    public static string Render(FactoryGenerationUnit unit)
    {
        var w = new CodeWriter();

        w.Line("#nullable enable");
        foreach (var u in unit.Usings) w.Line(u);

        using (w.Block($"namespace {unit.Namespace}"))
        {
            if (unit.ClassFactory != null)
                RenderClassFactory(w, unit.ClassFactory, unit.Mode);
            else if (unit.StaticFactory != null)
                RenderStaticFactory(w, unit.StaticFactory, unit.Mode);
            else if (unit.InterfaceFactory != null)
                RenderInterfaceFactory(w, unit.InterfaceFactory, unit.Mode);
        }

        return w.ToString();
    }

    static void RenderMethod(CodeWriter w, FactoryMethodModel method, FactoryMode mode)
    {
        switch (method)
        {
            case ReadMethodModel m: RenderReadMethod(w, m, mode); break;
            case WriteMethodModel m: RenderWriteMethod(w, m, mode); break;
            case SaveMethodModel m: RenderSaveMethod(w, m, mode); break;
            case CanMethodModel m: RenderCanMethod(w, m, mode); break;
        }
    }
}
```

## Model Builder

Transforms existing `TypeInfo` into the new model types. This is where all conditional logic moves.

```csharp
static class FactoryModelBuilder
{
    public static FactoryGenerationUnit Build(TypeInfo typeInfo)
    {
        var unit = new FactoryGenerationUnit
        {
            Namespace = typeInfo.Namespace,
            Usings = typeInfo.UsingStatements.ToList(),
            Mode = typeInfo.FactoryMode,
            HintName = typeInfo.SafeHintName,
            Diagnostics = typeInfo.Diagnostics.ToList()
        };

        if (typeInfo.IsStatic)
            return unit with { StaticFactory = BuildStaticFactory(typeInfo) };
        else if (typeInfo.IsInterface)
            return unit with { InterfaceFactory = BuildInterfaceFactory(typeInfo) };
        else
            return unit with { ClassFactory = BuildClassFactory(typeInfo) };
    }
}
```

## File Structure

```
src/Generator/
├── FactoryGenerator.cs              # Entry point (simplified)
├── FactoryGenerator.Transform.cs    # Keep existing transform logic
│
├── Model/
│   ├── FactoryGenerationUnit.cs
│   ├── ClassFactoryModel.cs
│   ├── StaticFactoryModel.cs
│   ├── InterfaceFactoryModel.cs
│   ├── Methods/
│   │   ├── FactoryMethodModel.cs
│   │   ├── ReadMethodModel.cs
│   │   ├── WriteMethodModel.cs
│   │   ├── SaveMethodModel.cs
│   │   ├── CanMethodModel.cs
│   │   └── InterfaceMethodModel.cs
│   ├── EventMethodModel.cs
│   ├── ExecuteDelegateModel.cs
│   ├── OrdinalSerializationModel.cs
│   └── Supporting/
│       ├── ParameterModel.cs
│       └── AuthorizationModel.cs
│
├── Builder/
│   └── FactoryModelBuilder.cs
│
└── Renderer/
    ├── CodeWriter.cs
    ├── FactoryRenderer.cs
    ├── ClassFactoryRenderer.cs
    ├── StaticFactoryRenderer.cs
    ├── InterfaceFactoryRenderer.cs
    └── OrdinalRenderer.cs
```

## Simplified Entry Point

```csharp
context.RegisterSourceOutput(classesToGenerate, static (spc, typeInfo) =>
{
    foreach (var diag in typeInfo.Diagnostics)
        ReportDiagnostic(spc, diag);

    var unit = FactoryModelBuilder.Build(typeInfo);
    var source = FactoryRenderer.Render(unit);

    spc.AddSource($"{unit.HintName}Factory.g.cs", source);

    if (unit.ClassFactory?.OrdinalSerialization != null)
    {
        var ordinalSource = OrdinalRenderer.Render(unit);
        spc.AddSource($"{unit.HintName}.Ordinal.g.cs", ordinalSource);
    }
});
```

## What Gets Deleted

- Entire `FactoryMethod` class hierarchy (~900 lines)
- `FactoryText` class
- All `StringBuilder` returning methods
- Post-processing hacks (`.Replace("[, ", "[")`, etc.)

## Validation Strategy

**Existing tests pass without modification** (except trivial whitespace in string comparison tests).

If all tests pass, the rewrite is successful.

## Implementation Tasks

- [ ] Create Model/ directory with all record types
- [ ] Create CodeWriter utility class
- [ ] Create FactoryModelBuilder (TypeInfo → Model)
- [ ] Create FactoryRenderer for ClassFactoryModel
- [ ] Create FactoryRenderer for StaticFactoryModel
- [ ] Create FactoryRenderer for InterfaceFactoryModel
- [ ] Create OrdinalRenderer
- [ ] Simplify FactoryGenerator.cs entry point
- [ ] Delete old FactoryMethod hierarchy and FactoryText
- [ ] Run tests, fix any whitespace differences
