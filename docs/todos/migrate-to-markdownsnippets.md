# Migrate to MarkdownSnippets

## Overview

Migrate from the custom `extract-snippets.ps1` approach to the standard MarkdownSnippets tool (`dotnet mdsnippets`) as described in the docs-snippets skill.

## Current State

- **Custom script**: `scripts/extract-snippets.ps1` handles extraction
- **Region format**: `#region docs:{path}:{snippet-id}` (path-prefixed)
- **Markdown format**: `<!-- snippet: docs:{path}:{id} -->` (HTML comments)
- **Active snippets**: 54 references in markdown, only 3 extracted from source
- **Samples location**: `docs/samples/` (correct structure already exists)

## Target State

- **Tool**: `dotnet mdsnippets` (MarkdownSnippets.Tool)
- **Region format**: `#region {snippet-id}` (simple, globally unique)
- **Markdown format**: `snippet: {id}` (line placeholder, MarkdownSnippets fills in)
- **All snippets**: Extracted from compiled code in `docs/samples/`

## Tasks

### 1. Install MarkdownSnippets Tool

- [ ] Create `.config/dotnet-tools.json` manifest
- [ ] Install MarkdownSnippets.Tool: `dotnet tool install MarkdownSnippets.Tool`

### 2. Create Configuration

- [ ] Create `mdsnippets.json` at repo root:
  ```json
  {
    "Convention": "InPlaceOverwrite",
    "LinkFormat": "GitHub",
    "OmitSnippetLinks": true
  }
  ```

### 3. Migrate Region Markers

Convert all `#region docs:{path}:{id}` to `#region {id}`:

**Files to update:**
- `src/Examples/OrderEntry/OrderEntry.Domain/Order.cs`
- `src/Examples/OrderEntry/OrderEntry.Domain/OrderLine.cs`
- `src/Examples/OrderEntry/OrderEntry.Domain.Client/AssemblyAttributes.cs`
- `docs/samples/RemoteFactory.Samples.Server/ServerConfigSamples.cs`
- `docs/samples/RemoteFactory.Samples.BlazorClient/HttpClientConfigSamples.cs`
- `docs/samples/RemoteFactory.Samples.DomainModel.Tests/FactoryModeSamples.cs`
- `docs/samples/RemoteFactory.Samples.Server/Program.cs`
- `docs/samples/RemoteFactory.Samples.BlazorClient/Program.cs`
- All files in `docs/samples/RemoteFactory.Samples.DomainModel/`

**Naming convention**: Use globally unique names like `order-entity`, `server-configuration`, etc.

### 4. Migrate Markdown Files

Convert snippet references from HTML comments to line placeholders:

**From:**
```markdown
<!-- snippet: docs:concepts/client-server-separation:order-entity -->
```csharp
// code here
```
<!-- /snippet -->
```

**To:**
```markdown
snippet: order-entity
```

**Files to update:**
- `docs/concepts/client-server-separation.md`
- `docs/concepts/factory-operations.md`
- `docs/concepts/service-injection.md`
- `docs/concepts/three-tier-execution.md`
- `docs/reference/factory-modes.md`
- `docs/getting-started/quick-start.md`

**Keep `pseudo:` markers** for illustrative code that can't be compiled.

### 5. Create Verification Script

- [ ] Create `scripts/verify-code-blocks.ps1` that:
  - Checks all `snippet: {id}` have matching `#region {id}`
  - Checks all `<!-- pseudo: -->` have closing `<!-- /snippet -->`
  - Flags unmarked code blocks

### 6. Update CI/CD

- [ ] Add to `.github/workflows/build.yml`:
  ```yaml
  - name: Run MarkdownSnippets
    run: dotnet mdsnippets

  - name: Verify docs unchanged
    run: |
      if [ -n "$(git status --porcelain docs/)" ]; then
        echo "Documentation out of sync - run 'dotnet mdsnippets'"
        exit 1
      fi
  ```

### 7. Cleanup

- [ ] Archive or remove `scripts/extract-snippets.ps1`
- [ ] Update CLAUDE.md with new snippet workflow

## Critical Files

| Action | File |
|--------|------|
| Create | `mdsnippets.json` |
| Create | `.config/dotnet-tools.json` |
| Create | `scripts/verify-code-blocks.ps1` |
| Remove | `scripts/extract-snippets.ps1` |
| Update | All `docs/samples/**/*.cs` files (region markers) |
| Update | All `docs/**/*.md` files (snippet markers) |

## Verification

1. `dotnet build docs/samples/` - Samples compile
2. `dotnet test docs/samples/` - Tests pass
3. `dotnet mdsnippets` - Snippets sync successfully
4. `pwsh scripts/verify-code-blocks.ps1` - All code blocks have markers
5. `git diff --exit-code docs/` - No uncommitted changes after sync

## Estimated Scope

- ~10 source files with region markers to update
- ~6 markdown files with snippet references to update
- ~54 snippet references to migrate
- 3 new config/script files to create
- 1 old script to remove
