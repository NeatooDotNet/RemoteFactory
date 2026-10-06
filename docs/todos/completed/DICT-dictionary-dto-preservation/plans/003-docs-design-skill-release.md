# Docs, Design, skill, and release notes

**Plan #:** 003
**Date:** 2026-09-10
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5, AC-4
**Status:** Done
**Last Updated:** 2026-10-06
**Plan-review opt-in:** No — documentation and release bookkeeping.
**Code-review opt-in:** Yes — every edited claim is checked against the shipped walker; published docs that contradict the code are the defect this todo exists to stop.
**Branch:** dict-003-docs-design-skill-release — stacked on `dict-001-gate-dictionary-carried-dto` (PR #106), because the docs describe the walker that branch ships. Merge #106 first.
**PR:** #107, then #109 — #107 merged into the stacked branch 27 seconds after #106, before the retarget, and #109 carried it into `DICT`.

---

## Scope

Brings every artifact that describes DTO discovery into line with the shipped behavior — `docs/trimming.md`, the skill's trimming reference, `src/Design/CLAUDE-DESIGN.md`, and the Design-project comment that currently records dictionary values as a known gap — stating the shapes the walk covers as a rule rather than a list of examples, and listing plainly the shapes it does not cover: tuple elements, members whose concrete type is chosen at runtime by the `$type` interface converter, non-generic collections, and `object`-typed values, each with its explicit-preservation remedy. Then cuts the release that ships the fix: version bump, release-notes page, index row, and nav order per the CI/CD standards in the repo `CLAUDE.md`. It does not restate the v1.7.0 migration hazard, which the todo's punchlist row carries independently of the fix, and it does not touch the mdsnippets-embedded code samples unless a sample's comment states the gap.

---

## Intent

- A consumer reading the trimming docs, the skill, or the Design reference learns one rule for what the generator preserves, and one short list of what it never can, with the remedy beside each.
- A consumer upgrading across v1.7.0 is warned that a type kept alive by a leaked `[Remote]` body loses that root.
- v1.10.1 is ready to tag once v1.10.0 is published.

---

## Framework & Architectural Alignment

- `CLAUDE.md` "Creating a New Release": release-notes template, index Highlights and All Releases rows, `nav_order` 1 for the new page with the 1.x pages incremented behind it, and both `FileVersion` and `PackageVersion` bumped.
- The skill is self-contained: no links to `.cs` or `docs/` files from `skills/RemoteFactory/`.
- mdsnippets: none of the edited prose sits inside a snippet block. `docs/release-notes` is excluded from mdsnippets, so no run is needed.
- DDD documentation guidelines: no tutorial prose.

---

## Constraints & Invariants

- Every claim matches the walker at `61930d3` and the recorded observations: the DICT-002 tests, the DICT-001 trimmed harness, and the field-on-the-wire probe in `reviews/003-evidence/`.
- No mdsnippets-embedded sample changes, and no snippet marker is added.
- Design builds and its tests stay green after the comment edit.
- No consumer is named in published docs.

---

## Steps

1. Grep every artifact for claims about what the walk reaches, and build the edit list from the grep rather than from memory.
2. State the rule and the not-covered list in `docs/trimming.md`, and add the v1.7.0 upgrade hazard there.
3. Add the upgrade hazard to the v1.7.0 release notes' Migration Guide, marked as added after the release.
4. State the same rule in the skill's trimming reference, and replace its stale section on nested event types.
5. Correct the skill's service-injection claim that field state never crosses the wire.
6. State the same rule in `CLAUDE-DESIGN.md` and replace the Design-project known-gap comment; build and test Design.
7. Cut v1.10.1: release-notes page, index rows, `nav_order` shift, both version properties.
8. Re-run the claim grep and record that nothing stale survives.

---

## Acceptance

- [x] `docs/trimming.md` states the rule — public instance properties and fields, through every generic collection recursively including dictionary keys and values, with a consumer's own generic collection kept as well — and lists what the walk does not reach, each with its remedy. `[explicit-skip: doc prose, checked at code review]` · Should
- [x] The skill's trimming reference states the same rule and boundary, and no longer says nested event types need manual preservation. `[explicit-skip: doc prose, checked at code review]` · Should
- [x] `CLAUDE-DESIGN.md` and the Design-project comment state the same rule, and Design builds and tests green. `[explicit-skip: doc prose plus build/test gate]` · Should
- [x] The skill's service-injection reference says public fields cross the wire on the named path and private fields never do, matching the recorded probe. `[explicit-skip: doc prose, checked at code review]` · Should
- [x] The v1.7.0 release notes and `docs/trimming.md` state the upgrade hazard and its remedies. `[explicit-skip: doc prose, checked at code review]` · Should
- [x] v1.10.1's release notes, both index rows, the `nav_order` shift, and both version properties are in place, and the notes state the dependency on v1.10.0. `[explicit-skip: release bookkeeping]` · Should
- [x] A closing grep of the claim phrases across docs, skill, and Design finds no statement that contradicts the shipped walker. `[explicit-skip: grep recorded in reviews/003-evidence]` · Should

---

## Current State (Pre-Flight)

Walked 2026-10-06 on this branch at `61930d3`.

- **`docs/trimming.md`** — line 311 lists `IReadOnlyList<T>` as the example of what is unwrapped; line 326 says the walk covers "public instance properties" and lists `List<T>`, `IReadOnlyList<T>`, and arrays; line 328 says an entity walks "its own public property graph"; line 332 is the preserve-it-yourself paragraph; line 361 describes the event walk. No snippet markers.
- **`skills/RemoteFactory/references/trimming.md`** — line 220 describes the walk as return types only, listing single-argument collections; lines 230-256 still say nested event types are NOT preserved automatically, and the sample comment credits the base-class annotation. That contradicts line 284 of the same file and TRIM-007. No snippet markers.
- **`skills/RemoteFactory/references/service-injection.md:42`** — "Field state never crosses the wire on either path — only public-getter-and-setter properties do." The line sits between snippet blocks 9-20 and 50-61, outside both. The probe recorded in `reviews/003-evidence/field-wire-observation.txt` shows public fields DO cross on the named path: a plain entity in Named format, and a constructor-injected entity in either format. Only the ordinal path drops them.
- **`src/Design/CLAUDE-DESIGN.md`** — line 400 is the trimming FAQ row; line 923 covers unwrapping; line 927 covers nested discovery ("public instance properties"); line 929 covers entity discovery ("public property graph"); line 937 covers the event walk.
- **`src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:146-150`** — the "Known gap: Dictionary<K,V> value types are not walked" comment. No region markers in the file.
- **`docs/release-notes/v1.7.0.md`** — the Migration Guide paragraph at line 128 says consumers "may now be able to delete" linker entries; the reverse hazard belongs beside it.
- **Release** — `src/Directory.Build.props` is at 1.10.0, for a release that is tagged locally but unpushed and so not on NuGet. The 1.x pages carry `nav_order` 1 through 13. v1.10.0's page uses `**Released:**` and `**Breaking changes:**` header lines rather than the template's `**Release Date:**`.

---

## Punchlist

- [x] v1.7.0 migration hazard · `docs/release-notes/v1.7.0.md` Migration Guide and the new "Upgrading across v1.7.0" section of `docs/trimming.md` · DICT-003 branch · AC-4 · Should — pulled down from the todo at Step 2
- [x] Stale nested-event-types section · `skills/RemoteFactory/references/trimming.md` · DICT-003 branch · AC-5 · Should
- [x] Field-state claim · `skills/RemoteFactory/references/service-injection.md:42` · DICT-003 branch · AC-5 · Should

---

## Test Evidence

No bullet is test-pinned. Each is documentation or release bookkeeping, checked against the walker at code review, and the closing grep records that nothing stale survives.

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Tier confirmed |
|---|---|---|---|---|
| `docs/trimming.md` states the rule and the not-covered list with remedies | Should | `[explicit-skip: doc prose]` | New sections "What the walk reaches" and "What the walk does not reach"; nested, entity, and event paragraphs reworded; Limitations bullet added | ✓ |
| Skill trimming reference states the same rule; no manual-preservation claim for nested event types | Should | `[explicit-skip: doc prose]` | "What the DTO walk reaches", "What the DTO walk does not reach", and the rewritten nested-event-types section; sample comment corrected; summary paragraph qualified | ✓ |
| `CLAUDE-DESIGN.md` and the Design comment state the same rule; Design green | Should | `[explicit-skip: doc prose plus build/test gate]` | FAQ row, unwrapping, nested, entity, and event paragraphs plus a new "Not reachable by the walk" paragraph; `FactoryEventHandlerPattern.cs` comment replaced; `reviews/003-design-build.log` 0/0, `003-design-test.log` 103 per TFM | ✓ |
| Skill service-injection claim matches the probe | Should | `[explicit-skip: doc prose]` | `service-injection.md:42` rewritten; `reviews/003-evidence/field-wire-observation.txt` | ✓ |
| v1.7.0 notes and `docs/trimming.md` state the upgrade hazard | Should | `[explicit-skip: doc prose]` | v1.7.0 Migration Guide paragraph, marked as added after the release; `docs/trimming.md` "Upgrading across v1.7.0" | ✓ |
| v1.10.1 notes, index rows, `nav_order`, both version properties, v1.10.0 dependency | Should | `[explicit-skip: release bookkeeping]` | `docs/release-notes/v1.10.1.md` at `nav_order` 1; 1.x pages shifted to 2-14; Highlights and All Releases rows; `src/Directory.Build.props` 1.10.1 for both properties, confirmed in the built `AssemblyFileVersion`; the "Released" line states the v1.10.0 dependency | ✓ |
| Closing grep finds nothing stale | Should | `[explicit-skip: grep]` | `reviews/003-evidence/closing-claim-grep.txt` — two hits, both correct statements, dispositions recorded | ✓ |

---

## Gate Record

- Round 1 (2026-10-06): test review skipped (no bullet is test-pinned; recorded in the todo's Skipped Steps). Code review CLEAN with 3 callouts, all punched at `d2fb050`: the reach bullet excludes non-generic collection subclasses, and the v1.10.1 notes and index row state what was observed red, against what, and where. Logs: build 0 errors; unit 816, integration 628, Design 103, all passing on both TFMs. — `reviews/003-code-review.md`
- Round 2 (2026-10-06): code review CLEAN, no callouts. Done.

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

### 2026-10-06 — Carries the upgrade hazard; code review instead of test review

- **Section affected:** Serves, Scope, review opt-ins
- **Original said:** Serves AC-5 only, and leaves the v1.7.0 hazard to the punchlist row; no code review.
- **What changed:** The punchlist row moved onto this plan at Step 2 because both its files are in this plan's path, so it also serves AC-4. Code review checks every edited claim against the walker. The test review is skipped because no bullet is test-pinned.
- **Why:** Step 2 triage. A doc that contradicts the code is this todo's defect class.
- **Discovery Log:** 2026-10-06 / DICT-003

---

## Notes

- Grep the artifacts for the claim before building the doc list; TRIM's retro records five occurrences of a doc list built from an inventory instead of a grep.
- Release dependency: as of 2026-10-05 the `v1.10.0` tag is created locally but unpushed, so v1.10.0 is not on NuGet. It must be published before this plan cuts the next version, or the release notes will describe a predecessor nobody can install.
- The release date is left for the tagger. v1.10.0's page states a release date that never happened, and repeating that would be worse than an explicit placeholder.
