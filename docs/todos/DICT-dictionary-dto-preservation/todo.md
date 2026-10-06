# DICT — Dictionary and Public-Field DTO Preservation

**ID:** DICT
**Type:** Bug
**Status:** In Progress
**Priority:** Medium
**Created:** 2026-09-10
**Last Updated:** 2026-10-05
**Initial split:** 4 plans; DICT-004 Retired 2026-10-05.
**Plan cap:** 6 — max(ceil(4 × 1.5), 4 + 2); counts plan numbers issued, including Abandoned and Retired. Issuing 007 is a stop-and-ask.
**Arc branch:** DICT — cut from main at Step 1 (repo convention, `docs/todos/CONVENTIONS.md`); every plan and punchlist branch PRs into it; it PRs into main at Step 8.

---

## Goal

The generator's DTO discovery tells the trimmer which types a trimmed client will construct, by walking types the way the reflection serializer does. It has two live blind spots. A `Dictionary<K,V>` unwraps to `KeyValuePair<K,V>`, a System type the walk rejects, so neither entry type is preserved. Public fields are skipped although the serializer runs with `IncludeFields` on. A type reachable only through either is trimmed and fails with `DeserializeNoConstructor` in the browser. The dictionary gap stayed masked until v1.7.0, because async `[Remote]` bodies leaked to the client and a `new V()` inside one rooted V by accident. zTreatment hit it on its 1.5.0 → 1.9.0 upgrade, confirmed 2026-09-10. Success: dictionary entry types and public-field types are preserved like list elements, each shape with a harness case observed red first, and the docs state what the walk covers, what it does not, and the v1.7.0 upgrade hazard.

## Acceptance Criteria

Priority words proposed by the orchestrator; the user confirms or changes them before DICT-001 is drafted. AC-7 and AC-8 were removed 2026-10-05 and their numbers are not reused.

- [ ] **AC-1** · Must — A DTO reachable only as a dictionary value type through a factory signature, a `[Factory]` entity property, or an event record property deserializes on a publish-trimmed client with no consumer LinkerConfig entry.
- [ ] **AC-2** · Could — A DTO reachable only as a dictionary key type is preserved the same way.
- [ ] **AC-3** · Must — The trimming gate carries a dictionary-valued DTO whose only construction site is an async `[Remote]` body, and that case was observed red against the pre-fix generator before the fix was trusted.
- [ ] **AC-4** · Should — The v1.7.0 release notes and `docs/trimming.md` state that upgrading across v1.7.0 removes the accidental root for DTOs constructed only inside async `[Remote]` bodies.
- [ ] **AC-5** · Should — The published docs, the skill, the Design comments, and the release notes for the shipping version describe the shapes the walk covers and the shapes it does not, per the CI/CD standards.
- [ ] **AC-6** · Should — A DTO reachable only through a public field of a walked type is preserved the same way, with a harness case observed red first.

AC-2 is Could because System.Text.Json deserializes only string, primitive, enum, and a few framework key types without a custom converter, so a DTO-typed key is rare and needs consumer work regardless.

## Out of Scope

- zTreatment's consumer-side remediation for `LocationAssessment` — a LinkerConfig entry, an explicit `DtoConstructorRegistry.Register` call, or a walked list property; zTreatment's call, one type, and it landed.
- The interface-factory leg's body-elimination proof — TRIM deferred item 20, unchanged.
- Tuple elements — removed 2026-10-05 as rare on the wire; DICT-003 lists them as not walked.
- Resolving or diagnosing interface-typed and abstract-typed members — the `$type` converter chooses the concrete type at runtime; the diagnostic was removed 2026-10-05 and DICT-003 documents the boundary instead.
- Non-generic collections, `object`-typed values, and any key type System.Text.Json cannot deserialize without a custom converter.
- Re-rooting `[Remote]` bodies on the client to restore the pre-1.7.0 behavior — the leak was the defect, not the fix.
- Replacing the walk with a consumer-authored System.Text.Json source-generation context — a serializer redesign, considered and declined 2026-09-10.

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-gate-dictionary-carried-dto](./plans/001-gate-dictionary-carried-dto.md) | Trimming gate: one red-first case per added shape | AC-3, AC-6 | Done | — |
| 002 | [002-dictionary-type-argument-walk](./plans/002-dictionary-type-argument-walk.md) | Walker covers dictionary entries and public fields | AC-1, AC-2, AC-6 | In Progress — gate round 2 | — |
| 003 | [003-docs-design-skill-release](./plans/003-docs-design-skill-release.md) | Docs, Design, skill, and release notes | AC-5 | Draft | — |
| 004 | [004-runtime-typed-member-diagnostic](./plans/004-runtime-typed-member-diagnostic.md) | Diagnostic for runtime-typed serialized members | — | Retired — removed by Goal narrowing | — |

Intended order: 001 → 002 → 003.

---

## Punchlist

- [ ] v1.7.0 migration hazard · `docs/release-notes/v1.7.0.md` Migration Guide and `docs/trimming.md` · done when both say a DTO constructed only inside an async `[Remote]` body loses its accidental client-side root across v1.7.0 and names the explicit-preservation remedies · AC-4 · Should

## Dismissed

- DICT-001 · Evidence `.txt` files carry no exit codes · each evidence README records every exit code
- DICT-002 · Shared `DiagnosticTestHelper` swallows generator exceptions as CS8785 · shared helper beyond DICT's criteria; DICT's own tests now guard themselves

---

## Discovery Log

### 2026-09-11 — DICT · serves AC-1, AC-6, AC-7, AC-8
- **Finding:** Fixing one walker shape per consumer incident is the rabbit hole; the serializer's shape set is finite, and the `$type` runtime boundary needs a build-time signal, not a walk.
- **Decision:** Re-split
- **Index changes:** Goal and criteria widened to the full shape set plus a diagnostic (user decision). AC-6, AC-7, AC-8 added; 001 and 002 widened; 004 added. Initial split 3 → 4, cap 5 → 6.
- **Follow-up:** DICT-004

### 2026-10-05 — DICT · serves AC-1, AC-6
- **Finding:** The widened todo was large for one observed failure that the consumer already fixed; the user chose dictionaries and public fields only.
- **Decision:** Re-split
- **Index changes:** Goal narrowed. AC-7 and AC-8 removed to Out of Scope; 001, 002, 003 narrowed; 004 Retired. 4 of 6 issued.
- **Follow-up:** DICT-001

### 2026-10-05 — DICT-001 · serves AC-3, AC-6
- **Finding:** Red-first cases shipped alone would leave a red harness that this plan's own gate must veto.
- **Decision:** Amend
- **Follow-up:** DICT-001 and DICT-002 share one branch and one PR; red evidence is captured at the commit before the fix.

### 2026-10-05 — DICT-002 · serves AC-1
- **Finding:** A generic type that enumerates an ever-larger version of itself never repeats, so recursive unwrapping needs a depth cap as well as a path check.
- **Decision:** Amend
- **Follow-up:** n/a — cap of 8, pinned by a test whose negative control overflowed the stack.

### 2026-10-06 — DICT-002 · serves AC-1
- **Finding:** The first cut swapped registrations for a consumer generic collection nested in a list or dictionary, keeping the element but dropping the collection.
- **Decision:** Amend
- **Follow-up:** n/a — the collection is kept as a candidate as well as unwrapped, red-first at `c381e77`.

---

## Skipped Steps

- Step 1 recon fan-out — the seams were mapped by the framework-side investigation of 2026-09-10 that created this todo: `src/Generator/DtoTypeWalker.cs` (`UnwrapType`, `WalkDtoGraph`, `WalkEntityProperties`, `WalkProperties`), `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` (`IncludeFields = true`), `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs` (runtime `$type` resolution), `src/Tests/RemoteFactory.TrimmingTests/` (`TrimTestEntity.cs`, `EntityPropertyDtoSmokeTest.cs`, `verify-trimmed.sh`), `docs/trimming.md:311-332`, and the known-gap comment at `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:146-150`. No Explore agents run.

---

## Sibling Todos

- (none)

---

## Close-Out Audit

(Filled at Step 7.)

---

## Follow-on

(Filled at Step 8.)

---

## Docs & Retro

(Filled at Step 8.)

---

## Results / Conclusions

(Filled at Step 8.)
