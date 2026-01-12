# Duplicate Save Methods When All Persistence Operations Have CancellationToken

## Status: FIXED

**Fix commit:** Trim whitespace from parameter Type in `MethodParameterInfo`

## Root Cause

The bug was triggered by **whitespace/indentation differences** in multi-line parameter declarations.

In `MethodParameterInfo` constructor, the parameter type was captured using:
```csharp
this.Type = parameterSyntax.Type!.ToFullString();
```

The `ToFullString()` method includes leading trivia (whitespace) from the source code. When methods had different indentation patterns like:

```csharp
// Insert/Update - one indentation pattern
public async Task<PersonEntity?> Insert([Service] IPersonDbContext personContext,
                                    [Service] IPersonPhoneListFactory personPhoneModelListFactory,
                                    CancellationToken cancellationToken)

// Delete - different indentation pattern
public async Task Delete([Service] IPersonDbContext personContext,
                             CancellationToken cancellationToken)
```

The `CancellationToken` type strings became different due to leading whitespace, causing the grouping logic in `FactoryGenerator.cs` to create separate groups:

```csharp
.GroupBy(m => string.Join(",", m.Parameters.Where(m => !m.IsTarget && !m.IsService)
                                .Select(m => m.Type.ToString())))
```

This resulted in:
- One SaveFactoryMethod for Insert+Update (one whitespace pattern)
- One SaveFactoryMethod for Delete (different whitespace pattern)

Both had `Name = "Save"`, causing duplicate interface methods.

## The Fix

Added `.Trim()` to normalize the type string:

```csharp
// FactoryGenerator.Types.cs line 727
this.Type = parameterSyntax.Type!.ToFullString().Trim();
```

## Test Coverage

Added `DuplicateSaveGeneratorTest.cs` with:
- `Person_Pattern_Should_Not_Generate_Duplicate_Save_Methods` - Uses multi-line formatting with different indentation patterns (exactly matching the Neatoo Person.cs scenario)
- `Simple_Insert_Update_Delete_With_CancellationToken_No_Duplicates` - Baseline test with single-line parameters

## Original Bug Description

### Reproduction Steps

In Neatoo repo, add CancellationToken to all factory methods in `src/Examples/Person/Person.DomainModel/Person.cs`:

```csharp
[Factory]
[AuthorizeFactory<IPersonAuth>]
internal partial class Person : EntityBase<Person>, IPerson
{
    [Remote]
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonDbContext personContext,
                                    [Service] IPersonPhoneListFactory personPhoneModelListFactory,
                                    CancellationToken cancellationToken)
    { ... }

    [Remote]
    [Insert]
    public async Task<PersonEntity?> Insert([Service] IPersonDbContext personContext,
                                    [Service] IPersonPhoneListFactory personPhoneModelListFactory,
                                    CancellationToken cancellationToken)
    { ... }

    [Remote]
    [Update]
    public async Task<PersonEntity?> Update([Service] IPersonDbContext personContext,
                                    [Service] IPersonPhoneListFactory personPhoneModelListFactory,
                                    CancellationToken cancellationToken)
    { ... }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonDbContext personContext,
                             CancellationToken cancellationToken)
    { ... }
}
```

### Compilation Errors (Before Fix)

```
error CS0111: Type 'IPersonFactory' already defines a member called 'Save' with the same parameter types
error CS0111: Type 'IPersonFactory' already defines a member called 'TrySave' with the same parameter types
error CS0111: Type 'PersonFactory' already defines a member called 'Save' with the same parameter types
error CS0111: Type 'PersonFactory' already defines a member called 'TrySave' with the same parameter types
```

## Task List

- [x] Reproduce bug with concrete evidence (from Neatoo main repo)
- [x] Reproduce bug in generator unit test
- [x] Identify root cause: ToFullString() includes whitespace trivia
- [x] Fix: Trim whitespace from parameter Type in MethodParameterInfo
- [x] Verify all tests pass

## Timeline

- **Discovered:** 2026-01-11 - While adding CancellationToken to Neatoo Person example
- **Fixed:** 2026-01-11 - Whitespace trivia handling in MethodParameterInfo
