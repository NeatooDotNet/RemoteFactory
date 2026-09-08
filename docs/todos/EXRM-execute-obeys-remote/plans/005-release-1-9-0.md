# Release Notes and Version for v1.9.0

**Plan #:** 005
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5
**Status:** Draft
**Last Updated:** 2026-09-08
**Plan-review opt-in:** — (declared at Step 2; expect No)
**Code-review opt-in:** — (declared at Step 2; expect No)
**Branch:** exrm-005-release-1-9-0 — cut from the arc at Step 2
**PR:** —

---

## Scope

Cut v1.9.0: release notes with a prominent behavior-change section and the one-line migration rule, the release-notes index and nav order, the version in `Directory.Build.props`, and, after the arc merges, the tag and the zTreatment notification. Records the deliberate departure from the major-bump standard. Does not change code.

---

## Intent

_(Step 2)_

## Framework & Architectural Alignment

_(Step 2)_

## Constraints & Invariants

_(Step 2)_

## Steps

_(Step 2)_

## Acceptance

_(Step 2)_

## Current State (Pre-Flight)

_(Step 3)_

## Punchlist

-

## Test Evidence

_(after implementation)_

## Gate Record

_(Step 5)_

## Plan Amendments

_(append-only)_

## Notes

The zTreatment session (ztreatmentneatoo-9c) is waiting on a message that 1.9.0 is tagged before UCG-001 starts.

Release-notes conventions on record (docs recon, 2026-09-08): front matter `layout: default`, `title: "v1.9.0"`, `description`, `parent: Release Notes`, `nav_order: 1` with older pages incremented; header lines `**Released:** YYYY-MM-DD` then `**Breaking changes:**` (v1.8.1 idiom: "No — but see [Behaviour Notes]"; v1.5.0 idiom: "Yes — … See [Migration Guide]"). Behavior-change section is a bold one-line claim followed by bullets naming who is unaffected and what moved; spelled "Behaviour Changes" (v1.7.0) or "Behaviour Notes" (v1.8.x); v1.7.0 adds "What this release does not claim". `index.md`: Highlights table (newest first, one dense **Feat**/**Fix**/**Breaking**-prefixed paragraph) and the All Releases list, plus a `## Documentation` section listing changed doc, Design, and skill files. The version lives in `src/Directory.Build.props` as `PackageVersion` (`FileVersion` has drifted; punchlist row on the todo).

Pointer from EXRM-002 (2026-09-08): release notes carry a bug fix beside the behavior change — a class-level [Execute] under [AuthorizeFactory] or [AspAuthorize] declared a non-nullable factory method while returning `Authorized<T>.Result`, so the generated file failed CS8603 under TreatWarningsAsErrors and a denial returned null through a non-nullable signature; `BuildClassExecuteMethod` now mirrors the Read path (`isNullable` includes `HasAuth`). Affected both bare and [Remote] class Execute; never compiled in-repo before 002.
