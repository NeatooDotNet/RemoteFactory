# Record Support Plan for RemoteFactory

## Overview

Add C# record support to RemoteFactory, enabling records (particularly Value Objects) to make round trips from client to database through the generated factory infrastructure.

## Motivation

C# records are ideal for DDD Value Objects:
- **Value-based equality** - Built-in structural comparison
- **Immutability** - `init` properties and positional syntax
- **Concise syntax** - `record Address(string Street, string City)`
- **With expressions** - Easy to create modified copies

Use cases for factory-enabled records:
- Fetch reference data (currencies, countries, status codes)
- Create value objects with server-side validation
- Complex value object construction requiring database lookups

## Current State

The generator only handles `ClassDeclarationSyntax` and `InterfaceDeclarationSyntax`:

```csharp
// FactoryGenerator.cs:20-23
predicate: static (s, _) => s is ClassDeclarationSyntax classDeclarationSyntax
```

Records use `RecordDeclarationSyntax` in Roslyn, so they are silently ignored.

## Implementation Plan

### Phase 1: Generator Changes

#### 1.1 Roslyn Type Hierarchy (Key Finding)

`ClassDeclarationSyntax` and `RecordDeclarationSyntax` share a common base class:

```
BaseTypeDeclarationSyntax
    └── TypeDeclarationSyntax (abstract)
            ├── ClassDeclarationSyntax (sealed)
            ├── InterfaceDeclarationSyntax (sealed)
            ├── RecordDeclarationSyntax (sealed)
            └── StructDeclarationSyntax (sealed)
```

Common properties on `TypeDeclarationSyntax`:
- `Identifier` - type name
- `Modifiers` - public, partial, sealed, etc.
- `TypeParameterList` - generic parameters
- `Members` - methods, properties, constructors
- `AttributeLists` - attributes on the type

**Note**: `ParameterList` (for primary constructors) is specific to `RecordDeclarationSyntax`.

#### 1.2 Update FactoryGenerator.cs Predicate

Simplify using `TypeDeclarationSyntax` as common base:

```csharp
predicate: static (s, _) =>
    s is TypeDeclarationSyntax typeDecl
    && typeDecl is (ClassDeclarationSyntax or RecordDeclarationSyntax)
    && !typeDecl.Modifiers.Any(SyntaxKind.AbstractKeyword)
    && !(typeDecl.TypeParameterList?.Parameters.Any() ?? false)
    && !typeDecl.AttributeLists.SelectMany(a => a.Attributes)
        .Any(a => a.Name.ToString() == "SuppressFactory")
```

#### 1.3 Update Transform Method

Unify class and record handling using `TypeDeclarationSyntax`:

```csharp
private static TypeInfo TransformTypeFactory(TypeDeclarationSyntax syntax, SemanticModel semanticModel)
{
    var symbol = semanticModel.GetDeclaredSymbol(syntax)
        ?? throw new Exception($"Cannot get symbol for {syntax}");

    // Check for record struct (not supported)
    if (syntax is RecordDeclarationSyntax record && record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
    {
        // Emit NF0206 diagnostic
    }

    return new TypeInfo(syntax, symbol, semanticModel);
}
```

The existing `TransformClassFactory` can be renamed to `TransformTypeFactory` and handle both.

#### 1.4 Handle Positional Records with `[Create]` on Type

Positional records like `record Person(string Name, int Age)` have:
- Auto-generated constructor with matching parameters
- Auto-generated `init` properties
- The constructor is the "primary constructor"

**Decision**: Allow `[Create]` attribute on the record type declaration itself:

```csharp
[Factory]
[Create]  // Applies to the primary constructor
public record Address(string Street, string City, string PostalCode);
```

This is equivalent to putting `[Create]` on the primary constructor but with cleaner syntax.

**Implementation**:
- Detect if type is a record with a primary constructor (has `ParameterList`)
- If `[Create]` is on the type, treat it as applying to the primary constructor
- Generate factory method using the primary constructor parameters

#### 1.5 New Diagnostic: NF0205 - Invalid `[Create]` on Type

**Error when `[Create]` is placed on a type declaration and**:
1. The type is not a record, OR
2. The type is a record but has no primary constructor

```csharp
// ERROR NF0205: [Create] on type requires a record with primary constructor
[Factory]
[Create]
public class NotARecord { }

// ERROR NF0205: [Create] on type requires a record with primary constructor
[Factory]
[Create]
public record NoParameters
{
    public string? Name { get; init; }
}

// OK: Record with primary constructor
[Factory]
[Create]
public record Address(string Street, string City);
```

#### 1.6 Exclude Record Structs

**`record struct` is explicitly NOT supported** - see [Design Constraints](../concepts/design-constraints.md).

Value types (structs) don't fit the client/server serialization model due to:
- Identity tracking issues (copied on assignment)
- Interface boxing loses type fidelity
- JSON `$ref` reference preservation doesn't work
- Factory operations expect nullable return types

### Phase 2: Test Coverage

#### 2.1 Unit Tests (FactoryGeneratorTests)

Add test cases for:
- [ ] Basic record with `[Factory]` attribute
- [ ] Positional record with `[Create]` on type declaration
- [ ] Record with explicit constructor and `[Create]` on constructor
- [ ] Record with `[Fetch]` static factory method
- [ ] Record inheriting from another record
- [ ] `record struct` is rejected with diagnostic (NF0206)
- [ ] `[Create]` on non-record class emits NF0205
- [ ] `[Create]` on record without primary constructor emits NF0205
- [ ] Record with `[SuppressFactory]` (should be ignored)

#### 2.2 Integration Tests (RemoteFactory.AspNet.Tests)

Add test cases for:
- [ ] Round-trip serialization of records
- [ ] Factory creation via HTTP endpoint
- [ ] Factory fetch via HTTP endpoint

### Phase 3: Example and Documentation

#### 3.1 Add Record Examples

Create example Value Objects in the Person example:

```csharp
// Simple positional record with [Create] on type
[Factory]
[Create]
public record Address(string Street, string City, string State, string PostalCode);

// Record with Fetch operation
[Factory]
[Create]
public record Currency(string Code, string Name, string Symbol)
{
    [Fetch]
    public static async Task<Currency> FetchByCode(
        string code,
        [Service] ICurrencyService service)
    {
        return await service.GetByCode(code);
    }
}
```

#### 3.2 Update Documentation

- Add records section to concepts documentation
- Update quick start with record example
- Document any limitations or special considerations

### Phase 4: Edge Cases and Validation

#### 4.1 Diagnostics

Add new diagnostics:
- [ ] **NF0205**: `[Create]` on type requires record with primary constructor
- [ ] **NF0206**: `record struct` not supported (value types incompatible with RemoteFactory)

Existing diagnostics apply to records:
- [ ] Generic records rejected (existing generic type check)
- [ ] Abstract records rejected (existing abstract check)
- [ ] Sealed records work normally

#### 4.2 Special Cases

Handle:
- [ ] Records with required members (C# 11+)
- [ ] Records with default parameter values
- [ ] Nested records

## Files to Modify

| File | Changes |
|------|---------|
| `src/Generator/FactoryGenerator.cs` | Add `RecordDeclarationSyntax` to predicate |
| `src/Generator/FactoryGenerator.Transform.cs` | Handle records, detect primary constructor, check for `[Create]` on type |
| `src/Generator/FactoryGenerator.Types.cs` | Add `IsRecord`, `HasPrimaryConstructor` to `TypeInfo` |
| `src/Generator/DiagnosticDescriptors.cs` | Add NF0205, NF0206 diagnostics |
| `src/Tests/FactoryGeneratorTests/*.cs` | Add record test cases |
| `docs/concepts/*.md` | Document record support |
| `docs/diagnostics/NF0205.md` | Document new diagnostic |
| `docs/diagnostics/NF0206.md` | Document new diagnostic |

## Resolved Decisions

1. **`[Create]` on positional records**: Allow `[Create]` on the record type declaration. It applies to the primary constructor.

2. **Primary constructor detection**: Check if `RecordDeclarationSyntax.ParameterList` is non-null and has parameters.

3. **Validation**: Emit NF0205 if `[Create]` is on a type that is not a record with a primary constructor.

## Success Criteria

- [ ] `[Factory]` attribute works on `record` types
- [ ] `[Create]`, `[Fetch]` operations work as expected
- [ ] Generated factory interfaces and implementations are correct
- [ ] Records serialize correctly for remote calls
- [ ] All existing tests continue to pass
- [ ] New record-specific tests pass
