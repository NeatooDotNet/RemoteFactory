# DICT — Dictionary and Public-Field DTO Preservation

**ID:** DICT
**Type:** Bug
**Status:** Complete
**Priority:** Medium
**Created:** 2026-09-10
**Last Updated:** 2026-10-06
**Initial split:** 4 plans; DICT-004 Retired 2026-10-05.
**Plan cap:** 6 — max(ceil(4 × 1.5), 4 + 2); counts plan numbers issued, including Abandoned and Retired. Issuing 007 is a stop-and-ask.
**Arc branch:** DICT — cut from main at Step 1 (repo convention, `docs/todos/CONVENTIONS.md`); every plan and punchlist branch PRs into it; it PRs into main at Step 8.

---

## Goal

The generator's DTO discovery tells the trimmer which types a trimmed client will construct, by walking types the way the reflection serializer does. It has two live blind spots. A `Dictionary<K,V>` unwraps to `KeyValuePair<K,V>`, a System type the walk rejects, so neither entry type is preserved. Public fields are skipped although the serializer runs with `IncludeFields` on. A type reachable only through either is trimmed and fails with `DeserializeNoConstructor` in the browser. The dictionary gap stayed masked until v1.7.0, because async `[Remote]` bodies leaked to the client and a `new V()` inside one rooted V by accident. zTreatment hit it on its 1.5.0 → 1.9.0 upgrade, confirmed 2026-09-10. Success: dictionary entry types and public-field types are preserved like list elements, each shape with a harness case observed red first, and the docs state what the walk covers, what it does not, and the v1.7.0 upgrade hazard.

## Acceptance Criteria

Priority words proposed by the orchestrator and confirmed by the user 2026-10-06, at close-out rather than before DICT-001 was drafted (recorded under Skipped Steps). AC-7 and AC-8 were removed 2026-10-05 and their numbers are not reused.

- [x] **AC-1** · Must — A DTO reachable only as a dictionary value type through a factory signature, a `[Factory]` entity property, or an event record property deserializes on a publish-trimmed client with no consumer LinkerConfig entry.
- [x] **AC-2** · Could — A DTO reachable only as a dictionary key type is preserved the same way.
- [x] **AC-3** · Must — The trimming gate carries a dictionary-valued DTO whose only construction site is an async `[Remote]` body, and that case was observed red against the pre-fix generator before the fix was trusted.
- [x] **AC-4** · Should — The v1.7.0 release notes and `docs/trimming.md` state that upgrading across v1.7.0 removes the accidental root for DTOs constructed only inside async `[Remote]` bodies.
- [x] **AC-5** · Should — The published docs, the skill, the Design comments, and the release notes for the shipping version describe the shapes the walk covers and the shapes it does not, per the CI/CD standards.
- [x] **AC-6** · Should — A DTO reachable only through a public field of a walked type is preserved the same way, with a harness case observed red first.

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
| 001 | [001-gate-dictionary-carried-dto](./plans/001-gate-dictionary-carried-dto.md) | Trimming gate: one red-first case per added shape | AC-3, AC-6 | Done | #106 |
| 002 | [002-dictionary-type-argument-walk](./plans/002-dictionary-type-argument-walk.md) | Walker covers dictionary entries and public fields | AC-1, AC-2, AC-6 | Done | #106 |
| 003 | [003-docs-design-skill-release](./plans/003-docs-design-skill-release.md) | Docs, Design, skill, and release notes | AC-5, AC-4 | Done | #107, #109 |
| 004 | [004-runtime-typed-member-diagnostic](./plans/004-runtime-typed-member-diagnostic.md) | Diagnostic for runtime-typed serialized members | — | Retired — removed by Goal narrowing | — |

Intended order: 001 → 002 → 003.

---

## Punchlist

- (none open. The v1.7.0 migration-hazard row moved down onto DICT-003 at its Step 2, because both its files are in that plan's path.)

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

### 2026-10-06 — DICT-003 · serves AC-4, AC-5
- **Finding:** The migration-hazard row lies in DICT-003's path. A probe also showed the skill's "field state never crosses the wire" claim is false for public fields on the named path.
- **Decision:** Amend
- **Follow-up:** DICT-003 carries the row and two skill corrections; code review checks claims, test review skipped.

---

## Skipped Steps

- Step 1 recon fan-out — the seams were mapped by the framework-side investigation of 2026-09-10 that created this todo: `src/Generator/DtoTypeWalker.cs` (`UnwrapType`, `WalkDtoGraph`, `WalkEntityProperties`, `WalkProperties`), `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` (`IncludeFields = true`), `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs` (runtime `$type` resolution), `src/Tests/RemoteFactory.TrimmingTests/` (`TrimTestEntity.cs`, `EntityPropertyDtoSmokeTest.cs`, `verify-trimmed.sh`), `docs/trimming.md:311-332`, and the known-gap comment at `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:146-150`. No Explore agents run.
- Priority-word confirmation before DICT-001 — the words stayed proposed through all three plans and were confirmed unchanged by the user at close-out, 2026-10-06. No plan's scope or gate depended on a word changing.
- DICT-003 Step 5 test-reviewer — a documentation-and-release plan whose every Acceptance bullet is `[explicit-skip]`, so nothing is test-pinned for it to check. The opted-in code review checks each edited claim against the shipped walker instead.

---

## Sibling Todos

- (none)

---

## Close-Out Audit

### 2026-10-06 — Grade: A

**Veto-tier findings:** None. The arc head is green after the main merge: both solutions build with 0 errors. Unit (816) and integration (628, 5 skipped) tests pass, as do Design's 103, on net9.0 and net10.0.
**Callouts:** None that affect a criterion. The 2026-09-11 Discovery Log entry is about 64 words against a 60-word budget and is accepted. The audit marked the bare "DICT-003 commits" line in the v1.10.1 notes as theoretical and did not triage it. It now carries the SHAs.
**Accepted gaps (B only):** n/a. The grade is A, and every criterion traces to code and evidence.
**User acknowledgment:** 2026-10-06. Acknowledged; proceed to Step 8.
**Full audit:** [`reviews/close-out-audit.md`](./reviews/close-out-audit.md)

---

## Follow-on

- Push the `v1.10.0` tag so it reaches NuGet. Then tag `v1.10.1` and replace its "Not yet" Released line and both "Unreleased" index dates · `docs/release-notes/` · DICT-003 · user's
- Sorted, concurrent, and immutable dictionaries, `Dto[][]`, and `List<MyStruct?>` are reached by the walker but no test pins them. The unit-test helper references CoreLib only · `DictionaryAndFieldDtoDiscoveryTests.cs` · close-out audit · Could
- A doc comment still says the walk covers "public properties of discovered DTOs" · `src/Generator/FactoryGenerator.Types.cs:807-808` · close-out audit · Could
- Intermittent `MSB3552` recurred during DICT-002's round-2 build. Its log is kept as `reviews/001-002-round2-build-msb3552-flake.log` · [#94](https://github.com/NeatooDotNet/RemoteFactory/issues/94) · DICT-002 · Could
- The shared `DiagnosticTestHelper` swallows generator exceptions as CS8785. It was dismissed here as beyond DICT's criteria, but the gap stands for every other test that uses it · `src/Tests/RemoteFactory.UnitTests` · DICT-002 test review · Could

---

## Docs & Retro

**Documentation:** shipped in DICT-003, as a plan of its own after the walker change.
- `docs/trimming.md` gained "What the walk reaches", "What the walk does not reach" (each shape with both remedies), and "Upgrading across v1.7.0".
- The v1.7.0 Migration Guide gained the reverse hazard, marked as added after the release.
- The skill's trimming reference states the same rule and lost its stale nested-event section.
- The skill's service-injection reference now says public fields cross on the named path, which the probe recorded in `reviews/003-evidence/` showed.
- `CLAUDE-DESIGN.md` and the Design comment in `FactoryEventHandlerPattern.cs` describe the shipped walk.
- v1.10.1's notes, index rows, `nav_order` shift, and version properties are cut but not tagged.

**Retro (one paragraph):** Four plans were issued against a cap of 6. Three are Done and one was Retired. The real cost was scope, not implementation. The todo was opened for one consumer failure, widened to the serializer's whole shape set plus a diagnostic, and then narrowed back to dictionaries and public fields. Each turn came from the user asking whether the walk is a rabbit hole. The narrowed answer held: the walk reaches what can be known statically, and the docs now list the rest with the remedy beside each entry, so a consumer is never surprised again. The red-first discipline paid three times:
- The trimmed harness reproduced the consumer's exact exception before the walker changed.
- The depth cap was pinned by a negative control that overflowed the stack.
- The code review's catch, that the first cut swapped which half of `List<PagedList<Dto>>` was registered, went in as a red test before the fix.

The doc review paid as well. Its round-1 callouts were all claims that overstated the evidence ("observed red" over 23 tests when 15 had been, "CI" for a local run). Those slips are the ones a writer makes about their own work, and a reviewer catches them cheaply. One process miss: the stacked PR #107 merged into its base 27 seconds after that base merged, before GitHub retargeted it, and #109 had to carry it into the arc. Next time, open a stacked plan PR against the arc only after its predecessor merges, or say in the PR to merge only after retargeting. Second process miss: the priority words stayed proposed until close-out. No gate turned on them, but the confirmation belongs before the first plan.

---

## Results / Conclusions

```
Plans: 4 issued of 6 cap — 3 Done, 0 Abandoned, 1 Retired (004, Goal narrowing).
Punchlist: 1 closed (pulled down into DICT-003). Dismissed: 2. Follow-on: 5.
Gates: 5 review files — 2 test reviews, 2 code reviews, 1 close-out audit.
       DICT-001 closed in one round; DICT-002 and DICT-003 each closed at round 2.
Issues recurred, not filed: #94.
Close-Out Audit: Grade A (acknowledged 2026-10-06).
Arc: DICT → main, PR #110.
```
