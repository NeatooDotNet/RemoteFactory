# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/claude-code) when working with this repository.

## Project Overview

**Neatoo RemoteFactory** is a Roslyn Source Generator-powered Data Mapper Factory for 3-tier .NET applications. It eliminates the need for DTOs, manual factories, and API controllers by generating everything at compile time.

## Supported Frameworks

- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0** (LTS)

All three frameworks are included in the NuGet packages.

## Design Source of Truth

**IMPORTANT**: Before proposing design changes or implementing features, consult the Design projects:

- **`src/Design/CLAUDE-DESIGN.md`** - Quick reference for all factory patterns and rules
- **`src/Design/Design.Domain/`** - Authoritative examples with extensive comments

The Design projects are the **single source of truth** for RemoteFactory's API. They contain:
- All three factory patterns (Class, Interface, Static) with working examples
- Heavily commented code explaining "why" and "what we didn't do"
- 26 passing tests demonstrating correct usage
- Server and client projects showing configuration

When in doubt about how something should work, read the Design code first.

## Solution Structure

```
RemoteFactory/
├── src/
│   ├── RemoteFactory/                    # Core library (Neatoo.RemoteFactory)
│   ├── RemoteFactory.AspNetCore/         # ASP.NET Core integration
│   ├── Generator/                        # Roslyn source generator (netstandard2.0)
│   ├── Design/                           # DESIGN SOURCE OF TRUTH
│   │   ├── Design.Domain/                # All factory patterns with comments
│   │   ├── Design.Tests/                 # 26 tests demonstrating patterns
│   │   ├── Design.Server/                # ASP.NET Core configuration
│   │   └── Design.Client.Blazor/         # Blazor WASM configuration
│   ├── Tests/
│   │   ├── RemoteFactory.UnitTests/      # Unit tests for generator, factories, serialization
│   │   ├── RemoteFactory.IntegrationTests/ # Integration tests with ASP.NET Core
│   │   └── ...
│   ├── docs/
│   │   └── reference-docs/              # Production-quality reference application (MarkdownSnippets source)
│   └── Examples/
│       ├── Person/                       # Complete Blazor WASM example
│       └── ...
├── docs/                                 # Documentation (Jekyll-based)
└── .github/workflows/                    # CI/CD (GitHub Actions)
```

## Key Build Commands

```bash
# Restore and build
dotnet build src/Neatoo.RemoteFactory.sln

# Run all tests
dotnet test src/Neatoo.RemoteFactory.sln

# Build in Release mode
dotnet build src/Neatoo.RemoteFactory.sln --configuration Release

# Create NuGet packages
dotnet pack src/RemoteFactory/RemoteFactory.csproj --configuration Release --output ./artifacts
dotnet pack src/RemoteFactory.AspNetCore/RemoteFactory.AspNetCore.csproj --configuration Release --output ./artifacts
```

## Architecture Notes

### Source Generator
- The `Generator` project **must** target `netstandard2.0` (Roslyn requirement)
- Generator.dll is packaged in `analyzers/dotnet/cs/` in the NuGet package
- Generated code appears in `obj/Debug/{tfm}/generated/`

### Multi-Targeting
- Central configuration in `src/Directory.Build.props`
- Package versions managed centrally in `src/Directory.Packages.props`
- Framework-conditional package references use `VersionOverride` with conditions

### NuGet Packages
- `Neatoo.RemoteFactory` - Core library + embedded source generator
- `Neatoo.RemoteFactory.AspNetCore` - Server-side ASP.NET Core integration

### Understanding [Remote] - Client-to-Server Boundary

**Core concept:** `[Remote]` marks entry points from the client to the server. Once execution crosses to the server, it stays there—subsequent method calls don't need `[Remote]`.

**Constructor vs Method Injection:**
- **Constructor injection** (`[Service]` on constructor): Services available on both client and server
- **Method injection** (`[Service]` on method parameters): Server-only services—the common case for most factory methods

**When to use `[Remote]`:**
- Factory methods that are entry points from the client
- Typically aggregate root operations (Create, Fetch, Save)

**When `[Remote]` is NOT needed (the common case):**
- Methods called from server-side code (most methods with method-injected services)
- Child entity operations within an aggregate
- Any method invoked after already crossing to the server

**Entity duality (Neatoo/CSLA pattern):** An entity can be an aggregate root in one object graph and a child in another. The same class may have `[Remote]` methods for aggregate root scenarios while other methods are server-only.

**Runtime enforcement:** Non-`[Remote]` methods are generated for client assemblies but result in "not-registered" DI exceptions if called—server-only services aren't in the client container.

**Best practice - Blazor WASM:** Exclude server-only packages (Entity Framework Core, etc.) from client projects to enforce the boundary and reduce bundle size.

## Testing

Tests run against all three target frameworks (net8.0, net9.0, net10.0).

### Test Projects

- **RemoteFactory.UnitTests**: Unit tests for generator, factories, serialization
- **RemoteFactory.IntegrationTests**: Integration tests with ASP.NET Core, client/server simulation

### Two DI Container Testing Pattern

This project uses a **client/server container simulation** for testing remote operations:
- `ClientServerContainers.Scopes()` creates three isolated DI containers: client, server, and local
- The client container serializes requests through a custom `MakeSerializedServerStandinDelegateRequest`
- The server container deserializes requests, executes methods, and serializes responses
- This validates the full round-trip without requiring HTTP

Key test files demonstrating this pattern:
- `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs` - Container setup
- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/` - Test target classes

## Planning Guidelines

When creating implementation plans for this project, **always evaluate** whether the following testing is applicable:

1. **Comprehensive Unit Tests**: Cover all code paths, edge cases, and error conditions
2. **Serialization Round-Trip Tests**: If the feature involves objects that cross the client/server boundary, include tests using the two DI container approach to validate serialization
3. **Diagnostic Tests**: If adding new Roslyn diagnostics, include tests that verify they are emitted correctly
4. **Integration Tests**: If the feature affects HTTP endpoints, include ASP.NET Core integration tests

Test plans should follow the existing patterns in the new test projects:
- `Theory/MemberData` for parameterized testing across containers
- `ClientServerContainers.Scopes()` for client/server/local container setup

### Release Notes Maintenance

#### Commit Conventions

Use conventional commits for automatic categorization:

| Prefix | Release Notes Section | Version Impact |
|--------|----------------------|----------------|
| `feat:` | What's New | Minor bump |
| `fix:` | Bug Fixes | Patch bump |
| `perf:` | What's New | Minor bump |
| `feat!:` or `BREAKING CHANGE:` | Breaking Changes | Major bump |
| `docs:`, `chore:`, `test:` | Omit from notes | None |

#### Creating a New Release

1. **Analyze commits since last release**:
   ```bash
   git describe --tags --abbrev=0  # Find last tag
   git log <last-tag>..HEAD --oneline
   git log <last-tag>..HEAD --format="%s" | findstr "^feat:"
   git log <last-tag>..HEAD --format="%s" | findstr "^fix:"
   ```

2. **Determine version bump**:
   - `BREAKING CHANGE:` or `!` suffix → Major (e.g., 10.0.0 → 11.0.0)
   - `feat:` or `perf:` → Minor (e.g., 10.1.0 → 10.2.0)
   - `fix:` only → Patch (e.g., 10.1.0 → 10.1.1)

3. **Create release notes file**: `docs/release-notes/vX.Y.Z.md`
   - Use template from `docs/release-notes/index.md`
   - Required sections: Overview, What's New, Breaking Changes, Bug Fixes, Commits
   - Include Migration Guide if breaking changes exist

4. **Update index page** (`docs/release-notes/index.md`):
   - **Highlights table**: Add if release has new features, breaking changes, or notable fixes
   - **All Releases list**: Always add (newest at top)

5. **Adjust nav_order**: Increment existing release page nav_orders, new release gets `nav_order: 1`

6. **Update version** in `src/Directory.Build.props`:
   ```xml
   <VersionPrefix>X.Y.Z</VersionPrefix>
   ```

7. **Commit and tag**:
   ```bash
   git add .
   git commit -m "chore: Prepare vX.Y.Z release"
   git tag -a vX.Y.Z -m "vX.Y.Z - Short description"
   git push origin main --tags
   ```

8. **Create GitHub release** (optional):
   ```bash
   gh release create vX.Y.Z --title "vX.Y.Z - Title" --notes-file docs/release-notes/vX.Y.Z.md
   ```

#### NuGet Package Links

Use this format in release notes:
```markdown
**NuGet:** [Neatoo.RemoteFactory X.Y.Z](https://nuget.org/packages/Neatoo.RemoteFactory/X.Y.Z)
```

## CI/CD

GitHub Actions workflow (`.github/workflows/build.yml`):
- Builds and tests on push to main and PRs
- Publishes to NuGet.org on version tags (v*)
- Supports manual workflow dispatch with version suffix for pre-releases
