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
| [v10.6.0](v10.6.0.md) | 2026-01-07 | Fire-and-forget domain events with [Event] attribute |
| [v10.5.0](v10.5.0.md) | 2026-01-04 | Constructor injection compatibility, AOT documentation |
| [v10.4.0](v10.4.0.md) | 2026-01-04 | CancellationToken support with IFactoryOnCancelled lifecycle hooks |
| [v10.3.0](v10.3.0.md) | 2026-01-03 | Comprehensive logging with CorrelationId for distributed tracing |
| [v10.2.0](v10.2.0.md) | 2026-01-01 | Ordinal serialization (40-50% smaller payloads), breaking: default format changed |

## All Releases

- [v10.6.0](v10.6.0.md) - 2026-01-07 - Fire-and-forget domain events
- [v10.5.0](v10.5.0.md) - 2026-01-04 - Constructor injection compatibility
- [v10.4.0](v10.4.0.md) - 2026-01-04 - CancellationToken support
- [v10.3.0](v10.3.0.md) - 2026-01-03 - Logging with CorrelationId
- [v10.2.0](v10.2.0.md) - 2026-01-01 - Ordinal serialization
- [v10.1.1](v10.1.1.md) - 2026-01-01 - Version metadata fix

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
