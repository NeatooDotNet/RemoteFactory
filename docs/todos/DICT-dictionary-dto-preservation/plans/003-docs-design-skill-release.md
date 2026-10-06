# Docs, Design, skill, and release notes

**Plan #:** 003
**Date:** 2026-09-10
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5
**Status:** Draft
**Last Updated:** 2026-10-05
**Plan-review opt-in:** No — documentation and release bookkeeping.
**Code-review opt-in:** No — no behavior change.
**Branch:** dict-003-docs-design-skill-release — cut from the arc at Step 2
**PR:** —

---

## Scope

Brings every artifact that describes DTO discovery into line with the shipped behavior — `docs/trimming.md`, the skill's trimming reference, `src/Design/CLAUDE-DESIGN.md`, and the Design-project comment that currently records dictionary values as a known gap — stating the shapes the walk covers as a rule rather than a list of examples, and listing plainly the shapes it does not cover: tuple elements, members whose concrete type is chosen at runtime by the `$type` interface converter, non-generic collections, and `object`-typed values, each with its explicit-preservation remedy. Then cuts the release that ships the fix: version bump, release-notes page, index row, and nav order per the CI/CD standards in the repo `CLAUDE.md`. It does not restate the v1.7.0 migration hazard, which the todo's punchlist row carries independently of the fix, and it does not touch the mdsnippets-embedded code samples unless a sample's comment states the gap.

---

## Intent

(Filled at Step 2.)

---

## Framework & Architectural Alignment

(Filled at Step 2.)

---

## Constraints & Invariants

(Filled at Step 2.)

---

## Steps

(Filled at Step 2.)

---

## Acceptance

(Filled at Step 2.)

---

## Current State (Pre-Flight)

(Filled at Step 3.)

---

## Punchlist

- (none)

---

## Test Evidence

(Filled before the Step 5 gate.)

---

## Gate Record

(Filled at Step 5.)

---

## Plan Amendments

### 2026-09-11 — Covers the shape set and the diagnostic

- **Section affected:** Scope
- **Original said:** Collection shapes covered; release for the fix.
- **What changed:** Also the runtime-typed boundary and the DICT-004 diagnostic, and the release ships both.
- **Why:** Todo widened 2026-09-11.
- **Discovery Log:** 2026-09-11 / DICT

### 2026-10-05 — Documents the boundary instead of a diagnostic

- **Section affected:** Scope
- **Original said:** Covers the shape set, the runtime-typed boundary, and the DICT-004 diagnostic.
- **What changed:** No diagnostic to document. The not-covered list now carries the runtime-typed boundary and tuples, each with its remedy.
- **Why:** User narrowed the todo 2026-10-05 and DICT-004 was retired.
- **Discovery Log:** 2026-10-05 / DICT

---

## Notes

- Grep the artifacts for the claim before building the doc list; TRIM's retro records five occurrences of a doc list built from an inventory instead of a grep.
- Release dependency: as of 2026-10-05 the `v1.10.0` tag is created locally but unpushed, so v1.10.0 is not on NuGet. It must be published before this plan cuts the next version, or the release notes will describe a predecessor nobody can install.
