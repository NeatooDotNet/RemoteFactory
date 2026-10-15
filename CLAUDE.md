# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/claude-code) when working with this repository.

## Project Overview

**Neatoo RemoteFactory** is a Roslyn source generator for 3-tier .NET applications. When the library that defines the domain objects is loaded on both tiers, the client/server seam needs no DTOs and no API controllers: it generates the factories and the serialization at compile time, and the library maps the one endpoint they go through. Persistence routing is one use of that, not the headline.

## Supported Frameworks

- **.NET 9.0** (STS)
- **.NET 10.0** (LTS)

Both frameworks are included in the NuGet packages.

## Design Source of Truth

**IMPORTANT**: Before proposing design changes or implementing features, consult the Design projects:

- **`src/Design/CLAUDE-DESIGN.md`** - Quick reference for all factory patterns and rules
- **`src/Design/Design.Domain/`** - Authoritative examples with extensive comments

The Design projects are the **single source of truth** for RemoteFactory's API. They contain:
- All three factory patterns (Class, Interface, Static) with working examples
- Heavily commented code explaining "why" and "what we didn't do"
- Tests on the real client/server containers demonstrating correct usage
- Server and client projects showing configuration
- The source of every C# snippet in `docs/`, `README.md` and the skill (`.claude/rules/design-snippets.md`)

When in doubt about how something should work, read the Design code first.

## Business Requirements

This is an open-source library, not a business application. The "business requirements" are the API contracts and behavioral guarantees. The Design projects are the requirements verification: any new feature or behavioral change must be reflected in Design project code and tests. Requirements verification (Step 7B) means confirming the Design projects have been updated to demonstrate the new/changed behavior as the source of truth going forward. Sources:

- **`src/Design/`** — Authoritative examples, comments, and passing tests
- **`docs/`** — Published documentation (Jekyll-based)

## Solution Structure

```
RemoteFactory/
├── src/
│   ├── RemoteFactory/                    # Core library (Neatoo.RemoteFactory)
│   ├── RemoteFactory.AspNetCore/         # ASP.NET Core integration
│   ├── Generator/                        # Roslyn source generator (netstandard2.0)
│   ├── Design/                           # DESIGN SOURCE OF TRUTH
│   │   ├── Design.Domain/                # All factory patterns with comments
│   │   ├── Design.Tests/                 # Tests demonstrating patterns
│   │   ├── Design.Server/                # ASP.NET Core configuration
│   │   └── Design.Client.Blazor/         # Blazor WASM configuration
│   ├── Tests/
│   │   ├── RemoteFactory.UnitTests/      # Unit tests for generator, factories, serialization
│   │   ├── RemoteFactory.IntegrationTests/ # Integration tests with ASP.NET Core
│   │   └── ...
│   └── Examples/
│       ├── Person/                       # Complete Blazor WASM example
│       └── ...
├── docs/                                 # Documentation (Jekyll-based)
├── skills/RemoteFactory/                 # Claude Code skill (stand-alone)
└── .github/workflows/                    # CI/CD (GitHub Actions)
```

## RemoteFactory Skill

The `skills/RemoteFactory/` directory contains a Claude Code skill for RemoteFactory guidance. **This skill must be completely self-contained.** It cannot reference `.cs` files, `docs/*.md` files, or any other files in the repository—only the markdown files within the skill directory itself (`SKILL.md` and `references/*.md`).

This constraint exists because the skill may be distributed separately from the repository and must work without access to the codebase.

### Skill Code Samples (MarkdownSnippets Integration)

The skill, `docs/` and `README.md` use MarkdownSnippets to embed `#region` blocks from `src/Design` (`docs/VisionAlignment.md` R-A; `.claude/rules/design-snippets.md`). Each region's behaviour is pinned by a Design test on the real client/server containers.

**Workflow for updating an embedded sample:**
1. Change the code inside the region in `src/Design`, and the Design test that pins it
2. Build and test both solutions (see Key Build Commands)
3. Run mdsnippets from the repository root to re-embed the code
4. Commit the source and the updated markdown together

**Adding a new embed — write both markers, not just the opening one.** A placeholder is an opening `snippet: my-region` comment *and* a matching `endSnippet` comment on the line after it. An opening marker alone is not an empty placeholder: under the `InPlaceOverwrite` convention mdsnippets reads it as the start of an existing embed and replaces everything up to the next `endSnippet` in the file — silently deleting whole sections when a later one exists, and failing the run outright when none does. Copy a live pair from `docs/attributes-reference.md`. The literal markers are deliberately not reproduced in this file, because mdsnippets scans it too and would treat them as a real (and missing) embed — which is the same hazard from the other side.

After any `mdsnippets` run, check `git diff --stat` for a file whose deletion count exceeds the edit you made; that is the signature of a swallowed section. Attribution-line drift in files you did not touch is expected and correct whenever a sample source file gained lines. Point the run's log outside the repository (`mdsnippets > /tmp/mdsnippets.log 2>&1`) — mdsnippets reads files under `docs/todos` despite the `mdsnippets.json` exclude list and will crash on its own open log.

**C# blocks in markdown:**
| Category | Source | How to Update |
|----------|--------|---------------|
| Every working example | A `#region` in `src/Design`, embedded by MarkdownSnippets | Edit the Design code and its test, run mdsnippets |
| Anti-pattern examples | Hand-written in markdown, labelled `// WRONG` | Edit markdown directly (intentionally wrong) |

No other C# is hand-written; what cannot be an embed or a WRONG block is prose.

**Region naming:** region names are unique across the repository, with a topic prefix (`operations-*`, `save-*`, `skill-*`, ...). `skill-*` regions are embedded only by the skill; a region without the prefix may be shared with `docs/` and `README.md`.

**Key files:**
- `src/Design/Design.Domain/` - the domain samples
- `src/Design/Design.Client.Blazor/` and `src/Design/Design.Server/` - client and server configuration samples
- `src/Design/Design.Tests/` - the tests that pin each region, and the test-side samples (container setup, HTTP error handling)
- `mdsnippets.json` - Configuration (includes `skills/RemoteFactory/` directory)

## Key Build Commands

**The main solution does not contain the Design projects.** `src/Neatoo.RemoteFactory.sln`
omits `Design.Domain`, `Design.Tests`, `Design.Server`, and `Design.Client.Blazor`; they
live in `src/Design/Design.sln`. Building or testing only the main solution after
changing Design code compiles nothing and runs the previous build's binaries — a green
run with your work absent from it. **Build and test both solutions.** If a run's test
count did not go up after you added tests, that is the symptom.

```bash
# Restore and build — BOTH solutions
dotnet build src/Neatoo.RemoteFactory.sln
dotnet build src/Design/Design.sln

# Run all tests — BOTH solutions
dotnet test src/Neatoo.RemoteFactory.sln
dotnet test src/Design/Design.sln

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

**[Remote] requires `internal`:** `[Remote] public` is a compile-time error (NF0105). Use `[Remote] internal`—the generator promotes the method to `public` on the factory interface, so clients call it through the factory. This enables IL trimming of method bodies on client assemblies.

**Constructor vs Method Injection:**
- **Constructor injection** (`[Service]` on constructor): Services available on both client and server
- **Method injection** (`[Service]` on method parameters): resolved on the tier that runs the method—the server for a `[Remote]` or `internal` method (the common case, so this is where server-only services go), the calling tier for a `public` local method such as a root `[Create]`

**When to use `[Remote]`:**
- Factory methods that are entry points from the client
- Typically aggregate root operations (Fetch, Save). A root `[Create]` is local unless creation needs something only the server has (VisionAlignment D14)
- Always paired with `internal` visibility

**When `[Remote]` is NOT needed (the common case):**
- Methods called from server-side code (most methods with method-injected services)
- Child entity operations within an aggregate
- Any method invoked after already crossing to the server

**Entity duality (VisionAlignment R-G):** An entity can be an aggregate root in one object graph and a child in another, and the factory method's signature is usually the whole difference. Design's `OrderLine` has a child `Fetch(row)`, plain `internal`, and a root `[Remote] internal Fetch(id)`.

**Runtime enforcement:** a non-`[Remote]` method runs on the tier that calls it. If the client calls one that needs the server:
- a `public` method runs on the client and fails with the standard DI resolution error on its first server-only `[Service]`;
- an `internal` method carries a server-only guard. In a client published with the `Neatoo.RemoteFactory.IsServerRuntime` switch set to `false` (the trimming setup), the guard throws `InvalidOperationException` and the trimmer removes the body. With the default (`true`), the guard passes and the method fails the way a `public` one does.

**Best practice - Blazor WASM:** Exclude server-only packages (Entity Framework Core, etc.) from client projects to enforce the boundary and reduce bundle size.

### LazyLoad Is Being Removed

`LazyLoad<T>` and `ILazyLoadFactory` are scheduled for removal (ruled 2026-10-09; see the `techdebt` issue "Remove LazyLoad<T> and ILazyLoadFactory" and `docs/VisionAlignment.md` R-K). Do not write new guidance, examples, tests or skill content against them, and exclude them from the docs and the skill as they are revised. Deferred loading of a child list is a `[Remote]` `[Fetch]` on the child list factory, called when needed.

## Testing

Tests run against both target frameworks (net9.0, net10.0).

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

1. **Assemble what actually shipped.** Start from the todo container(s) closed since the last release — their plan files, Gate Records and Discovery Log — not from the commit log:
   ```bash
   git describe --tags --abbrev=0  # Find last tag
   git log <last-tag>..HEAD --oneline
   ```
   **A `feat:`/`fix:` scan under-reports the release.** Work lands under whatever prefix its commit carried: v1.9.0's generator fix shipped inside a `test(execute):` commit and a whole documentation rewrite under `docs:`, so a prefix scan found only one of the two commits its notes list, and none of its documentation work. Use the log to *confirm* the assembled list and to fill the `Commits` section, never to derive it.

2. **Determine version bump**:
   - `BREAKING CHANGE:` or `!` suffix → Major (e.g., 0.14.0 → 1.0.0)
   - `feat:` or `perf:` → Minor (e.g., 0.1.0 → 0.2.0)
   - `fix:` only → Patch (e.g., 0.1.0 → 0.1.1)

3. **Create release notes file**: `docs/release-notes/vX.Y.Z.md`
   - Use template from `docs/release-notes/index.md`
   - Required sections: Overview, What's New, Breaking Changes, Bug Fixes, Commits
   - Include Migration Guide if breaking changes exist

4. **Update index page** (`docs/release-notes/index.md`):
   - **Highlights table**: Add if release has new features, breaking changes, or notable fixes
   - **All Releases list**: Always add (newest at top)

5. **Adjust nav_order**: new release gets `nav_order: 1`; increment the **1.x pages** behind it. The v0.x tail is frozen — 33 of those pages already share `nav_order: 3`, so renumbering them is neither possible as a sequence nor useful to a reader.

6. **Update version** in `src/Directory.Build.props` — both properties, which have drifted apart before:
   ```xml
   <FileVersion>X.Y.Z</FileVersion>
   <PackageVersion>X.Y.Z</PackageVersion>
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

## CI/CD

GitHub Actions workflow (`.github/workflows/build.yml`):
- Builds and tests on push to main and PRs
- Publishes to NuGet.org on version tags (v*)
- Supports manual workflow dispatch with version suffix for pre-releases
