# FactoryGenerator Data Model Analysis

## Executive Summary

This document provides a thorough analysis of all data model records and classes in the `FactoryGenerator` source generator. The analysis evaluates:
- Whether types should be separate top-level classes vs. nested
- Clarity and design quality
- Redundancy issues
- Overall design recommendations

**Key Findings:**
1. **Code duplication exists** between `FactoryGenerator.cs` and `FactoryGenerator.Types.cs` - identical type definitions appear in both files
2. **Location information is duplicated** across `TypeInfo`, `TypeFactoryMethodInfo`, and `DiagnosticInfo`
3. **MethodInfo/MethodParameterInfo are shared** with code generation classes creating tight coupling
4. **Nested types are appropriate** for internal generator types but separation into partial files is correct
5. **FactoryMethod hierarchy** mixes data and behavior - acceptable for code generation but could be cleaner

---

## 1. Type Inventory

| Type | Category | Lines | File | Purpose |
|------|----------|-------|------|---------|
| `TypeInfo` | Data Model | ~95 | FactoryGenerator.Types.cs | Container for type with [Factory] attribute |
| `TypeFactoryMethodInfo` | Data Model | ~65 | FactoryGenerator.Types.cs | Factory method information |
| `TypeAuthMethodInfo` | Data Model | ~90 | FactoryGenerator.Types.cs | Authorization method information |
| `MethodInfo` | Data Model (Base) | ~45 | FactoryGenerator.Types.cs | Base record for method info |
| `MethodParameterInfo` | Data Model | ~30 | FactoryGenerator.Types.cs | Method parameter information |
| `AspAuthorizeInfo` | Data Model | ~45 | FactoryGenerator.Types.cs | ASP.NET Authorize attribute info |
| `DiagnosticInfo` | Data Model | ~110 | DiagnosticInfo.cs | Diagnostic location information |
| `FactoryMethod` | Code Gen (Abstract) | ~210 | FactoryGenerator.Types.cs | Base class for code generation |
| `ReadFactoryMethod` | Code Gen | ~90 | FactoryGenerator.Types.cs | Create/Fetch operations |
| `WriteFactoryMethod` | Code Gen | ~25 | FactoryGenerator.Types.cs | Insert/Update/Delete operations |
| `SaveFactoryMethod` | Code Gen | ~165 | FactoryGenerator.Types.cs | Combined Save operations |
| `InterfaceFactoryMethod` | Code Gen | ~40 | FactoryGenerator.Types.cs | Interface-based operations |
| `CanFactoryMethod` | Code Gen | ~65 | FactoryGenerator.Types.cs | Authorization check methods |
| `FactoryText` | Helper | ~12 | FactoryGenerator.Types.cs | Code fragment container |
| `EquatableArray<T>` | Infrastructure | ~104 | EquatableArray.cs | Immutable equatable array |
| `HashCode` | Infrastructure | ~385 | HashCode.cs | Polyfill for hash codes |

---

## 2. Detailed Type Analysis

### 2.1 TypeInfo

**Purpose:** Main container for all extracted information about a type decorated with `[Factory]` attribute. Populated during the transform phase, consumed during generation.

**Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| `Name` | `string` | Type identifier |
| `IsPartial` | `bool` | Whether type is partial |
| `SignatureText` | `string` | Full type signature text |
| `IsInterface` | `bool` | Interface vs class |
| `IsStatic` | `bool` | Static class indicator |
| `ServiceTypeName` | `string` | Service type for DI |
| `ImplementationTypeName` | `string` | Implementation type name |
| `Namespace` | `string` | Containing namespace |
| `UsingStatements` | `EquatableArray<string>` | Required using directives |
| `FactoryMethods` | `EquatableArray<TypeFactoryMethodInfo>` | Factory methods |
| `AuthMethods` | `EquatableArray<TypeAuthMethodInfo>` | Authorization methods |
| `SafeHintName` | `string` | Safe file name for source output |
| `Diagnostics` | `EquatableArray<DiagnosticInfo>` | Collected diagnostics |
| `ClassFilePath` | `string` | Source file path |
| `ClassStartLine` | `int` | Start line (0-indexed) |
| `ClassStartColumn` | `int` | Start column |
| `ClassEndLine` | `int` | End line |
| `ClassEndColumn` | `int` | End column |
| `ClassTextSpanStart` | `int` | Text span start |
| `ClassTextSpanLength` | `int` | Text span length |

**Construction:** Constructor accepts `TypeDeclarationSyntax`, `INamedTypeSymbol`, and `SemanticModel`. Performs all extraction logic internally.

**Usage:**
- Created in `TransformClassFactory` and `TransformInterfaceFactory`
- Consumed in `GenerateFactory`, `GenerateExecute`, `GenerateInterfaceFactory`

**Issues:**
1. **Location properties are duplicated** - 7 properties for location info that could be a separate `SourceLocation` record
2. **Constructor is too large** (~60 lines) - does too much work
3. **Mutable properties** - `FactoryMethods` and `AuthMethods` have `set` accessors but shouldn't be modified after construction

**Clarity:** Name is clear. The "Type" prefix (vs just "Info") helps distinguish it from other `XxxInfo` types.

---

### 2.2 TypeFactoryMethodInfo

**Purpose:** Contains information about a method decorated with a factory operation attribute (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]`).

**Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| *Inherited from MethodInfo* | | |
| `AuthMethodInfos` | `EquatableArray<TypeAuthMethodInfo>` | Associated auth methods |
| `NamePostfix` | `string` (override) | Method name without operation |
| `IsConstructor` | `bool` | Constructor indicator |
| `FactoryOperation` | `FactoryOperation` | Operation type enum |
| `IsSave` | `bool` | Insert/Update/Delete operation |
| `IsStaticFactory` | `bool` | Static factory method |
| `MethodFilePath` | `string` | Source file path |
| `MethodStartLine` | `int` | Start line |
| `MethodStartColumn` | `int` | Start column |
| `MethodEndLine` | `int` | End line |
| `MethodEndColumn` | `int` | End column |
| `MethodTextSpanStart` | `int` | Text span start |
| `MethodTextSpanLength` | `int` | Text span length |

**Construction:** Constructor accepts `FactoryOperation`, `IMethodSymbol`, `BaseMethodDeclarationSyntax`, and `IEnumerable<TypeAuthMethodInfo>`.

**Issues:**
1. **Location properties duplicated** - Same 7 properties as `TypeInfo` for location
2. **Mutable property** - `AuthMethodInfos` has `set` accessor
3. **`IsConstructor` is mutable** - Has `set` accessor but shouldn't change

**Clarity:** Name clearly indicates "Type's Factory Method Info" - differentiates from the `FactoryMethod` code generation class.

---

### 2.3 TypeAuthMethodInfo

**Purpose:** Information about an authorization method in an `[AuthorizeFactory<T>]` class.

**Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| *Inherited from MethodInfo* | | |
| `AuthorizeFactoryOperation` | `AuthorizeFactoryOperation` | Operations this authorizes |

**Additional Method:**
- `MakeAuthCall(FactoryMethod, StringBuilder)` - Generates authorization call code

**Issues:**
1. **Contains code generation logic** - `MakeAuthCall` method generates code, mixing data and behavior
2. **Tight coupling** - `MakeAuthCall` takes `FactoryMethod` parameter creating bidirectional dependency

**Recommendation:** Extract `MakeAuthCall` to a separate code generation utility class.

---

### 2.4 MethodInfo (Base Record)

**Purpose:** Base record providing common method information shared by `TypeFactoryMethodInfo` and `TypeAuthMethodInfo`.

**Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| `Name` | `string` | Method name |
| `ClassName` | `string` | Containing class name |
| `NamePostfix` | `string` (virtual) | Name without prefix |
| `IsNullable` | `bool` | Nullable return type |
| `IsBool` | `bool` | Returns bool |
| `IsTask` | `bool` | Returns Task |
| `IsRemote` | `bool` | Has [Remote] attribute |
| `ReturnType` | `string?` | Return type as string |
| `Parameters` | `EquatableArray<MethodParameterInfo>` | Parameters |
| `AspAuthorizeCalls` | `EquatableArray<AspAuthorizeInfo>` | ASP.NET auth attributes |

**Issues:**
1. **Mutable properties** - `Name`, `ClassName`, `AspAuthorizeCalls` have public setters
2. **`AspAuthorizeCalls` seems misplaced** - Should this be on `TypeFactoryMethodInfo` only?

---

### 2.5 MethodParameterInfo

**Purpose:** Information about a single method parameter.

**Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| `Name` | `string` | Parameter name |
| `Type` | `string` | Parameter type as string |
| `IsService` | `bool` | Has [Service] attribute |
| `IsTarget` | `bool` | Is the target parameter |

**Issues:**
1. **All properties mutable** - Has public setters on all properties
2. **Parameterless constructor** - Allows incomplete initialization
3. **Custom Equals/GetHashCode** - Only compares `Name` and `Type`, ignoring `IsService` and `IsTarget`

**Recommendation:** Make immutable with required constructor parameters.

---

### 2.6 AspAuthorizeInfo

**Purpose:** Captures ASP.NET Core `[Authorize]` or `[AspAuthorize]` attribute configuration.

**Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| `ConstructorArguments` | `EquatableArray<string>` | Positional arguments |
| `NamedArguments` | `EquatableArray<string>` | Named arguments |

**Method:**
- `ToAspAuthorizedDataText()` - Generates code string

**Issues:**
1. **Contains code generation** - `ToAspAuthorizedDataText()` generates code, mixing concerns
2. **Properties are private** - No public accessors for the arrays

**Clarity:** Name is clear but `Info` suffix is inconsistent with not being internal.

---

### 2.7 DiagnosticInfo

**Purpose:** Stores diagnostic information in an equatable format for incremental generator caching. Location objects are not equatable, so this stores serialized location data.

**Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| `DiagnosticId` | `string` | Diagnostic ID (e.g., "NF0101") |
| `FilePath` | `string` | Source file path |
| `StartLine` | `int` | Start line (0-indexed) |
| `StartColumn` | `int` | Start column |
| `EndLine` | `int` | End line |
| `EndColumn` | `int` | End column |
| `TextSpanStart` | `int` | Text span start |
| `TextSpanLength` | `int` | Text span length |
| `MessageArgs` | `EquatableArray<string>` | Format arguments |

**Issues:**
1. **Duplicated location schema** - Same 7 location properties appear in `TypeInfo` and `TypeFactoryMethodInfo`

**Recommendation:** Extract a shared `SourceLocation` record.

---

### 2.8 FactoryText

**Purpose:** Container for collecting various generated source code fragments during the generation phase.

**Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| `Delegates` | `StringBuilder` | Delegate declarations |
| `ConstructorPropertyAssignmentsLocal` | `StringBuilder` | Local constructor assignments |
| `ConstructorPropertyAssignmentsRemote` | `StringBuilder` | Remote constructor assignments |
| `PropertyDeclarations` | `StringBuilder` | Property declarations |
| `MethodsBuilder` | `StringBuilder` | Method implementations |
| `SaveMethods` | `StringBuilder` | Save method implementations |
| `InterfaceMethods` | `StringBuilder` | Interface method signatures |
| `ServiceRegistrations` | `StringBuilder` | DI service registrations |

**Issues:**
1. **Mutable container class** - All properties are mutable StringBuilders
2. **Not a data model** - This is a code generation helper, not a data model

**Clarity:** Name is clear but could be `FactoryCodeBuilder` or similar.

---

## 3. Code Generation Classes (FactoryMethod Hierarchy)

### 3.1 FactoryMethod (Abstract Base)

**Purpose:** Base class for all factory method code generators. Provides common functionality for generating method signatures, delegates, and service registrations.

**Key Properties:**
- `ServiceType`, `ImplementationType` - Type names for generation
- `Name`, `UniqueName`, `NamePostfix` - Method naming
- `AuthMethodInfos`, `AspAuthorizeInfo` - Authorization data
- `Parameters` - Method parameters
- Various computed properties: `IsSave`, `IsBool`, `IsNullable`, `IsTask`, `IsRemote`, `IsAsync`

**Key Methods:**
- `AddFactoryText(FactoryText)` - Main entry point for code generation
- `InterfaceMethods()`, `Delegates()`, `PropertyDeclarations()` - Generate code fragments
- `PublicMethod()`, `RemoteMethod()`, `LocalMethod()` - Generate method implementations
- `LocalMethodStart()` - Generate authorization checking code

**Issues:**
1. **Mixes data and behavior heavily** - Contains both data storage and code generation
2. **Large class** - ~210 lines with many responsibilities
3. **Mutable state** - Many properties have public setters

---

### 3.2 Inheritance Hierarchy

```
FactoryMethod (abstract)
    |
    +-- ReadFactoryMethod (Create, Fetch)
    |       |
    |       +-- WriteFactoryMethod (Insert, Update, Delete)
    |       |
    |       +-- InterfaceFactoryMethod
    |
    +-- SaveFactoryMethod (combined Insert+Update+Delete)
    |
    +-- CanFactoryMethod (authorization checks)
```

**Analysis:**
- `WriteFactoryMethod` extends `ReadFactoryMethod` - makes sense as write operations need read-like setup
- `InterfaceFactoryMethod` extends `ReadFactoryMethod` - reasonable for interface proxying
- `SaveFactoryMethod` and `CanFactoryMethod` directly extend `FactoryMethod` - appropriate as they have unique behavior

**Issues:**
1. **WriteFactoryMethod extends ReadFactoryMethod** - Naming suggests IS-A relationship that is questionable
2. **Significant code duplication** - `DoFactoryMethodCall()` logic repeated with variations

---

## 4. Redundancy Analysis

### 4.1 Location Information Duplication

The following location properties appear identically in three places:

**TypeInfo:**
```csharp
public string ClassFilePath { get; }
public int ClassStartLine { get; }
public int ClassStartColumn { get; }
public int ClassEndLine { get; }
public int ClassEndColumn { get; }
public int ClassTextSpanStart { get; }
public int ClassTextSpanLength { get; }
```

**TypeFactoryMethodInfo:**
```csharp
public string MethodFilePath { get; }
public int MethodStartLine { get; }
public int MethodStartColumn { get; }
public int MethodEndLine { get; }
public int MethodEndColumn { get; }
public int MethodTextSpanStart { get; }
public int MethodTextSpanLength { get; }
```

**DiagnosticInfo:**
```csharp
public string FilePath { get; }
public int StartLine { get; }
public int StartColumn { get; }
public int EndLine { get; }
public int EndColumn { get; }
public int TextSpanStart { get; }
public int TextSpanLength { get; }
```

**Recommendation:** Extract to:
```csharp
internal sealed record SourceLocation(
    string FilePath,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn,
    int TextSpanStart,
    int TextSpanLength)
{
    public static SourceLocation FromLocation(Location location)
    {
        var lineSpan = location.GetLineSpan();
        return new SourceLocation(
            lineSpan.Path ?? "",
            lineSpan.StartLinePosition.Line,
            lineSpan.StartLinePosition.Character,
            lineSpan.EndLinePosition.Line,
            lineSpan.EndLinePosition.Character,
            location.SourceSpan.Start,
            location.SourceSpan.Length);
    }
}
```

### 4.2 File Duplication

**CRITICAL ISSUE:** The file `FactoryGenerator.cs` contains duplicate type definitions that also exist in `FactoryGenerator.Types.cs`:
- `TypeInfo` - defined in both files
- `TypeFactoryMethodInfo` - defined in both files
- `TypeAuthMethodInfo` - defined in both files
- `MethodInfo` - defined in both files
- `MethodParameterInfo` - defined in both files
- `AspAuthorizeInfo` - defined in both files
- All FactoryMethod classes - defined in both files
- `FactoryText` - defined in both files

This appears to be a result of partial class refactoring where the types were extracted to a separate file but not removed from the original.

**Recommendation:** Remove duplicate definitions from `FactoryGenerator.cs` and keep only in `FactoryGenerator.Types.cs`.

### 4.3 Computed vs Stored Properties

In `ReadFactoryMethod` and subclasses, several properties are computed from `CallMethod`:
```csharp
public override bool IsSave => this.CallMethod.IsSave;
public override bool IsBool => this.CallMethod.IsBool;
public override bool IsTask => this.IsRemote || this.CallMethod.IsTask || ...;
```

This is good - these should be computed, not stored redundantly.

However, `SaveFactoryMethod` stores a reference to `WriteFactoryMethods` list and computes from it:
```csharp
public List<WriteFactoryMethod> WriteFactoryMethods { get; }
public override bool IsRemote => this.WriteFactoryMethods.Any(m => m.IsRemote);
```

This is also appropriate - aggregating from child methods.

---

## 5. Relationship Diagram

```
+------------------+     creates      +-------------------+
| SemanticModel    |----------------->| TypeInfo          |
| ClassDeclaration |                  +-------------------+
+------------------+                  | Name              |
                                      | IsPartial         |
                                      | IsInterface       |
                                      | IsStatic          |
                                      | ServiceTypeName   |
                                      | ImplementationType|
                                      | Namespace         |
                                      | UsingStatements   |
                                      | SafeHintName      |
                                      | Diagnostics       |-------+
                                      | [Location Props]  |       |
                                      +-------------------+       |
                                              |                   |
                                              | contains          |
                                              v                   v
+------------------+              +------------------------+  +------------------+
| MethodInfo       |<-------------| TypeFactoryMethodInfo  |  | DiagnosticInfo   |
+------------------+  extends     +------------------------+  +------------------+
| Name             |              | FactoryOperation       |  | DiagnosticId     |
| ClassName        |              | IsSave                 |  | MessageArgs      |
| IsNullable       |              | IsConstructor          |  | [Location Props] |
| IsBool           |              | IsStaticFactory        |  +------------------+
| IsTask           |              | AuthMethodInfos        |
| IsRemote         |              | [Location Props]       |
| ReturnType       |              +------------------------+
| Parameters       |                       |
| AspAuthorizeCalls|                       | uses in generation
+------------------+                       v
        ^                      +-----------------------+
        |                      | FactoryMethod (base)  |
        | extends              +-----------------------+
        |                      | ServiceType           |
+--------------------+         | ImplementationType    |
| TypeAuthMethodInfo |         | Name, UniqueName      |
+--------------------+         | Parameters            |
| AuthorizeFactory   |         | AuthMethodInfos       |
| Operation          |         | AspAuthorizeInfo      |
+--------------------+         +-----------------------+
        |                              ^
        | MakeAuthCall()               | extends
        +----------------------------->+
                                       |
              +------------------------+------------------------+
              |                        |                        |
   +------------------+     +-------------------+     +------------------+
   | ReadFactoryMethod|     | SaveFactoryMethod |     | CanFactoryMethod |
   +------------------+     +-------------------+     +------------------+
   | CallMethod       |     | WriteFactoryMethods|
   +------------------+     +-------------------+
           ^
           | extends
           |
  +--------+--------+
  |                 |
+------------------+  +------------------------+
|WriteFactoryMethod|  | InterfaceFactoryMethod |
+------------------+  +------------------------+
```

---

## 6. Nested vs. Top-Level Analysis

### Current Structure

Types are nested inside `FactoryGenerator` class using partial class pattern:
- `FactoryGenerator.cs` - Main generator logic + Initialize method
- `FactoryGenerator.Types.cs` - Type definitions (partial class)
- `FactoryGenerator.Transform.cs` - Transform methods (partial class)

### Arguments FOR Keeping Nested

1. **Encapsulation** - These types are purely internal implementation details
2. **Access to static methods** - Nested types can access private static methods like `FindNamespace`, `SafeHintName`
3. **Namespace clarity** - Clearly communicates these are part of FactoryGenerator
4. **Compilation isolation** - Source generators have strict requirements about dependencies

### Arguments FOR Top-Level Classes

1. **File size** - `FactoryGenerator.Types.cs` is already 1053 lines
2. **Testability** - Top-level types are easier to unit test independently
3. **Reusability** - Could potentially be shared with other generators
4. **Readability** - Smaller, focused files are easier to maintain

### Recommendation

**Keep nested but improve organization:**

1. **Keep types nested** - They are internal implementation details and benefit from encapsulation
2. **Use partial class pattern** - Already being done, but clean up duplicates
3. **Consider separating FactoryMethod hierarchy** - These are significant enough to warrant their own file:
   - `FactoryGenerator.Types.cs` - Data model records only
   - `FactoryGenerator.CodeGen.cs` - FactoryMethod hierarchy and FactoryText
   - `FactoryGenerator.Transform.cs` - Transform methods (already separate)

---

## 7. Design Recommendations

### 7.1 Critical: Remove Duplicate Definitions

**Priority: HIGH**

Remove the duplicate type definitions from `FactoryGenerator.cs`. The types should only be defined in `FactoryGenerator.Types.cs`.

### 7.2 Extract SourceLocation Record

**Priority: MEDIUM**

Create a shared `SourceLocation` record to eliminate the 7-property duplication:

```csharp
internal sealed record SourceLocation : IEquatable<SourceLocation>
{
    public string FilePath { get; }
    public int StartLine { get; }
    public int StartColumn { get; }
    public int EndLine { get; }
    public int EndColumn { get; }
    public int TextSpanStart { get; }
    public int TextSpanLength { get; }

    public SourceLocation(Location location)
    {
        var lineSpan = location.GetLineSpan();
        FilePath = lineSpan.Path ?? "";
        StartLine = lineSpan.StartLinePosition.Line;
        StartColumn = lineSpan.StartLinePosition.Character;
        EndLine = lineSpan.EndLinePosition.Line;
        EndColumn = lineSpan.EndLinePosition.Character;
        TextSpanStart = location.SourceSpan.Start;
        TextSpanLength = location.SourceSpan.Length;
    }
}
```

Then update types:
```csharp
internal record TypeInfo
{
    public SourceLocation ClassLocation { get; }
    // ... other properties
}

internal record TypeFactoryMethodInfo : MethodInfo
{
    public SourceLocation MethodLocation { get; }
    // ... other properties
}

internal sealed record DiagnosticInfo
{
    public string DiagnosticId { get; }
    public SourceLocation Location { get; }
    public EquatableArray<string> MessageArgs { get; }
}
```

### 7.3 Make Records Immutable

**Priority: MEDIUM**

Remove public setters and parameterless constructors:

```csharp
// Before
internal sealed record MethodParameterInfo
{
    public MethodParameterInfo() { }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsService { get; set; }
    public bool IsTarget { get; set; }
}

// After
internal sealed record MethodParameterInfo(
    string Name,
    string Type,
    bool IsService,
    bool IsTarget)
{
    public MethodParameterInfo WithIsTarget(bool isTarget) =>
        this with { IsTarget = isTarget };
}
```

### 7.4 Extract Code Generation from Data Records

**Priority: LOW**

Move `MakeAuthCall` from `TypeAuthMethodInfo` to a code generation utility:

```csharp
internal static class AuthCodeGenerator
{
    public static void GenerateAuthCall(
        TypeAuthMethodInfo authMethod,
        FactoryMethod inMethod,
        StringBuilder methodBuilder)
    {
        // ... existing MakeAuthCall logic
    }
}
```

Similarly, move `ToAspAuthorizedDataText()` from `AspAuthorizeInfo`.

### 7.5 Separate Code Generation Files

**Priority: LOW**

Split `FactoryGenerator.Types.cs` into:
- `FactoryGenerator.DataModels.cs` - TypeInfo, TypeFactoryMethodInfo, TypeAuthMethodInfo, MethodInfo, MethodParameterInfo, AspAuthorizeInfo, SourceLocation
- `FactoryGenerator.CodeGen.cs` - FactoryMethod hierarchy, FactoryText

### 7.6 Consider Record Primary Constructors

**Priority: LOW**

For records with simple construction, use primary constructors:

```csharp
// Current
internal sealed record DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    public string DiagnosticId { get; }
    // ... 8 more properties

    public DiagnosticInfo(
        string diagnosticId,
        // ... 8 parameters)
    {
        DiagnosticId = diagnosticId;
        // ... 8 assignments
    }
}

// Recommended
internal sealed record DiagnosticInfo(
    string DiagnosticId,
    SourceLocation Location,
    EquatableArray<string> MessageArgs) : IEquatable<DiagnosticInfo>;
```

---

## 8. Summary

### What's Working Well

1. **Partial class organization** - Separating transform and type definitions is good
2. **EquatableArray** - Proper implementation for incremental generator caching
3. **DiagnosticDescriptors** - Well-organized diagnostic definitions
4. **FactoryMethod hierarchy** - Polymorphism for different operation types works well
5. **Naming conventions** - `TypeXxxInfo` vs `FactoryMethod` clearly distinguishes data from code gen

### What Needs Improvement

1. **Remove duplicate definitions** - Critical issue with types defined in both files
2. **Extract SourceLocation** - Eliminate 21 duplicated location properties
3. **Improve immutability** - Remove public setters, require constructor initialization
4. **Separate concerns** - Move code generation logic out of data records
5. **Reduce constructor complexity** - TypeInfo constructor does too much

### Nested vs Top-Level Verdict

**Recommendation: Keep nested**

The types should remain nested inside `FactoryGenerator` because:
1. They are purely internal implementation details
2. They need access to utility methods in the generator
3. Source generators have strict isolation requirements
4. The partial class pattern already provides good file organization

However, the partial class files should be reorganized to cleanly separate data models from code generation classes.

---

## Appendix: Complete Type Listing by File

### DiagnosticInfo.cs
- `DiagnosticInfo` (sealed record)

### DiagnosticDescriptors.cs
- `DiagnosticDescriptors` (static class)

### EquatableArray.cs
- `EquatableArray<T>` (readonly struct)

### HashCode.cs
- `HashCode` (struct - polyfill)

### FactoryGenerator.cs
- `FactoryGenerator` (partial class - main)
- **DUPLICATE DEFINITIONS** of all types below

### FactoryGenerator.Types.cs
- `TypeInfo` (record)
- `TypeFactoryMethodInfo` (record : MethodInfo)
- `TypeAuthMethodInfo` (record : MethodInfo)
- `MethodInfo` (record)
- `MethodParameterInfo` (sealed record)
- `AspAuthorizeInfo` (record)
- `FactoryMethod` (abstract class)
- `ReadFactoryMethod` (class : FactoryMethod)
- `WriteFactoryMethod` (class : ReadFactoryMethod)
- `SaveFactoryMethod` (class : FactoryMethod)
- `InterfaceFactoryMethod` (class : ReadFactoryMethod)
- `CanFactoryMethod` (class : FactoryMethod)
- `FactoryText` (class)

### FactoryGenerator.Transform.cs
- Transform methods only (no types)

### MapperGenerator.cs (separate generator)
- `MapperInfo` (record)
- `MapperTypeInfo` (record)
- `MapperMethodInfo` (sealed record)
- `PropertyInfo` (sealed record)
