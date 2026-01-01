# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/claude-code) when working with this repository.

## Project Overview

**Neatoo RemoteFactory** is a Roslyn Source Generator-powered Data Mapper Factory for 3-tier .NET applications. It eliminates the need for DTOs, manual factories, and API controllers by generating everything at compile time.

## Supported Frameworks

- **.NET 8.0** (LTS)
- **.NET 9.0** (STS)
- **.NET 10.0** (LTS)

All three frameworks are included in the NuGet packages.

## Solution Structure

```
RemoteFactory/
├── src/
│   ├── RemoteFactory/                    # Core library (Neatoo.RemoteFactory)
│   ├── RemoteFactory.AspNetCore/         # ASP.NET Core integration
│   ├── Generator/                        # Roslyn source generator (netstandard2.0)
│   ├── Tests/
│   │   ├── FactoryGeneratorTests/        # Unit tests for generator
│   │   ├── RemoteFactory.AspNet.Tests/   # Integration tests
│   │   └── ...
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

## Testing

Tests run against all three target frameworks (net8.0, net9.0, net10.0):
- **FactoryGeneratorTests**: Unit tests for the Roslyn source generator
- **RemoteFactory.AspNet.Tests**: Integration tests with ASP.NET Core

### Two DI Container Testing Pattern

This project uses a **client/server container simulation** for testing remote operations:
- `ClientServerContainers.Scopes()` creates three isolated DI containers: client, server, and local
- The client container serializes requests through `NeatooJsonSerializer`
- The server container deserializes requests, executes methods, and serializes responses
- This validates the full round-trip without requiring HTTP

Key test files demonstrating this pattern:
- `src/Tests/FactoryGeneratorTests/ClientServerContainers.cs` - Container setup
- `src/Tests/FactoryGeneratorTests/FactoryTestBase.cs` - Base class for factory tests
- `src/Tests/FactoryGeneratorTests/Factory/RemoteWriteTests.cs` - Example using Theory/MemberData

## Planning Guidelines

When creating implementation plans for this project, **always evaluate** whether the following testing is applicable:

1. **Comprehensive Unit Tests**: Cover all code paths, edge cases, and error conditions
2. **Serialization Round-Trip Tests**: If the feature involves objects that cross the client/server boundary, include tests using the two DI container approach to validate serialization
3. **Diagnostic Tests**: If adding new Roslyn diagnostics, include tests that verify they are emitted correctly
4. **Integration Tests**: If the feature affects HTTP endpoints, include ASP.NET Core integration tests

Test plans should follow the existing patterns in `FactoryGeneratorTests/` using:
- `FactoryTestBase<TFactory>` for client/server container setup
- `Theory/MemberData` for parameterized testing across containers
- Reflection-based validation for generated factory methods

## Documentation

Documentation is in `/docs/` using Jekyll format:
- `docs/index.md` - Main entry point
- `docs/getting-started/` - Installation, quick start, project structure
- `docs/concepts/` - Architecture, factory operations, service injection
- `docs/authorization/` - Authorization approaches
- `docs/reference/` - Attributes, interfaces, generated code
- `docs/release-notes/` - Version history and release notes

### Release Notes Maintenance

When creating a new release:

1. **Create release notes file**: `docs/release-notes/vX.Y.Z.md` using the template in `docs/release-notes/index.md`
2. **Update index page** (`docs/release-notes/index.md`):
   - **Highlights table**: Add if the release has new features, breaking changes, or bug fixes
   - **All Releases list**: Always add (newest at top)
3. **Adjust nav_order**: Increment existing release page nav_orders, new release gets `nav_order: 1`
4. **Required sections**: Overview, What's New, Breaking Changes, Bug Fixes, Migration Guide (if applicable), Commits
5. **Timing**: Create release notes before tagging the version

## CI/CD

GitHub Actions workflow (`.github/workflows/build.yml`):
- Builds and tests on push to main and PRs
- Publishes to NuGet.org on version tags (v*)
- Supports manual workflow dispatch with version suffix for pre-releases
