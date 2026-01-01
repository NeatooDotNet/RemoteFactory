# Multi-Targeting Plan: .NET 8, .NET 9, and .NET 10

**Date**: 2025-12-30
**Status**: Complete
**Goal**: Build and test for .NET 8, .NET 9, and .NET 10 with all three included in NuGet packages

---

## Overview

Update the RemoteFactory solution to support multi-targeting across three .NET versions, ensuring the NuGet packages include assemblies for all target frameworks.

---

## Phase 1: Directory.Build.props (Central Configuration)

**File**: `src/Directory.Build.props`

**Change**: Update `TargetFrameworks` from `net9.0` to `net8.0;net9.0;net10.0`

```xml
<!-- Before -->
<TargetFrameworks>net9.0</TargetFrameworks>

<!-- After -->
<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
```

---

## Phase 2: Directory.Packages.props (Package Versioning)

**File**: `src/Directory.Packages.props`

**Challenge**: Microsoft.Extensions.* packages should ideally use matching versions per framework for optimal compatibility.

**Solution**: Add version 8.x entries for packages that need framework-specific versions:

```xml
<!-- Add these 8.x versions -->
<PackageVersion Include="Microsoft.Extensions.DependencyInjection.v8" Version="8.0.1" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions.v8" Version="8.0.2" />
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing.v8" Version="8.0.11" />
<PackageVersion Include="Microsoft.AspNetCore.Metadata.v8" Version="8.0.11" />
```

---

## Phase 3: RemoteFactory.csproj

**File**: `src/RemoteFactory/RemoteFactory.csproj`

### Issue 1: Hardcoded Generator.dll Path

**Problem**: Line 20 has a hardcoded path that won't work with multi-targeting:
```xml
<None Include="$(OutputPath)\net9.0\Neatoo.Generator.dll" Pack="true" PackagePath="analyzers/dotnet/cs" />
```

**Fix**: Generator targets `netstandard2.0` (no framework-specific folder):
```xml
<None Include="..\Generator\bin\$(Configuration)\netstandard2.0\Neatoo.Generator.dll" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
```

### Issue 2: Framework-Conditional Package References

**Add conditional ItemGroups**:
```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" VersionOverride="8.0.1" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' != 'net8.0'">
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
</ItemGroup>
```

---

## Phase 4: RemoteFactory.AspNetCore.csproj

**File**: `src/RemoteFactory.AspNetCore/RemoteFactory.AspNetCore.csproj`

**Status**: The `<FrameworkReference Include="Microsoft.AspNetCore.App" />` automatically resolves per target framework. No changes needed for the framework reference itself.

---

## Phase 5: Generator.csproj

**File**: `src/Generator/Generator.csproj`

**Status**: **NO CHANGES** - Must remain `netstandard2.0` (Roslyn source generator requirement).

The Generator already correctly sets:
```xml
<TargetFramework>netstandard2.0</TargetFramework>
<TargetFrameworks></TargetFrameworks>  <!-- Clears inherited multi-targeting -->
```

---

## Phase 6: Test Projects

**Files**:
- `src/Tests/FactoryGeneratorTests/FactoryGeneratorTests.csproj`
- `src/Tests/RemoteFactory.AspNet.Tests/RemoteFactory.AspNet.Tests.csproj`
- `src/Tests/RemoteFactory.AspNetCore.TestLibrary/RemoteFactory.AspNetCore.TestLibrary.csproj`
- `src/Tests/RemoteFactory.AspNetCore.TestServer/RemoteFactory.AspNetCore.TestServer.csproj`

**Status**: No explicit changes needed. Test projects inherit `TargetFrameworks` from `Directory.Build.props` and will automatically test all three frameworks (net8.0, net9.0, net10.0).

---

## Phase 7: GitHub Actions Workflow

**File**: `.github/workflows/build.yml`

**Changes needed**:

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: |
      8.0.x
      9.0.x
      10.0.x
    dotnet-quality: 'preview'  # Required for .NET 10
```

---

## Summary of Changes

| File | Change |
|------|--------|
| `Directory.Build.props` | `TargetFrameworks` → `net8.0;net9.0;net10.0` |
| `Directory.Packages.props` | Add 8.x package versions for conditional refs |
| `RemoteFactory.csproj` | Fix Generator.dll path, add conditional packages |
| `RemoteFactory.AspNetCore.csproj` | Add conditional package references if needed |
| `Generator.csproj` | No changes (stays netstandard2.0) |
| Test projects | No changes (inherit multi-targeting from Directory.Build.props) |
| `build.yml` | Install .NET 8, 9, and 10 SDKs |

---

## Expected NuGet Package Structure

After implementation, the NuGet packages will contain:

```
Neatoo.RemoteFactory.nupkg
├── lib/
│   ├── net8.0/
│   │   └── Neatoo.RemoteFactory.dll
│   ├── net9.0/
│   │   └── Neatoo.RemoteFactory.dll
│   └── net10.0/
│       └── Neatoo.RemoteFactory.dll
└── analyzers/
    └── dotnet/
        └── cs/
            └── Neatoo.Generator.dll

Neatoo.RemoteFactory.AspNetCore.nupkg
└── lib/
    ├── net8.0/
    │   └── Neatoo.RemoteFactory.AspNetCore.dll
    ├── net9.0/
    │   └── Neatoo.RemoteFactory.AspNetCore.dll
    └── net10.0/
        └── Neatoo.RemoteFactory.AspNetCore.dll
```

---

## Checklist

- [x] Update Directory.Build.props for net8.0;net9.0;net10.0
- [x] Update Directory.Packages.props with 8.x package versions
- [x] Fix RemoteFactory.csproj Generator.dll path
- [x] Add conditional package references to RemoteFactory.csproj
- [x] Update GitHub Actions to install .NET 8, 9, and 10 SDKs
- [x] Build and verify all target frameworks compile
- [x] Run tests for all three frameworks (net8.0, net9.0, net10.0)
- [x] Verify NuGet package structure contains all frameworks
- [x] Update documentation (CLAUDE.md, README.md, docs/)
