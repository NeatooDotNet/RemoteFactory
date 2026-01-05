# Documentation Examples Test Project

## Overview

Created a dedicated test project at `docs/examples/` where all documentation code snippets are compiled and tested. Uses region markers in C# with a PowerShell extraction tool to sync snippets to markdown files.

**Status:** COMPLETED

**Completed:** 2026-01-03

---

## Implementation Summary

### Files Created

| Directory | Files |
|-----------|-------|
| `docs/examples/` | `DocsExamples.csproj`, `Directory.Packages.props`, `AssemblyAttributes.cs`, `xunit.runner.json` |
| `Infrastructure/` | `DocsTestBase.cs`, `DocsContainers.cs`, `MockServices.cs` |
| `GettingStarted/` | `QuickStartTests.cs` |
| `Concepts/` | `FactoryOperationsTests.cs`, `ServiceInjectionTests.cs` |
| `Authorization/` | `AuthorizationTests.cs` |
| `Tools/` | `ExtractSnippets.ps1` |

### Extracted Snippets (19 total)

| Category | Count | Snippets |
|----------|-------|----------|
| quick-start | 4 | person-model, person-interface, person-entity, person-context-interface |
| factory-ops | 7 | create-constructor, create-method, create-static, fetch-bool, insert-update, delete, execute |
| service | 4 | basic, multiple, logging, current-user |
| auth | 4 | interface, implementation, model, denied |

### Test Coverage

- **32 tests** across all example files
- All tests pass on **net8.0, net9.0, net10.0**
- Tests verify actual behavior with assertions (not just compilation)

---

## Architecture

### Region Marker Convention

```csharp
#region docs-{section}-{snippet-name}
[Factory]
public class PersonModel : IPersonModel
{
    // Code extracted to documentation
}
#endregion docs-{section}-{snippet-name}
```

### DocsTestBase<T>

Base class providing:
- Client/server DI container setup via `DocsContainers.Scopes()`
- Automatic data store clearing between tests
- Factory resolution from client scope

### DocsContainers

Creates isolated containers simulating 3-tier architecture:
- **Server:** `NeatooFactory.Server` mode
- **Client:** `NeatooFactory.Remote` mode with simulated HTTP transport
- **Local:** `NeatooFactory.Logical` mode for in-process testing

### SharedPersonDataStore

Static storage pattern enabling data persistence across DI scope boundaries during tests.

---

## Usage

### Run Tests

```bash
dotnet test docs/examples/DocsExamples.csproj
```

### Extract Snippets

```powershell
cd docs/examples/Tools
.\ExtractSnippets.ps1

# Output: docs/snippets/snippets.json
```

### Extraction Options

```powershell
# JSON only (default)
.\ExtractSnippets.ps1

# Individual .cs files
.\ExtractSnippets.ps1 -Format files

# Both formats
.\ExtractSnippets.ps1 -Format both

# Custom output path
.\ExtractSnippets.ps1 -OutputPath "../custom/snippets"
```

---

## Key Patterns Documented

### Factory Operations
- `[Create]` - Constructor, instance method, static method
- `[Fetch]` - Bool return for nullable results
- `[Insert]` + `[Update]` - Combined upsert pattern
- `[Delete]` - Separate delete operation
- `[Execute]` - Static partial class with private underscore-prefixed methods

### Service Injection
- `[Service]` attribute excludes parameters from factory signature
- Multiple services in single method
- `ILogger<T>` injection
- `ICurrentUser` pattern for audit trails

### Authorization
- `[AuthorizeFactory<TAuth>]` attribute on domain models
- `[AuthorizeFactory(AuthorizeFactoryOperation.X)]` on auth interface methods
- `CanCreate()`, `CanFetch()`, `CanSave()`, `CanDelete()` factory methods
- `TrySave()` returns `Authorized<T>` instead of throwing
- `NotAuthorizedException` when authorization fails

---

## Issues Resolved During Implementation

| Issue | Resolution |
|-------|------------|
| NF0104 long type name | Added `[assembly: FactoryHintNameLength(100)]` |
| Data not persisting across scopes | Created `SharedPersonDataStore` with static storage |
| Execute methods NF0103 | Must be in static partial class with private `_` prefix |
| Test parallelism failures | Added `xunit.runner.json` with `parallelizeTestCollections: false` |
| Authorization tests failing | Configure mock user on both client and server scopes |
| DeniedModel NullReferenceException | Separate Read (allow) and Write (deny) authorization |

---

## Future Enhancements

- [ ] Add ThreeTierExecutionTests.cs for Server/Remote/Logical mode documentation
- [ ] Add more advanced authorization scenarios
- [ ] Integrate extraction tool into documentation build pipeline
- [ ] Add markdown template system for automatic snippet insertion
