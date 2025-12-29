# RemoteFactory Source Generator Diagnostics Plan

## Executive Summary

This document outlines a comprehensive strategy for surfacing diagnostic messages in the RemoteFactory source generator to improve developer experience. Currently, the generator silently skips methods that don't qualify for factory generation, making it difficult for developers to understand why their code isn't generating expected output.

---

## 1. Analysis of Current Silent Failure Points

### Current State

The generator currently uses a `List<string> messages` pattern to collect debug information that is either:
- Embedded in generated code comments (for exceptions)
- Silently discarded (for most skip scenarios)
- Never surfaced to the developer

### Existing Diagnostics

Only catastrophic exceptions are reported as diagnostics:

| ID | Location | Severity | Description |
|----|----------|----------|-------------|
| `NT0001` | `Location.None` | Error | MapperGenerator exception |
| `NT0002` | `Location.None` | Error | GenerateExecute exception |
| `NT0004` | `Location.None` | Error | GenerateFactory/GenerateInterfaceFactory exception |

**Problems with Current Approach:**
1. Diagnostics use `Location.None` - no correlation to user's source code
2. Only exceptions trigger diagnostics - intentional skips are silent
3. No diagnostic ID scheme for different issue types
4. No way for users to suppress warnings they don't care about

### Complete Inventory of Skip/Failure Scenarios

I've identified **15 distinct scenarios** where methods are skipped or issues occur:

#### Category A: User Errors (Should Be Errors or Warnings)

| # | Location | Current Message | Root Cause | Frequency |
|---|----------|-----------------|------------|-----------|
| A1 | Line 1261 | `"Class {typeInfo.Name} is not partial. Cannot generate factory."` | Missing `partial` keyword | Common |
| A2 | Line 1274 | `"{method.Name} skipped. Delegates must return Task not {method.ReturnType}"` | Execute method returns non-Task | Common |
| A3 | Line 1585 | `"Ignoring {methodSymbol.Name}; it must be static. Only static factories are allowed."` | Non-static factory returning target type | Common |
| A4 | Line 1595 | `"Ignoring {methodSymbol.Name}. Execute Operations must be a static method in a static class"` | Execute in non-static context | Common |
| A5 | Line 1697 | `"Ignoring {methodSymbol.Name}; wrong return type of {methodType} for an AuthorizeFactory method"` | Auth method with wrong return type | Occasional |
| A6 | Line 1091 | `"Multiple Insert/Update/Delete methods with the same name: {writeMethodGroup.First().Name}"` | Ambiguous save operations | Rare |
| A7 | Line 1579 | `"Ignoring {methodSymbol.Name}, Only Fetch and Create methods can return the target type"` | Write operation returns target type | Occasional |

#### Category B: Expected Skips (Should Be Info or Hidden)

| # | Location | Current Message | Root Cause | Frequency |
|---|----------|-----------------|------------|-----------|
| B1 | Line 1604 | `"Ignoring [{methodSymbol.Name}] method with attribute [{attributeName}]. Not a FactoryOperation attribute."` | Method has unrelated attributes | Very Common |
| B2 | Line 1705 | `"Ignoring [{methodSymbol.Name}] method with attribute [{attributeName}]. Not a AuthorizeFactoryOperation attribute."` | Auth class method without proper attribute | Very Common |
| B3 | Line 1718 | `"No AuthorizeFactoryAttribute"` | Class doesn't use authorization | Very Common |

#### Category C: Internal/Unexpected (Should Be Hidden or Debug Only)

| # | Location | Current Message | Root Cause | Frequency |
|---|----------|-----------------|------------|-----------|
| C1 | Line 1549 | `"No BaseMethodDeclarationSyntax for {methodSymbol.Name}"` | Compiler-generated method | Rare |
| C2 | Line 1656 | `"No MethodDeclarationSyntax for {methodSymbol.Name}"` | Compiler-generated method | Rare |
| C3 | Line 1668 | `"No AttributeSyntax for {methodSymbol.Name} {attribute.ToString()}"` | Attribute from metadata | Rare |
| C4 | Line 1713 | `"No TypeDeclarationSyntax for {authorizeAttribute}"` | External auth class | Rare |
| C5 | Line 1731 | `"Parent class: " + parentSyntax.Identifier.Text` | Debug info only | N/A |

---

## 2. Recommended Diagnostic Severity for Each Scenario

### Severity Philosophy

Based on research of Microsoft's source generators (System.Text.Json, EF Core) and Roslyn best practices:

| Severity | When to Use | User Action | IDE Behavior |
|----------|-------------|-------------|--------------|
| **Error** | Code will not work as intended; must be fixed | Required fix | Red squiggle, build may fail |
| **Warning** | Likely a mistake; should be reviewed | Should review | Yellow squiggle |
| **Info** | FYI - might be intentional | Optional | Blue suggestion |
| **Hidden** | Internal diagnostics, visible via `dotnet build` verbosity | None | Not shown in IDE |

### Recommended Severities

#### Errors (Must Fix)

| ID | Scenario | Recommended Severity | Justification |
|----|----------|---------------------|---------------|
| `NF0101` | A1: Class not partial for static Execute | **Error** | Generation literally cannot proceed |
| `NF0102` | A2: Execute method returns non-Task | **Error** | Remote execution requires Task |
| `NF0103` | A4: Execute in non-static class | **Error** | Static execute pattern requires static class |

#### Warnings (Should Review)

| ID | Scenario | Recommended Severity | Justification |
|----|----------|---------------------|---------------|
| `NF0201` | A3: Non-static factory returning target type | **Warning** | Method will be silently ignored |
| `NF0202` | A5: Auth method wrong return type | **Warning** | Authorization won't be applied |
| `NF0203` | A6: Ambiguous save operations | **Warning** | Only one will be used |
| `NF0204` | A7: Write operation returns target type | **Warning** | Unexpected pattern |

#### Info (FYI)

| ID | Scenario | Recommended Severity | Justification |
|----|----------|---------------------|---------------|
| `NF0301` | B1/B2: Method skipped - no factory attribute | **Info** (opt-in) | Expected behavior, but useful for debugging |

#### Hidden (Debug Only)

| ID | Scenario | Recommended Severity | Justification |
|----|----------|---------------------|---------------|
| `NF0401` | C1-C5: Internal diagnostic | **Hidden** | Not actionable by user |

---

## 3. Sample Diagnostic Messages (User-Friendly, Actionable)

### Error Messages

```
NF0101: Static class 'MyCommands' must be declared as partial to generate Execute delegates.
        Add the 'partial' modifier: public static partial class MyCommands

NF0102: Execute method 'ProcessData' must return Task or Task<T>, not 'void'.
        Change the return type to Task or Task<TResult> for remote execution.

NF0103: Execute method 'ProcessData' must be in a static class.
        Either make the containing class static, or use a non-Execute factory operation.
```

### Warning Messages

```
NF0201: Factory method 'Create' returns the target type 'Customer' but is not static.
        Make the method static, or the method will not be included in factory generation.

NF0202: Authorization method 'CanCreate' must return bool, string, or string? - found 'int'.
        Change the return type to indicate authorization status.

NF0203: Multiple Insert methods found with matching parameters. Only 'InsertPrimary' will be used in Save.
        Rename methods or use different parameter signatures to disambiguate.

NF0204: Method 'Delete' has Delete attribute but returns target type 'Customer'.
        Delete operations typically return void or Task. This method will be skipped.
```

### Info Messages (Opt-In Verbose Mode)

```
NF0301: Method 'HelperMethod' does not have a factory operation attribute and will not be generated.
        Add [Create], [Fetch], [Insert], [Update], [Delete], or [Execute] to include in factory generation.
```

---

## 4. Implementation Approach

### 4.1 Diagnostic Descriptor Definitions

Create a centralized `DiagnosticDescriptors.cs` file:

```csharp
namespace Neatoo.RemoteFactory.FactoryGenerator;

internal static class DiagnosticDescriptors
{
    // Category constants
    private const string CategoryUsage = "RemoteFactory.Usage";
    private const string CategoryConfiguration = "RemoteFactory.Configuration";

    // Error: Class must be partial
    public static readonly DiagnosticDescriptor ClassMustBePartial = new(
        id: "NF0101",
        title: "Class must be partial for factory generation",
        messageFormat: "Static class '{0}' must be declared as partial to generate Execute delegates",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When using static Execute operations, the containing class must be partial so the generator can add delegate definitions.",
        helpLinkUri: "https://github.com/neatoo/RemoteFactory/docs/diagnostics/NF0101.md");

    // Error: Execute must return Task
    public static readonly DiagnosticDescriptor ExecuteMustReturnTask = new(
        id: "NF0102",
        title: "Execute method must return Task",
        messageFormat: "Execute method '{0}' must return Task or Task<T>, not '{1}'",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Execute operations are designed for remote execution and must be asynchronous.");

    // Error: Execute requires static class
    public static readonly DiagnosticDescriptor ExecuteRequiresStaticClass = new(
        id: "NF0103",
        title: "Execute method requires static class",
        messageFormat: "Execute method '{0}' must be in a static class",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Warning: Factory method not static
    public static readonly DiagnosticDescriptor FactoryMethodMustBeStatic = new(
        id: "NF0201",
        title: "Factory method returning target type must be static",
        messageFormat: "Factory method '{0}' returns target type '{1}' but is not static. Method will be skipped.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Warning: Auth method wrong return type
    public static readonly DiagnosticDescriptor AuthMethodWrongReturnType = new(
        id: "NF0202",
        title: "Authorization method has invalid return type",
        messageFormat: "Authorization method '{0}' must return bool, string, or string? - found '{1}'. Method will be skipped.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Warning: Ambiguous save operations
    public static readonly DiagnosticDescriptor AmbiguousSaveOperations = new(
        id: "NF0203",
        title: "Ambiguous save operations",
        messageFormat: "Multiple {0} methods found with matching parameters. Only '{1}' will be used in Save.",
        category: CategoryConfiguration,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Warning: Write returns target type
    public static readonly DiagnosticDescriptor WriteReturnsTargetType = new(
        id: "NF0204",
        title: "Write operation should not return target type",
        messageFormat: "Method '{0}' has {1} attribute but returns target type. Only Fetch and Create can return target type.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // Info: Method skipped (verbose mode)
    public static readonly DiagnosticDescriptor MethodSkippedNoAttribute = new(
        id: "NF0301",
        title: "Method has no factory operation attribute",
        messageFormat: "Method '{0}' does not have a factory operation attribute and will not be generated",
        category: CategoryConfiguration,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false); // Opt-in via EditorConfig
}
```

### 4.2 Getting Location from User's Code

The key to good diagnostics is pointing to the user's code, not `Location.None`:

```csharp
// From IMethodSymbol - points to method declaration
Location GetMethodLocation(IMethodSymbol methodSymbol)
{
    return methodSymbol.Locations.FirstOrDefault() ?? Location.None;
}

// From ClassDeclarationSyntax - points to class declaration
Location GetClassLocation(ClassDeclarationSyntax syntax)
{
    return syntax.Identifier.GetLocation();
}

// From AttributeData - points to the attribute usage
Location GetAttributeLocation(AttributeData attribute)
{
    return attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
}

// From BaseMethodDeclarationSyntax - points to method
Location GetMethodSyntaxLocation(BaseMethodDeclarationSyntax syntax)
{
    if (syntax is MethodDeclarationSyntax methodSyntax)
        return methodSyntax.Identifier.GetLocation();
    if (syntax is ConstructorDeclarationSyntax ctorSyntax)
        return ctorSyntax.Identifier.GetLocation();
    return syntax.GetLocation();
}
```

### 4.3 Integrating into Generator Pipeline

**Important Consideration**: Diagnostics must be reported during the `RegisterSourceOutput` phase, not during `transform`. The pipeline should:

1. **Transform phase**: Collect diagnostic info into the model
2. **Output phase**: Report diagnostics and generate source

```csharp
// Enhanced TypeInfo record to carry diagnostic info
internal record TypeInfo
{
    // ... existing properties ...

    public EquatableArray<DiagnosticInfo> Diagnostics { get; init; } = [];
}

internal record DiagnosticInfo
{
    public DiagnosticDescriptor Descriptor { get; init; }
    public Location Location { get; init; }
    public object[] MessageArgs { get; init; }
}

// In RegisterSourceOutput:
context.RegisterSourceOutput(classesToGenerate, static (spc, typeInfo) =>
{
    // Report all collected diagnostics
    foreach (var diagnostic in typeInfo.Diagnostics)
    {
        spc.ReportDiagnostic(Diagnostic.Create(
            diagnostic.Descriptor,
            diagnostic.Location,
            diagnostic.MessageArgs));
    }

    // Continue with generation if no errors
    if (!typeInfo.Diagnostics.Any(d => d.Descriptor.DefaultSeverity == DiagnosticSeverity.Error))
    {
        GenerateFactory(spc, typeInfo);
    }
});
```

### 4.4 Storing Location Information

Since `Location` is not equatable (breaks incremental caching), store serializable location info:

```csharp
internal record LocationInfo
{
    public string FilePath { get; init; }
    public int StartLine { get; init; }
    public int StartColumn { get; init; }
    public int EndLine { get; init; }
    public int EndColumn { get; init; }

    public static LocationInfo FromLocation(Location location)
    {
        var lineSpan = location.GetLineSpan();
        return new LocationInfo
        {
            FilePath = lineSpan.Path,
            StartLine = lineSpan.StartLinePosition.Line,
            StartColumn = lineSpan.StartLinePosition.Character,
            EndLine = lineSpan.EndLinePosition.Line,
            EndColumn = lineSpan.EndLinePosition.Character
        };
    }

    public Location ToLocation(SyntaxTree syntaxTree)
    {
        // Reconstruct location from stored info
        var start = new LinePosition(StartLine, StartColumn);
        var end = new LinePosition(EndLine, EndColumn);
        var span = syntaxTree.GetText().Lines.GetTextSpan(new LinePositionSpan(start, end));
        return Location.Create(syntaxTree, span);
    }
}
```

---

## 5. Approaches Comparison

### Option A: All Warnings

**Description**: Report all skip scenarios as warnings.

| Pros | Cons |
|------|------|
| Simple implementation | Overwhelming for large codebases |
| Nothing is silent | Warnings become noise, ignored |
| Easy to understand | No differentiation of severity |

**Verdict**: Not recommended - causes "warning fatigue"

### Option B: Tiered Severity (Recommended)

**Description**: Errors for definite mistakes, Warnings for likely mistakes, Info for FYI.

| Pros | Cons |
|------|------|
| Clear signal for critical issues | More complex to implement |
| Suppressible warnings for intentional patterns | Requires careful categorization |
| Follows .NET ecosystem patterns | |
| Users can configure via EditorConfig | |

**Verdict**: Recommended - balances signal vs. noise

### Option C: Opt-In Verbose Mode

**Description**: Silent by default, verbose diagnostics enabled via attribute or analyzer option.

| Pros | Cons |
|------|------|
| Zero noise for experienced users | Beginners get no guidance |
| Maximum control | Hidden errors are dangerous |
| Generator stays fast | Discoverability problem |

**Verdict**: Partial adoption - use for Info-level diagnostics only

### Option D: Separate Analyzer

**Description**: Generator stays silent, companion analyzer provides diagnostics.

| Pros | Cons |
|------|------|
| Generator stays fast | Two packages to maintain |
| Analyzer can be more sophisticated | Inconsistent behavior |
| IDE-specific features (code fixes) | Users might not install analyzer |

**Verdict**: Consider for advanced features (code fixes) later

---

## 6. Developer Experience Considerations

### 6.1 "Why Didn't My Method Generate?"

**Current Experience**: Complete mystery. Developer must:
1. Guess what went wrong
2. Read documentation
3. Compare with working examples
4. Maybe find embedded error in generated code comments

**Improved Experience with Diagnostics**:
1. See warning squiggle on method
2. Hover for message: "Factory method 'Create' returns target type but is not static"
3. Understand immediately what to fix
4. Optionally suppress if intentional: `#pragma warning disable NF0201`

### 6.2 Large Codebase Scenario

**Scenario**: 50 methods in a class, only 3 have factory attributes.

**Risk**: Flooding Error List with 47 "method skipped" messages.

**Mitigation Strategy**:
- Category B (expected skips) default to **Info** and **disabled by default**
- Enable via `.editorconfig` for debugging:
  ```ini
  [*.cs]
  dotnet_diagnostic.NF0301.severity = suggestion
  ```

### 6.3 Progressive Disclosure

| User Expertise | What They See |
|----------------|---------------|
| Beginner | Errors (things that are broken) |
| Intermediate | Errors + Warnings (things that might be wrong) |
| Advanced/Debugging | Errors + Warnings + Info (everything) |

### 6.4 How Other Generators Handle This

**System.Text.Json Source Generator**:
- Uses SYSLIB1220-1229 range for diagnostics
- Errors for unsupported scenarios
- Warnings for deprecated patterns
- Comprehensive documentation for each diagnostic ID

**EF Core Model Validation**:
- Errors for invalid configurations
- Warnings for performance concerns
- Info for suggestions

**Source Generator Cookbook Guidance**:
- "Emit a warning notifying the user that generation can not proceed"
- "For code-based issues, consider implementing a diagnostic analyzer"

---

## 7. Recommended Implementation Plan

### Phase 1: Foundation (High Priority)

1. **Create `DiagnosticDescriptors.cs`** with all descriptor definitions
2. **Create `LocationInfo` record** for equatable location storage
3. **Add `Diagnostics` collection** to `TypeInfo` record
4. **Implement Error diagnostics (NF0101-NF0103)**:
   - Class must be partial
   - Execute must return Task
   - Execute requires static class

### Phase 2: Warnings (Medium Priority)

5. **Implement Warning diagnostics (NF0201-NF0204)**:
   - Factory method must be static
   - Auth method wrong return type
   - Ambiguous save operations
   - Write returns target type
6. **Add help link URLs** to documentation

### Phase 3: Polish (Lower Priority)

7. **Implement Info diagnostic (NF0301)** - opt-in verbose mode
8. **Create documentation pages** for each diagnostic ID
9. **Add code examples** showing correct patterns
10. **Consider analyzer companion** for code fixes

### Estimated Effort

| Phase | Effort | Impact |
|-------|--------|--------|
| Phase 1 | 4-6 hours | High - surfaces critical errors |
| Phase 2 | 3-4 hours | Medium - reduces debugging time |
| Phase 3 | 4-6 hours | Low - improves discoverability |

---

## 8. Diagnostic ID Scheme

### Prefix: `NF` (Neatoo Factory)

### Ranges

| Range | Category | Description |
|-------|----------|-------------|
| NF0100-NF0199 | Errors | Must be fixed for generation to work |
| NF0200-NF0299 | Warnings | Should be reviewed, may indicate bugs |
| NF0300-NF0399 | Info | FYI, useful for debugging |
| NF0400-NF0499 | Hidden | Internal diagnostics |
| NF0500-NF0599 | Reserved | Future Mapper diagnostics |
| NF0900-NF0999 | Internal | Generator exceptions |

### Complete Diagnostic Catalog

| ID | Severity | Message |
|----|----------|---------|
| NF0101 | Error | Class must be partial for factory generation |
| NF0102 | Error | Execute method must return Task |
| NF0103 | Error | Execute method requires static class |
| NF0201 | Warning | Factory method returning target type must be static |
| NF0202 | Warning | Authorization method has invalid return type |
| NF0203 | Warning | Ambiguous save operations |
| NF0204 | Warning | Write operation should not return target type |
| NF0301 | Info | Method has no factory operation attribute |
| NF0901 | Error | Generator exception (MapperGenerator) |
| NF0902 | Error | Generator exception (GenerateExecute) |
| NF0904 | Error | Generator exception (GenerateFactory) |

---

## 9. EditorConfig Support

Users can customize diagnostic behavior:

```ini
# .editorconfig

# Treat all RemoteFactory warnings as errors (strict mode)
dotnet_analyzer_diagnostic.category-RemoteFactory.Usage.severity = error

# Enable verbose mode for debugging
dotnet_diagnostic.NF0301.severity = suggestion

# Suppress specific warning for this project
dotnet_diagnostic.NF0203.severity = none
```

---

## 10. Summary and Recommendation

### Recommendation: Implement Option B (Tiered Severity)

**Rationale**:
1. Follows established .NET ecosystem patterns (System.Text.Json, EF Core)
2. Provides clear signal for critical issues without noise
3. Allows user customization via EditorConfig
4. Progressive disclosure based on user expertise
5. Future-proof - can add analyzer companion later

**Key Principles**:
- **Errors**: Only for things that are definitely broken
- **Warnings**: For things that are probably wrong
- **Info**: Opt-in for debugging "why didn't it generate"
- **Location matters**: Always point to user's code, never `Location.None`
- **Actionable messages**: Tell user what's wrong AND how to fix it

**Success Metrics**:
- Zero silent failures for user errors (Category A scenarios)
- No warning fatigue (Category B stays quiet by default)
- Every diagnostic has documentation with examples
- Users can debug generation issues without reading generator source

---

## Appendix: Source Code References

### Files to Modify

1. **New**: `src/RemoteFactory.FactoryGenerator/DiagnosticDescriptors.cs`
2. **New**: `src/RemoteFactory.FactoryGenerator/DiagnosticInfo.cs`
3. **Modify**: `src/RemoteFactory.FactoryGenerator/FactoryGenerator.cs`
   - Add `Diagnostics` property to `TypeInfo`
   - Replace `messages.Add()` with diagnostic collection
   - Report diagnostics in `RegisterSourceOutput`
4. **Modify**: `src/RemoteFactory.FactoryGenerator/MapperGenerator.cs`
   - Similar changes for mapper diagnostics

### Documentation to Create

- `docs/diagnostics/NF0101.md` - Class must be partial
- `docs/diagnostics/NF0102.md` - Execute must return Task
- (etc. for each diagnostic ID)
- `docs/diagnostics/index.md` - Diagnostic reference overview

---

## References

- [Roslyn Source Generator Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)
- [System.Text.Json Source Generator Diagnostics](https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib1220-1229)
- [Customize Roslyn Analyzer Rules](https://learn.microsoft.com/en-us/visualstudio/code-quality/use-roslyn-analyzers)
- [Roslyn Analyzers Overview](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview)
