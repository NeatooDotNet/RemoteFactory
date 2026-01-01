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

## Documentation

Documentation is in `/docs/` using Jekyll format:
- `docs/index.md` - Main entry point
- `docs/getting-started/` - Installation, quick start, project structure
- `docs/concepts/` - Architecture, factory operations, service injection
- `docs/authorization/` - Authorization approaches
- `docs/reference/` - Attributes, interfaces, generated code

## CI/CD

GitHub Actions workflow (`.github/workflows/build.yml`):
- Builds and tests on push to main and PRs
- Publishes to NuGet.org on version tags (v*)
- Supports manual workflow dispatch with version suffix for pre-releases
