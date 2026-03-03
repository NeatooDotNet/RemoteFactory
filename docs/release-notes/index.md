---
layout: default
title: "Release Notes"
description: "Version history and release notes for Neatoo RemoteFactory"
nav_order: 10
has_children: true
---

# Release Notes

Version history for Neatoo RemoteFactory NuGet packages.

## Highlights

Releases with new features, breaking changes, or bug fixes.

| Version | Date | Highlights |
|---------|------|------------|
| [v0.16.1](v0.16.1.md) | 2026-03-02 | Fix net10.0 forced to v9 Microsoft.Extensions packages |
| [v0.16.0](v0.16.0.md) | 2026-02-28 | Hosted Blazor WebAssembly, PersonApp renamed to Person.Client |
| [v0.15.0](v0.15.0.md) | 2026-02-26 | Version reset from v10.x to v0.x pre-release |
| [v0.14.0](v0.14.0.md) | 2026-02-26 | CanSave on IFactorySave\<T\> |
| [v0.13.0](v0.13.0.md) | 2026-02-18 | [Execute] on class factories |
| [v0.12.0](v0.12.0.md) | 2026-02-01 | CorrelationContext DI redesign, event correlation propagation fix |
| [v0.11.2](v0.11.2.md) | 2026-01-18 | Fix nullable value type syntax in ordinal serialization |
| [v0.11.1](v0.11.1.md) | 2026-01-18 | Fix nullable typeof() in ordinal serialization |
| [v0.11.0](v0.11.0.md) | 2026-01-15 | Generator refactoring, exception stack trace fix |
| [v0.10.0](v0.10.0.md) | 2026-01-15 | Support for `params` parameters in factory methods |
| [v0.9.0](v0.9.0.md) | 2026-01-13 | Client-server code separation with FactoryMode.RemoteOnly |
| [v0.8.0](v0.8.0.md) | 2026-01-13 | Optional CancellationToken on all factory methods |
| [v0.7.0](v0.7.0.md) | 2026-01-11 | Fix duplicate Save method generation with multi-line parameters |
| [v0.6.0](v0.6.0.md) | 2026-01-07 | Fire-and-forget domain events with [Event] attribute |
| [v0.5.0](v0.5.0.md) | 2026-01-04 | Constructor injection compatibility, AOT documentation |
| [v0.4.0](v0.4.0.md) | 2026-01-04 | CancellationToken support with IFactoryOnCancelled lifecycle hooks |
| [v0.3.0](v0.3.0.md) | 2026-01-03 | Comprehensive logging with CorrelationId for distributed tracing |
| [v0.2.0](v0.2.0.md) | 2026-01-01 | Ordinal serialization (40-50% smaller payloads), breaking: default format changed |

## All Releases

- [v0.16.1](v0.16.1.md) - 2026-03-02 - Fix net10.0 package versions
- [v0.16.0](v0.16.0.md) - 2026-02-28 - Hosted Blazor WebAssembly
- [v0.15.0](v0.15.0.md) - 2026-02-26 - Version reset from v10.x to v0.x
- [v0.14.0](v0.14.0.md) - 2026-02-26 - CanSave on IFactorySave\<T\>
- [v0.13.0](v0.13.0.md) - 2026-02-18 - [Execute] on class factories
- [v0.12.0](v0.12.0.md) - 2026-02-01 - CorrelationContext DI redesign
- [v0.11.2](v0.11.2.md) - 2026-01-18 - Nullable value type syntax fix
- [v0.11.1](v0.11.1.md) - 2026-01-18 - Nullable typeof() fix
- [v0.11.0](v0.11.0.md) - 2026-01-15 - Generator refactoring
- [v0.10.0](v0.10.0.md) - 2026-01-15 - Support for params parameters
- [v0.9.0](v0.9.0.md) - 2026-01-13 - Client-server code separation
- [v0.8.0](v0.8.0.md) - 2026-01-13 - Optional CancellationToken on factory methods
- [v0.7.0](v0.7.0.md) - 2026-01-11 - Parameter whitespace bug fix
- [v0.6.0](v0.6.0.md) - 2026-01-07 - Fire-and-forget domain events
- [v0.5.0](v0.5.0.md) - 2026-01-04 - Constructor injection compatibility
- [v0.4.0](v0.4.0.md) - 2026-01-04 - CancellationToken support
- [v0.3.0](v0.3.0.md) - 2026-01-03 - Logging with CorrelationId
- [v0.2.0](v0.2.0.md) - 2026-01-01 - Ordinal serialization
- [v0.1.1](v0.1.1.md) - 2026-01-01 - Version metadata fix

---

## Release Notes Template

When creating release notes for a new version:

### 1. Create the release file

Create `docs/release-notes/vX.Y.Z.md`:

```markdown
---
layout: default
title: "vX.Y.Z"
description: "Release notes for Neatoo RemoteFactory vX.Y.Z"
parent: Release Notes
nav_order: N  # Newest = 1, increment existing pages
---

# vX.Y.Z - Short Title

**Release Date:** YYYY-MM-DD
**NuGet:** [Neatoo.RemoteFactory X.Y.Z](https://nuget.org/packages/Neatoo.RemoteFactory/X.Y.Z)

## Overview

Brief 1-2 sentence summary of what this release delivers.

## What's New

- Feature additions with examples (or "None")

## Breaking Changes

- Any incompatible changes (or "None")

## Bug Fixes

- Resolved issues (or "None")

## Migration Guide

Step-by-step instructions for upgrading from previous version (if applicable).

## Commits

- `hash` - Commit message
```

### 2. Update this index

- **Highlights table**: Add if the release has new features, breaking changes, or bug fixes
- **All Releases list**: Always add (newest at top)
