# Plan: Remove Mapper Source Generator Feature

## Overview
Remove the mapper source generator feature, including all related code, tests, and documentation from the RemoteFactory project.

## Components to Remove

### 1. Source Generator Code

| File | Action | Notes |
|------|--------|-------|
| `src\Generator\MapperGenerator.cs` | Delete | Main mapper generator (277 lines) |
| `src\RemoteFactory\FactoryAttributes.cs` | Edit | Remove `MapperIgnoreAttribute` (lines 110-116) |

### 2. Test Files

| File | Action |
|------|--------|
| `src\Tests\FactoryGeneratorTests\Mapper\` | Delete entire directory |
| - `MapperTests.cs` | Delete |
| - `PersonMapperTests.cs` | Delete |
| - `MapperEnumTests.cs` | Delete |
| - `MapperIgnoreAttribute.cs` | Delete |
| - `MapperNullableBang.cs` | Delete |
| - `MapperAbstractGenericTests.cs` | Delete |
| `src\Tests\FactoryGeneratorSandbox\MapperClassTests.cs` | Delete |

### 3. Generated Files (will disappear after generator removal)
- `Generated\Neatoo.Generator\Neatoo.Mapper\*.g.cs` - Auto-removed when generator deleted

### 4. Documentation Files

| File | Action | Details |
|------|--------|---------|
| `docs\source-generation\mapper-generator.md` | Delete | Primary mapper documentation (510 lines) |
| `docs\source-generation\how-it-works.md` | Edit | Remove Section 4 about mappers |
| `docs\source-generation\appendix-internals.md` | Edit | Remove mapper generator section (lines 215-249) |
| `docs\reference\attributes.md` | Edit | Remove MapperIgnore references |
| `docs\reference\generated-code.md` | Edit | Remove mapper section (lines 500-533) |
| `docs\getting-started\quick-start.md` | Edit | Remove mapper examples (lines 84-85, 96, 120, 379-395) |
| `docs\getting-started\project-structure.md` | Edit | Remove mapper reference (line 398) |
| `docs\examples\common-patterns.md` | Edit | Remove mapper patterns |
| `docs\comparison\vs-dtos.md` | Edit | Remove mapper advantages references |
| `docs\comparison\vs-csla.md` | Edit | Remove mapper usage examples |
| `docs\concepts\service-injection.md` | Edit | Remove mapper usage references |
| `docs\testing\generator-testing-approach.md` | Edit | Remove mapper test references |
| `docs\DOCUMENTATION_PLAN.md` | Edit | Remove mapper references |
| `docs\index.md` | Edit | Remove mapper links and references |
| `README.md` | Edit | Remove mapper feature mentions |

## Implementation Steps

### Step 1: Delete Source Generator
1. Delete `src\Generator\MapperGenerator.cs`
2. Edit `src\RemoteFactory\FactoryAttributes.cs` to remove `MapperIgnoreAttribute`

### Step 2: Delete Test Files
1. Delete entire `src\Tests\FactoryGeneratorTests\Mapper\` directory
2. Delete `src\Tests\FactoryGeneratorSandbox\MapperClassTests.cs`

### Step 3: Delete Primary Documentation
1. Delete `docs\source-generation\mapper-generator.md`

### Step 4: Update Documentation Files
Edit each of the remaining documentation files to remove mapper references:
- `how-it-works.md` - Remove mapper section
- `appendix-internals.md` - Remove mapper internals
- `attributes.md` - Remove MapperIgnore
- `generated-code.md` - Remove mapper code examples
- `quick-start.md` - Update examples without mappers
- `project-structure.md` - Remove mapper reference
- `common-patterns.md` - Remove mapper patterns
- `vs-dtos.md` - Remove mapper comparison
- `vs-csla.md` - Remove mapper examples
- `service-injection.md` - Remove mapper usage
- `generator-testing-approach.md` - Remove mapper test references
- `DOCUMENTATION_PLAN.md` - Update plan
- `index.md` - Remove mapper links
- `README.md` - Remove mapper feature

### Step 5: Update Example Projects
Update `src\Examples\Person\Person.DomainModel\PersonModel.cs`:
- Remove partial method declarations (lines 54-55)
- Replace `MapFrom(personEntity)` call (line 66) with explicit property assignments
- Replace `MapTo(personEntity)` call (line 82) with explicit property assignments

### Step 6: Build and Verify
1. Run `dotnet build` to ensure no compilation errors
2. Run `dotnet test` to verify remaining tests pass
3. Verify generated files in `Generated\Neatoo.Mapper\` are no longer produced

## Verification Checklist
- [ ] No references to "MapTo" or "MapFrom" partial methods remain (except user code)
- [ ] No references to "MapperIgnore" attribute remain
- [ ] No references to mapper-generator.md remain
- [ ] Build succeeds
- [ ] All remaining tests pass
