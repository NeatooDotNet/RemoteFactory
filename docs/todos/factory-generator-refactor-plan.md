# FactoryGenerator Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Refactor FactoryGenerator from mixed model/rendering to clean Code Model + Renderer pattern.

**Architecture:** Transform phase produces `TypeInfo` (unchanged). New `FactoryModelBuilder` converts `TypeInfo` to domain-specific models (`ClassFactoryModel`, etc.). New `FactoryRenderer` walks models and emits code using `CodeWriter` helper.

**Tech Stack:** C# records for models, pattern matching for rendering, `CodeWriter` for indentation management.

**Validation:** All existing tests must pass without modification (except trivial whitespace in string comparisons).

---

## Phase 1: Foundation (CodeWriter)

### Task 1.1: Create CodeWriter Utility

**Files:**
- Create: `src/Generator/Renderer/CodeWriter.cs`

**Step 1: Create the CodeWriter class**

```csharp
// src/Generator/Renderer/CodeWriter.cs
using System;
using System.Text;

namespace Neatoo.RemoteFactory.Generator.Renderer;

/// <summary>
/// Helper for generating formatted C# code with automatic indentation management.
/// </summary>
internal class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent = 0;
    private const string IndentString = "    "; // 4 spaces to match NormalizeWhitespace

    public void Line(string text = "")
    {
        if (string.IsNullOrEmpty(text))
        {
            _sb.AppendLine();
        }
        else
        {
            _sb.Append(GetIndent());
            _sb.AppendLine(text);
        }
    }

    public void OpenBrace()
    {
        Line("{");
        _indent++;
    }

    public void CloseBrace()
    {
        _indent--;
        Line("}");
    }

    public IDisposable Block(string header)
    {
        Line(header);
        OpenBrace();
        return new BlockScope(this);
    }

    public IDisposable Braces()
    {
        OpenBrace();
        return new BlockScope(this);
    }

    private string GetIndent() => new string(' ', _indent * 4);

    public override string ToString() => _sb.ToString();

    private class BlockScope : IDisposable
    {
        private readonly CodeWriter _writer;
        public BlockScope(CodeWriter writer) => _writer = writer;
        public void Dispose() => _writer.CloseBrace();
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Renderer/CodeWriter.cs
git commit -m "refactor: add CodeWriter utility for clean code generation"
```

---

## Phase 2: Supporting Models

### Task 2.1: Create ParameterModel

**Files:**
- Create: `src/Generator/Model/Supporting/ParameterModel.cs`

**Step 1: Create the ParameterModel record**

```csharp
// src/Generator/Model/Supporting/ParameterModel.cs
namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a method parameter for code generation.
/// </summary>
internal sealed record ParameterModel
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool IsService { get; init; }
    public bool IsTarget { get; init; }
    public bool IsCancellationToken { get; init; }
    public bool IsParams { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/Supporting/ParameterModel.cs
git commit -m "refactor: add ParameterModel for factory method parameters"
```

### Task 2.2: Create AuthorizationModel

**Files:**
- Create: `src/Generator/Model/Supporting/AuthorizationModel.cs`

**Step 1: Create authorization-related records**

```csharp
// src/Generator/Model/Supporting/AuthorizationModel.cs
using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Authorization configuration for a factory method.
/// </summary>
internal sealed record AuthorizationModel
{
    public required IReadOnlyList<AuthMethodCall> AuthMethods { get; init; }
    public required IReadOnlyList<AspAuthorizeCall> AspAuthorize { get; init; }
    public bool AspForbid { get; init; }

    public bool HasAuth => AuthMethods.Count > 0 || AspAuthorize.Count > 0;
}

/// <summary>
/// A call to an authorization method in an AuthorizeFactory class.
/// </summary>
internal sealed record AuthMethodCall
{
    public required string ClassName { get; init; }
    public required string MethodName { get; init; }
    public bool IsTask { get; init; }
    public required IReadOnlyList<ParameterModel> Parameters { get; init; }
}

/// <summary>
/// An ASP.NET Core [Authorize] attribute applied to the method.
/// </summary>
internal sealed record AspAuthorizeCall
{
    public required IReadOnlyList<string> ConstructorArgs { get; init; }
    public required IReadOnlyList<string> NamedArgs { get; init; }

    public string ToAspAuthorizeDataText()
    {
        var constructorArgsText = string.Join(", ", ConstructorArgs);
        var namedArgsText = string.Join(", ", NamedArgs);
        var text = $"new AspAuthorizeData({constructorArgsText})";
        if (!string.IsNullOrEmpty(namedArgsText))
        {
            text += $"{{ {namedArgsText} }}";
        }
        return text;
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/Supporting/AuthorizationModel.cs
git commit -m "refactor: add AuthorizationModel for factory method auth"
```

### Task 2.3: Create EventMethodModel and ExecuteDelegateModel

**Files:**
- Create: `src/Generator/Model/EventMethodModel.cs`
- Create: `src/Generator/Model/ExecuteDelegateModel.cs`

**Step 1: Create EventMethodModel**

```csharp
// src/Generator/Model/EventMethodModel.cs
using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for an [Event] method that fires asynchronously.
/// </summary>
internal sealed record EventMethodModel
{
    public required string Name { get; init; }
    public required string DelegateName { get; init; }
    public bool IsAsync { get; init; }
    public required IReadOnlyList<ParameterModel> Parameters { get; init; }
    public required IReadOnlyList<ParameterModel> ServiceParameters { get; init; }
    public required string ContainingTypeName { get; init; }
    public bool IsStaticClass { get; init; }
}
```

**Step 2: Create ExecuteDelegateModel**

```csharp
// src/Generator/Model/ExecuteDelegateModel.cs
using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for an [Execute] delegate in a static factory class.
/// </summary>
internal sealed record ExecuteDelegateModel
{
    public required string Name { get; init; }
    public required string DelegateName { get; init; }
    public required string ReturnType { get; init; }
    public bool IsNullable { get; init; }
    public required IReadOnlyList<ParameterModel> Parameters { get; init; }
    public required IReadOnlyList<ParameterModel> ServiceParameters { get; init; }
}
```

**Step 3: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add src/Generator/Model/EventMethodModel.cs src/Generator/Model/ExecuteDelegateModel.cs
git commit -m "refactor: add EventMethodModel and ExecuteDelegateModel"
```

### Task 2.4: Create OrdinalSerializationModel

**Files:**
- Create: `src/Generator/Model/OrdinalSerializationModel.cs`

**Step 1: Create the model**

```csharp
// src/Generator/Model/OrdinalSerializationModel.cs
using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for generating ordinal serialization support.
/// </summary>
internal sealed record OrdinalSerializationModel
{
    public required string TypeName { get; init; }
    public required string FullTypeName { get; init; }
    public required string Namespace { get; init; }
    public bool IsRecord { get; init; }
    public bool HasPrimaryConstructor { get; init; }
    public required IReadOnlyList<OrdinalPropertyModel> Properties { get; init; }
    public required IReadOnlyList<string> ConstructorParameterNames { get; init; }
    public required IReadOnlyList<string> Usings { get; init; }
}

/// <summary>
/// A property in ordinal serialization order.
/// </summary>
internal sealed record OrdinalPropertyModel
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool IsNullable { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/OrdinalSerializationModel.cs
git commit -m "refactor: add OrdinalSerializationModel"
```

---

## Phase 3: Method Models

### Task 3.1: Create FactoryMethodModel Base and ReadMethodModel

**Files:**
- Create: `src/Generator/Model/Methods/FactoryMethodModel.cs`
- Create: `src/Generator/Model/Methods/ReadMethodModel.cs`

**Step 1: Create the abstract base**

```csharp
// src/Generator/Model/Methods/FactoryMethodModel.cs
using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Base model for all factory methods.
/// </summary>
internal abstract record FactoryMethodModel
{
    public required string Name { get; init; }
    public required string UniqueName { get; init; }
    public required string ReturnType { get; init; }
    public required string ServiceType { get; init; }
    public required string ImplementationType { get; init; }
    public FactoryOperation Operation { get; init; }

    public bool IsRemote { get; init; }
    public bool IsTask { get; init; }
    public bool IsAsync { get; init; }
    public bool IsNullable { get; init; }

    public required IReadOnlyList<ParameterModel> Parameters { get; init; }
    public AuthorizationModel? Authorization { get; init; }

    public bool HasAuth => Authorization?.HasAuth ?? false;
}
```

**Step 2: Create ReadMethodModel**

```csharp
// src/Generator/Model/Methods/ReadMethodModel.cs
namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for [Create] and [Fetch] factory methods.
/// </summary>
internal sealed record ReadMethodModel : FactoryMethodModel
{
    public bool IsConstructor { get; init; }
    public bool IsStaticFactory { get; init; }
    public bool IsBool { get; init; }
}
```

**Step 3: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add src/Generator/Model/Methods/
git commit -m "refactor: add FactoryMethodModel and ReadMethodModel"
```

### Task 3.2: Create WriteMethodModel and SaveMethodModel

**Files:**
- Create: `src/Generator/Model/Methods/WriteMethodModel.cs`
- Create: `src/Generator/Model/Methods/SaveMethodModel.cs`

**Step 1: Create WriteMethodModel**

```csharp
// src/Generator/Model/Methods/WriteMethodModel.cs
namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for [Insert], [Update], [Delete] factory methods.
/// </summary>
internal sealed record WriteMethodModel : FactoryMethodModel
{
    // Write methods operate on a target parameter
    // Used internally by SaveMethodModel
}
```

**Step 2: Create SaveMethodModel**

```csharp
// src/Generator/Model/Methods/SaveMethodModel.cs
namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for combined Save method (Insert + Update + Delete).
/// </summary>
internal sealed record SaveMethodModel : FactoryMethodModel
{
    public WriteMethodModel? InsertMethod { get; init; }
    public WriteMethodModel? UpdateMethod { get; init; }
    public WriteMethodModel? DeleteMethod { get; init; }
    public bool IsDefault { get; init; }
}
```

**Step 3: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add src/Generator/Model/Methods/WriteMethodModel.cs src/Generator/Model/Methods/SaveMethodModel.cs
git commit -m "refactor: add WriteMethodModel and SaveMethodModel"
```

### Task 3.3: Create CanMethodModel and InterfaceMethodModel

**Files:**
- Create: `src/Generator/Model/Methods/CanMethodModel.cs`
- Create: `src/Generator/Model/Methods/InterfaceMethodModel.cs`

**Step 1: Create CanMethodModel**

```csharp
// src/Generator/Model/Methods/CanMethodModel.cs
namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for CanXxx authorization check methods.
/// Returns Authorized instead of the entity type.
/// </summary>
internal sealed record CanMethodModel : FactoryMethodModel
{
    // CanXxx methods return Authorized, not the entity
}
```

**Step 2: Create InterfaceMethodModel**

```csharp
// src/Generator/Model/Methods/InterfaceMethodModel.cs
namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for methods on interface factories.
/// </summary>
internal sealed record InterfaceMethodModel : FactoryMethodModel
{
    // Interface methods delegate to injected service
}
```

**Step 3: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add src/Generator/Model/Methods/CanMethodModel.cs src/Generator/Model/Methods/InterfaceMethodModel.cs
git commit -m "refactor: add CanMethodModel and InterfaceMethodModel"
```

---

## Phase 4: Factory Models

### Task 4.1: Create FactoryGenerationUnit

**Files:**
- Create: `src/Generator/Model/FactoryGenerationUnit.cs`

**Step 1: Create the top-level container**

```csharp
// src/Generator/Model/FactoryGenerationUnit.cs
using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Top-level container for a factory generation unit.
/// Represents all the information needed to generate one source file.
/// </summary>
internal sealed record FactoryGenerationUnit
{
    public required string Namespace { get; init; }
    public required IReadOnlyList<string> Usings { get; init; }
    public FactoryMode Mode { get; init; }
    public required string HintName { get; init; }
    public required IReadOnlyList<DiagnosticInfo> Diagnostics { get; init; }

    // Exactly one of these should be set
    public ClassFactoryModel? ClassFactory { get; init; }
    public StaticFactoryModel? StaticFactory { get; init; }
    public InterfaceFactoryModel? InterfaceFactory { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/FactoryGenerationUnit.cs
git commit -m "refactor: add FactoryGenerationUnit top-level container"
```

### Task 4.2: Create ClassFactoryModel

**Files:**
- Create: `src/Generator/Model/ClassFactoryModel.cs`

**Step 1: Create the model**

```csharp
// src/Generator/Model/ClassFactoryModel.cs
using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for generating a factory from a class or record with [Factory].
/// </summary>
internal sealed record ClassFactoryModel
{
    public required string TypeName { get; init; }
    public required string ServiceTypeName { get; init; }
    public required string ImplementationTypeName { get; init; }
    public bool IsPartial { get; init; }

    public required IReadOnlyList<FactoryMethodModel> Methods { get; init; }
    public required IReadOnlyList<EventMethodModel> Events { get; init; }
    public OrdinalSerializationModel? OrdinalSerialization { get; init; }

    /// <summary>
    /// True if there's a default Save method (implements IFactorySave).
    /// </summary>
    public bool HasDefaultSave { get; init; }

    /// <summary>
    /// True if the entity type needs to be registered in DI.
    /// </summary>
    public bool RequiresEntityRegistration { get; init; }

    /// <summary>
    /// True if ordinal converter should be registered.
    /// </summary>
    public bool RegisterOrdinalConverter { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/ClassFactoryModel.cs
git commit -m "refactor: add ClassFactoryModel"
```

### Task 4.3: Create StaticFactoryModel

**Files:**
- Create: `src/Generator/Model/StaticFactoryModel.cs`

**Step 1: Create the model**

```csharp
// src/Generator/Model/StaticFactoryModel.cs
using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for generating a factory from a static class with [Factory].
/// </summary>
internal sealed record StaticFactoryModel
{
    public required string TypeName { get; init; }
    public required string SignatureText { get; init; }
    public bool IsPartial { get; init; }

    public required IReadOnlyList<ExecuteDelegateModel> Delegates { get; init; }
    public required IReadOnlyList<EventMethodModel> Events { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/StaticFactoryModel.cs
git commit -m "refactor: add StaticFactoryModel"
```

### Task 4.4: Create InterfaceFactoryModel

**Files:**
- Create: `src/Generator/Model/InterfaceFactoryModel.cs`

**Step 1: Create the model**

```csharp
// src/Generator/Model/InterfaceFactoryModel.cs
using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for generating a factory from an interface with [Factory].
/// </summary>
internal sealed record InterfaceFactoryModel
{
    public required string ServiceTypeName { get; init; }
    public required string ImplementationTypeName { get; init; }

    public required IReadOnlyList<InterfaceMethodModel> Methods { get; init; }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Model/InterfaceFactoryModel.cs
git commit -m "refactor: add InterfaceFactoryModel"
```

---

## Phase 5: Model Builder

### Task 5.1: Create FactoryModelBuilder - Structure

**Files:**
- Create: `src/Generator/Builder/FactoryModelBuilder.cs`

**Step 1: Create the builder skeleton**

```csharp
// src/Generator/Builder/FactoryModelBuilder.cs
using System.Collections.Generic;
using System.Linq;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Builder;

/// <summary>
/// Transforms TypeInfo into domain-specific factory models.
/// All conditional logic for "what to generate" lives here.
/// </summary>
internal static class FactoryModelBuilder
{
    public static Model.FactoryGenerationUnit Build(Factory.TypeInfo typeInfo)
    {
        var unit = new Model.FactoryGenerationUnit
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

    private static Model.StaticFactoryModel BuildStaticFactory(Factory.TypeInfo typeInfo)
    {
        // TODO: Implement in Task 5.2
        throw new System.NotImplementedException();
    }

    private static Model.InterfaceFactoryModel BuildInterfaceFactory(Factory.TypeInfo typeInfo)
    {
        // TODO: Implement in Task 5.3
        throw new System.NotImplementedException();
    }

    private static Model.ClassFactoryModel BuildClassFactory(Factory.TypeInfo typeInfo)
    {
        // TODO: Implement in Task 5.4
        throw new System.NotImplementedException();
    }
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Builder/FactoryModelBuilder.cs
git commit -m "refactor: add FactoryModelBuilder skeleton"
```

### Task 5.2: Implement BuildStaticFactory

**Files:**
- Modify: `src/Generator/Builder/FactoryModelBuilder.cs`

**Step 1: Implement the method**

Add these helper methods and implement BuildStaticFactory:

```csharp
private static Model.ParameterModel BuildParameter(Factory.MethodParameterInfo p)
{
    return new Model.ParameterModel
    {
        Name = p.Name,
        Type = p.Type,
        IsService = p.IsService,
        IsTarget = p.IsTarget,
        IsCancellationToken = p.IsCancellationToken,
        IsParams = p.IsParams
    };
}

private static Model.StaticFactoryModel BuildStaticFactory(Factory.TypeInfo typeInfo)
{
    var delegates = new List<Model.ExecuteDelegateModel>();
    var events = new List<Model.EventMethodModel>();

    foreach (var method in typeInfo.FactoryMethods)
    {
        if (method.FactoryOperation == FactoryOperation.Event)
        {
            events.Add(BuildEventMethod(typeInfo, method, isStaticClass: true));
        }
        else if (method.FactoryOperation == FactoryOperation.Execute)
        {
            var delegateName = method.Name;
            if (delegateName.StartsWith("Execute"))
                delegateName = delegateName.Substring("Execute".Length);
            if (delegateName.StartsWith("_"))
                delegateName = delegateName.Substring(1);

            var parameters = method.Parameters.ToList();

            delegates.Add(new Model.ExecuteDelegateModel
            {
                Name = method.Name,
                DelegateName = delegateName,
                ReturnType = method.ReturnType ?? "void",
                IsNullable = method.IsNullable,
                Parameters = parameters.Where(p => !p.IsService && !p.IsCancellationToken)
                    .Select(BuildParameter).ToList(),
                ServiceParameters = parameters.Where(p => p.IsService)
                    .Select(BuildParameter).ToList()
            });
        }
    }

    return new Model.StaticFactoryModel
    {
        TypeName = typeInfo.Name,
        SignatureText = typeInfo.SignatureText,
        IsPartial = typeInfo.IsPartial,
        Delegates = delegates,
        Events = events
    };
}

private static Model.EventMethodModel BuildEventMethod(Factory.TypeInfo typeInfo, Factory.TypeFactoryMethodInfo method, bool isStaticClass)
{
    var parameters = method.Parameters.ToList();
    var delegateName = method.Name;
    if (!delegateName.EndsWith("Event"))
        delegateName = $"{delegateName}Event";

    return new Model.EventMethodModel
    {
        Name = method.Name,
        DelegateName = delegateName,
        IsAsync = method.IsTask,
        Parameters = parameters.Where(p => !p.IsService && !p.IsCancellationToken)
            .Select(BuildParameter).ToList(),
        ServiceParameters = parameters.Where(p => p.IsService)
            .Select(BuildParameter).ToList(),
        ContainingTypeName = isStaticClass ? typeInfo.Name : typeInfo.ImplementationTypeName,
        IsStaticClass = isStaticClass
    };
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Builder/FactoryModelBuilder.cs
git commit -m "refactor: implement BuildStaticFactory in FactoryModelBuilder"
```

### Task 5.3: Implement BuildInterfaceFactory

**Files:**
- Modify: `src/Generator/Builder/FactoryModelBuilder.cs`

**Step 1: Implement the method**

```csharp
private static Model.InterfaceFactoryModel BuildInterfaceFactory(Factory.TypeInfo typeInfo)
{
    var methods = new List<Model.InterfaceMethodModel>();

    foreach (var method in typeInfo.FactoryMethods)
    {
        var parameters = method.Parameters.Select(BuildParameter).ToList();

        methods.Add(new Model.InterfaceMethodModel
        {
            Name = method.Name,
            UniqueName = method.Name,
            ReturnType = method.ReturnType ?? typeInfo.ServiceTypeName,
            ServiceType = typeInfo.ServiceTypeName,
            ImplementationType = typeInfo.ImplementationTypeName,
            Operation = method.FactoryOperation,
            IsRemote = true, // Interface methods are always remote
            IsTask = method.IsTask,
            IsAsync = method.IsTask,
            IsNullable = method.IsNullable,
            Parameters = parameters,
            Authorization = BuildAuthorization(method)
        });
    }

    // Add CanXxx methods for methods with auth
    var canMethods = new List<Model.InterfaceMethodModel>();
    foreach (var method in methods.Where(m => m.HasAuth))
    {
        // Only add Can method if auth doesn't have target parameter
        var authHasTarget = method.Authorization?.AuthMethods
            .SelectMany(a => a.Parameters)
            .Any(p => p.IsTarget) ?? false;

        if (!authHasTarget)
        {
            canMethods.Add(BuildCanMethod(typeInfo, method));
        }
    }
    methods.AddRange(canMethods);

    // Ensure unique names
    AssignUniqueNames(methods.Cast<Model.FactoryMethodModel>().ToList());

    return new Model.InterfaceFactoryModel
    {
        ServiceTypeName = typeInfo.ServiceTypeName,
        ImplementationTypeName = typeInfo.ImplementationTypeName,
        Methods = methods
    };
}

private static Model.AuthorizationModel? BuildAuthorization(Factory.TypeFactoryMethodInfo method)
{
    var authMethods = method.AuthMethodInfos.Select(a => new Model.AuthMethodCall
    {
        ClassName = a.ClassName,
        MethodName = a.Name,
        IsTask = a.IsTask,
        Parameters = a.Parameters.Select(BuildParameter).ToList()
    }).ToList();

    var aspAuthorize = method.AspAuthorizeCalls.Select(a => new Model.AspAuthorizeCall
    {
        ConstructorArgs = a.ConstructorArguments.ToList(),
        NamedArgs = a.NamedArguments.ToList()
    }).ToList();

    if (authMethods.Count == 0 && aspAuthorize.Count == 0)
        return null;

    return new Model.AuthorizationModel
    {
        AuthMethods = authMethods,
        AspAuthorize = aspAuthorize,
        AspForbid = false // Set appropriately based on context
    };
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Builder/FactoryModelBuilder.cs
git commit -m "refactor: implement BuildInterfaceFactory in FactoryModelBuilder"
```

### Task 5.4: Implement BuildClassFactory

**Files:**
- Modify: `src/Generator/Builder/FactoryModelBuilder.cs`

**Step 1: Implement the method (this is the largest builder method)**

```csharp
private static Model.ClassFactoryModel BuildClassFactory(Factory.TypeInfo typeInfo)
{
    var methods = new List<Model.FactoryMethodModel>();
    var events = new List<Model.EventMethodModel>();
    var writeMethodGroups = new Dictionary<string, List<Model.WriteMethodModel>>();

    foreach (var method in typeInfo.FactoryMethods)
    {
        if (method.FactoryOperation == FactoryOperation.Event)
        {
            events.Add(BuildEventMethod(typeInfo, method, isStaticClass: false));
            continue;
        }

        if (method.IsSave)
        {
            var writeMethod = BuildWriteMethod(typeInfo, method);

            // Group by parameter signature (excluding target, service, CT)
            var key = string.Join(",", writeMethod.Parameters
                .Where(p => !p.IsTarget && !p.IsService && !p.IsCancellationToken)
                .Select(p => p.Type));

            if (!writeMethodGroups.ContainsKey(key))
                writeMethodGroups[key] = new List<Model.WriteMethodModel>();
            writeMethodGroups[key].Add(writeMethod);

            methods.Add(writeMethod);
        }
        else
        {
            methods.Add(BuildReadMethod(typeInfo, method));
        }
    }

    // Build Save methods from write method groups
    var saveMethods = BuildSaveMethods(typeInfo, writeMethodGroups);
    methods.AddRange(saveMethods);

    // Add CanXxx methods
    foreach (var method in methods.ToList())
    {
        if (method.HasAuth)
        {
            var authHasTarget = method.Authorization?.AuthMethods
                .SelectMany(a => a.Parameters)
                .Any(p => p.IsTarget) ?? false;

            if (!authHasTarget && !methods.Any(m => m.Name == $"Can{method.Name}"))
            {
                methods.Add(BuildCanMethodFromFactory(typeInfo, method));
            }
        }
    }

    // Ensure unique names
    AssignUniqueNames(methods);

    // Determine if entity registration is needed
    var requiresEntityRegistration = typeInfo.FactoryMode == FactoryMode.Full &&
        methods.OfType<Model.ReadMethodModel>()
            .Any(m => !m.IsConstructor && !m.IsStaticFactory);

    // Check for default save
    var defaultSave = saveMethods
        .FirstOrDefault(s => s.Parameters.Count(p => !p.IsTarget && !p.IsService && !p.IsCancellationToken) == 0);
    if (defaultSave != null)
    {
        // Mark as default by creating new instance with IsDefault = true
        var index = methods.IndexOf(defaultSave);
        methods[index] = defaultSave with { IsDefault = true };
    }

    return new Model.ClassFactoryModel
    {
        TypeName = typeInfo.Name,
        ServiceTypeName = typeInfo.ServiceTypeName,
        ImplementationTypeName = typeInfo.ImplementationTypeName,
        IsPartial = typeInfo.IsPartial,
        Methods = methods,
        Events = events,
        HasDefaultSave = defaultSave != null,
        RequiresEntityRegistration = requiresEntityRegistration,
        RegisterOrdinalConverter = ShouldRegisterOrdinalConverter(typeInfo),
        OrdinalSerialization = BuildOrdinalSerialization(typeInfo)
    };
}

private static Model.ReadMethodModel BuildReadMethod(Factory.TypeInfo typeInfo, Factory.TypeFactoryMethodInfo method)
{
    var parameters = method.Parameters.Select(BuildParameter).ToList();
    var isRemote = method.IsRemote || method.AuthMethodInfos.Any(a => a.IsRemote) || method.AspAuthorizeCalls.Any();

    return new Model.ReadMethodModel
    {
        Name = method.Name,
        UniqueName = method.Name,
        ReturnType = typeInfo.ServiceTypeName,
        ServiceType = typeInfo.ServiceTypeName,
        ImplementationType = typeInfo.ImplementationTypeName,
        Operation = method.FactoryOperation,
        IsRemote = isRemote,
        IsTask = isRemote || method.IsTask || method.AuthMethodInfos.Any(a => a.IsTask),
        IsAsync = method.AuthMethodInfos.Any(a => a.IsTask) || method.AspAuthorizeCalls.Any(),
        IsNullable = method.IsNullable || method.IsBool,
        Parameters = parameters,
        Authorization = BuildAuthorization(method),
        IsConstructor = method.IsConstructor,
        IsStaticFactory = method.IsStaticFactory,
        IsBool = method.IsBool
    };
}

private static Model.WriteMethodModel BuildWriteMethod(Factory.TypeInfo typeInfo, Factory.TypeFactoryMethodInfo method)
{
    var parameters = method.Parameters.Select(BuildParameter).ToList();

    // Add target parameter at the beginning if not already present
    if (!parameters.Any(p => p.IsTarget))
    {
        parameters.Insert(0, new Model.ParameterModel
        {
            Name = "target",
            Type = typeInfo.ServiceTypeName,
            IsTarget = true
        });
    }

    var isRemote = method.IsRemote || method.AuthMethodInfos.Any(a => a.IsRemote) || method.AspAuthorizeCalls.Any();

    return new Model.WriteMethodModel
    {
        Name = method.Name,
        UniqueName = method.Name,
        ReturnType = typeInfo.ServiceTypeName,
        ServiceType = typeInfo.ServiceTypeName,
        ImplementationType = typeInfo.ImplementationTypeName,
        Operation = method.FactoryOperation,
        IsRemote = isRemote,
        IsTask = isRemote || method.IsTask || method.AuthMethodInfos.Any(a => a.IsTask),
        IsAsync = method.AuthMethodInfos.Any(a => a.IsTask) || method.AspAuthorizeCalls.Any(),
        IsNullable = method.IsNullable,
        Parameters = parameters,
        Authorization = BuildAuthorization(method)
    };
}

private static List<Model.SaveMethodModel> BuildSaveMethods(
    Factory.TypeInfo typeInfo,
    Dictionary<string, List<Model.WriteMethodModel>> writeMethodGroups)
{
    var saveMethods = new List<Model.SaveMethodModel>();
    var nameOverride = writeMethodGroups.Count == 1 ? "Save" : null;

    foreach (var group in writeMethodGroups)
    {
        var insertMethod = group.Value.FirstOrDefault(m => m.Operation == FactoryOperation.Insert);
        var updateMethod = group.Value.FirstOrDefault(m => m.Operation == FactoryOperation.Update);
        var deleteMethod = group.Value.FirstOrDefault(m => m.Operation == FactoryOperation.Delete);

        var representative = group.Value.OrderByDescending(m => (int)m.Operation).First();
        var namePostfix = representative.Name.Replace(representative.Operation.ToString(), "");
        var name = nameOverride ?? $"Save{namePostfix}";

        var isRemote = group.Value.Any(m => m.IsRemote);
        var isTask = isRemote || group.Value.Any(m => m.IsTask);
        var hasAuth = group.Value.Any(m => m.HasAuth);
        var isNullable = deleteMethod != null || group.Value.Any(m => m.IsNullable);

        saveMethods.Add(new Model.SaveMethodModel
        {
            Name = name,
            UniqueName = name,
            ReturnType = typeInfo.ServiceTypeName,
            ServiceType = typeInfo.ServiceTypeName,
            ImplementationType = typeInfo.ImplementationTypeName,
            Operation = FactoryOperation.Insert, // Representative
            IsRemote = isRemote,
            IsTask = isTask,
            IsAsync = group.Value.Any(m => m.IsAsync),
            IsNullable = isNullable,
            Parameters = representative.Parameters,
            Authorization = hasAuth ? new Model.AuthorizationModel
            {
                AuthMethods = group.Value.SelectMany(m => m.Authorization?.AuthMethods ?? []).Distinct().ToList(),
                AspAuthorize = group.Value.SelectMany(m => m.Authorization?.AspAuthorize ?? []).Distinct().ToList(),
                AspForbid = false
            } : null,
            InsertMethod = insertMethod,
            UpdateMethod = updateMethod,
            DeleteMethod = deleteMethod,
            IsDefault = false
        });
    }

    return saveMethods;
}

private static void AssignUniqueNames(List<Model.FactoryMethodModel> methods)
{
    var usedNames = new HashSet<string>();

    foreach (var method in methods.OrderBy(m => m.Parameters.Count))
    {
        var baseName = method.UniqueName;
        var uniqueName = baseName;
        var counter = 1;

        while (usedNames.Contains(uniqueName))
        {
            uniqueName = $"{baseName}{counter}";
            counter++;
        }

        usedNames.Add(uniqueName);

        if (uniqueName != method.UniqueName)
        {
            // Need to update - but records are immutable, so we need to replace in list
            var index = methods.IndexOf(method);
            methods[index] = method switch
            {
                Model.ReadMethodModel r => r with { UniqueName = uniqueName },
                Model.WriteMethodModel w => w with { UniqueName = uniqueName },
                Model.SaveMethodModel s => s with { UniqueName = uniqueName },
                Model.CanMethodModel c => c with { UniqueName = uniqueName },
                Model.InterfaceMethodModel i => i with { UniqueName = uniqueName },
                _ => method
            };
        }
    }
}

private static bool ShouldRegisterOrdinalConverter(Factory.TypeInfo typeInfo)
{
    return typeInfo.OrdinalProperties.Any() &&
        typeInfo.IsPartial &&
        !typeInfo.IsNested &&
        (!typeInfo.IsRecord || typeInfo.HasPrimaryConstructor) &&
        !typeInfo.RequiresServiceInstantiation;
}

private static Model.OrdinalSerializationModel? BuildOrdinalSerialization(Factory.TypeInfo typeInfo)
{
    if (!typeInfo.OrdinalProperties.Any() || !typeInfo.IsPartial || typeInfo.IsNested)
        return null;

    if (typeInfo.IsRecord && !typeInfo.HasPrimaryConstructor)
        return null;

    if (typeInfo.RequiresServiceInstantiation)
        return null;

    return new Model.OrdinalSerializationModel
    {
        TypeName = typeInfo.Name,
        FullTypeName = $"{typeInfo.Namespace}.{typeInfo.Name}",
        Namespace = typeInfo.Namespace,
        IsRecord = typeInfo.IsRecord,
        HasPrimaryConstructor = typeInfo.HasPrimaryConstructor,
        Properties = typeInfo.OrdinalProperties.Select(p => new Model.OrdinalPropertyModel
        {
            Name = p.Name,
            Type = p.Type,
            IsNullable = p.IsNullable
        }).ToList(),
        ConstructorParameterNames = typeInfo.PrimaryConstructorParameterNames.ToList(),
        Usings = typeInfo.UsingStatements.ToList()
    };
}

private static Model.CanMethodModel BuildCanMethodFromFactory(Factory.TypeInfo typeInfo, Model.FactoryMethodModel method)
{
    return new Model.CanMethodModel
    {
        Name = $"Can{method.Name}",
        UniqueName = $"Can{method.Name}",
        ReturnType = "Authorized",
        ServiceType = typeInfo.ServiceTypeName,
        ImplementationType = typeInfo.ImplementationTypeName,
        Operation = method.Operation,
        IsRemote = method.IsRemote,
        IsTask = method.IsTask,
        IsAsync = method.IsAsync,
        IsNullable = false,
        Parameters = method.Authorization?.AuthMethods
            .SelectMany(a => a.Parameters)
            .DistinctBy(p => p.Name)
            .ToList() ?? [],
        Authorization = method.Authorization
    };
}
```

**Step 2: Verify it compiles**

Run: `dotnet build src/Generator/Generator.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/Generator/Builder/FactoryModelBuilder.cs
git commit -m "refactor: implement BuildClassFactory in FactoryModelBuilder"
```

---

## Phase 6: Renderers

This is the largest phase. Each renderer is responsible for emitting code for its model type.

### Task 6.1: Create FactoryRenderer Entry Point

**Files:**
- Create: `src/Generator/Renderer/FactoryRenderer.cs`

### Task 6.2: Create ClassFactoryRenderer

**Files:**
- Create: `src/Generator/Renderer/ClassFactoryRenderer.cs`

### Task 6.3: Create MethodRenderers (Read, Write, Save, Can)

**Files:**
- Create: `src/Generator/Renderer/MethodRenderer.cs`

### Task 6.4: Create StaticFactoryRenderer

**Files:**
- Create: `src/Generator/Renderer/StaticFactoryRenderer.cs`

### Task 6.5: Create InterfaceFactoryRenderer

**Files:**
- Create: `src/Generator/Renderer/InterfaceFactoryRenderer.cs`

### Task 6.6: Create OrdinalRenderer

**Files:**
- Create: `src/Generator/Renderer/OrdinalRenderer.cs`

---

## Phase 7: Integration

### Task 7.1: Update FactoryGenerator Entry Point

**Files:**
- Modify: `src/Generator/FactoryGenerator.cs`

Replace the generation methods with calls to the new model builder and renderer.

### Task 7.2: Run Tests - First Pass

Run: `dotnet test src/Neatoo.RemoteFactory.sln`

Fix any compilation or obvious errors.

### Task 7.3: Compare Generated Output

Compare generated files before and after to identify differences.

### Task 7.4: Iterate on Renderer to Match Output

Adjust renderer to produce identical output to the original.

---

## Phase 8: Cleanup

### Task 8.1: Delete Old Code

**Files:**
- Delete from `src/Generator/FactoryGenerator.cs`: `GenerateFactory`, `GenerateExecute`, `GenerateInterfaceFactory`, `GenerateOrdinalSerialization` methods
- Delete from `src/Generator/FactoryGenerator.Types.cs`: `FactoryMethod` hierarchy, `FactoryText` class

### Task 8.2: Final Test Run

Run: `dotnet test src/Neatoo.RemoteFactory.sln`

All tests must pass.

### Task 8.3: Final Commit

```bash
git add -A
git commit -m "refactor: complete FactoryGenerator Code Model + Renderer refactor"
```

---

## Notes

- **Phases 1-5** can be done incrementally with the old generator still working
- **Phase 6** is the critical implementation phase - the renderers must produce identical output
- **Phase 7** is where we wire up and validate
- **Phase 8** is cleanup after all tests pass

The key insight is that the existing tests validate the *behavior* of the generated factories, not the exact string output. As long as the generated code is semantically equivalent (compiles and behaves the same), tests should pass.
