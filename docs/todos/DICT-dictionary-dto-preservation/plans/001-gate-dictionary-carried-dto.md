# Trimming gate: one red-first case per added shape

**Plan #:** 001
**Date:** 2026-09-10
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-6
**Status:** In Progress
**Last Updated:** 2026-10-05
**Plan-review opt-in:** No — harness-only; no generator, runtime, or documented-rule change.
**Code-review opt-in:** No — test-only.
**Branch:** dict-001-gate-dictionary-carried-dto — cut from the arc at Step 2; shared with DICT-002 (one seam, see Amendments)
**PR:** —

---

## Scope

Adds to the publish-trimmed harness one consumer-shaped case per shape this todo teaches the walk: a `[Factory]` entity carrying a `Dictionary<string, Dto>` property whose value type has an implicit parameterless constructor and whose only construction site is inside the entity's async `[Remote]` operation body — the zTreatment `BodyAssessmentInfo.Locations` shape — and a DTO reachable only as a public field of a walked type. Each case's DTO is otherwise unconstructed in client-reachable code. The harness deserializes each from a JSON literal, and the CI gate names each new type as a positive control so a case that stops generating cannot pass by absence. Both cases are run against the current generator first and their red results recorded in `reviews/001-evidence/` before any fix exists; a case that cannot be observed red does not count. Dictionary keys get no harness case, because System.Text.Json cannot deserialize a DTO-typed key without a converter; DICT-002 pins them with a generator test. It does not change the generator, the runtime, or the registry.

---

## Intent

- Prove both gaps exist in a publish-trimmed client, measured, before any fix, so DICT-002 is verified by cases that demonstrably fail without it.
- Close the hole the v1.7.0 gate left: it measured `[Remote]` body absence, but no case depended on a leaked body, so removing the leak broke a consumer and nothing here noticed.
- Leave each shape a permanent CI regression signal once DICT-002 turns it green.

---

## Framework & Architectural Alignment

- The harness contract in `RemoteFactory.TrimmingTests/README.md`: checks return `bool`, append to `failedChecks`, and the process exits non-zero on any failure.
- The smoke-test pattern of `EntityPropertyDtoSmokeTest`: deserialize from JSON literals through `INeatooJsonSerializer`, never construct the DTO in client-reachable code.
- The gate's positive-control convention in `verify-trimmed.sh`: a registrar holder name proves the registrar exists in the artifact.
- Red-first precedent: TRIM-007's keyboard negative control and EXRM-003's falsification run.

---

## Constraints & Invariants

- Every existing harness check and gate assertion stays green and unedited; the gate gains one additive positive-control line.
- The new DTOs are never constructed in client-reachable code; the dictionary value's only construction site is the async `[Remote]` body.
- No generator, runtime, or registry change.
- No new server-only port: the async body takes no server-only service, so no absence marker or DI-guard change is needed.
- No reflection in harness code.

---

## Steps

1. Add a `[Factory]` carrier entity to the harness with a dictionary-valued property whose value DTO is constructed only inside the entity's async `[Remote]` fetch body, plus a property holding a plain DTO that carries a second DTO in a public field.
2. Add a smoke test with one check per shape — deserialize the dictionary, deserialize the field host — each from a JSON literal, reporting the exception type and message on failure.
3. Wire both checks into the harness entry point as separately named failures, so one red cannot hide the other.
4. Add the carrier's registrar holder to the gate's positive controls.
5. Run the untrimmed harness and confirm both checks pass, which proves the JSON and the checks are right.
6. Publish trimmed, run the gate script and the harness, and confirm exactly the two new checks fail with a no-constructor error while every existing check passes.
7. Record the untrimmed and trimmed outputs and exit codes in `reviews/001-evidence/`.

---

## Acceptance

- [ ] The dictionary check passes on the untrimmed harness and fails on the publish-trimmed harness with a no-constructor error against the pre-fix generator. `[explicit-skip: one-off keyboard verification recorded in reviews/001-evidence, per TRIM-007 precedent]` · Must
- [ ] The public-field check passes on the untrimmed harness and fails on the publish-trimmed harness with a no-constructor error against the pre-fix generator. `[explicit-skip: one-off keyboard verification recorded in reviews/001-evidence, per TRIM-007 precedent]` · Should
- [ ] In that trimmed run every pre-existing harness check and every gate assertion stays green, and the carrier's registrar holder is present as a positive control. `[trimmed-harness]` · Must
- [ ] Both solutions build, and the unit and integration suites are green on net9.0 and net10.0. `[explicit-skip: build/test gates]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-05 against arc commit `5457120`.

- **Harness entry** — `Program.cs` is top-level statements; each smoke test returns `bool` and a failure appends a named entry to `failedChecks`; the process returns 1 if any entry exists. Existing smoke tests are invoked one per `if` near the end.
- **Smoke pattern** — `EntityPropertyDtoSmokeTest` builds its own `ServiceCollection`, calls `AddNeatooRemoteFactory(NeatooFactory.Remote, assembly)`, resolves `INeatooJsonSerializer`, and deserializes from string literals.
- **No incidental rooting from the call site** — `INeatooJsonSerializer.Deserialize<T>(string)` and `Deserialize(string, Type)` carry no `[DynamicallyAccessedMembers]` and no `new()` constraint (`NeatooJsonSerializer.cs:21-22`), so a generic call site does not root `T`'s constructor.
- **Serializer options** — `IncludeFields = true` (`NeatooJsonSerializer.cs:77`); `TypeInfoResolver` is `NeatooJsonTypeInfoResolver`, which supplies `CreateObject` from `DtoConstructorRegistry` when a type is registered and otherwise leaves the default reflection path, which finds nothing once the constructor is trimmed.
- **The two gaps, by read** — `DtoTypeWalker.UnwrapType` turns `Dictionary<K,V>` into `KeyValuePair<K,V>`, which `IsDtoStructureCandidate` rejects as `System.*`; `WalkProperties` visits `IPropertySymbol` only, so public fields are never walked.
- **Async body removal** — the class-factory async leg is measured absent by the existing gate (`ClassAsyncBody_MARKER`, TRIM-009), so a construction site inside an async `[Remote]` body does not survive on the trimmed client.
- **Gate positive controls** — a `for control in` list near the top of `verify-trimmed.sh`, including `NeatooClassFactoryRegistrar_TrimTestEntity`.
- **Project** — `RemoteFactory.TrimmingTests.csproj` is single-TFM net9.0, `PublishTrimmed`, `TrimMode=full`, `TreatWarningsAsErrors=false`; it inherits `AnalysisMode=all`. It is in `src/Neatoo.RemoteFactory.sln` but is an exe, so `dotnet test` does not run it. Local publish per its README is `-c Release -r win-x64 --self-contained true`; CI uses `linux-x64`.

---

## Punchlist

- (none)

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method / evidence | Tier confirmed |
|---|---|---|---|---|
| Dictionary check green untrimmed, red trimmed, pre-fix | Must | `[explicit-skip: keyboard verification]` | `DictionaryAndFieldDtoSmokeTest.RunDictionaryValue` at `effedce`; `reviews/001-evidence/untrimmed-run.txt` PASSED, `trimmed-run.txt` FAILED with `NotSupportedException` for `TrimDictValue` at `$.l-f-1` | ✓ |
| Public-field check green untrimmed, red trimmed, pre-fix | Should | `[explicit-skip: keyboard verification]` | `DictionaryAndFieldDtoSmokeTest.RunPublicField` at `effedce`; same files, `TrimFieldCarried` at `$.Carried` | ✓ |
| Every pre-existing harness check and gate assertion green; holder control present | Must | `[trimmed-harness]` | `reviews/001-evidence/trimmed-gate.txt` exit 0 with `NeatooClassFactoryRegistrar_TrimDictCarrier` ok; `trimmed-run.txt` fails exactly the two new checks | ✓ |
| Both solutions build; unit and integration green on net9.0 and net10.0 | Must | `[explicit-skip: build/test gates]` | `reviews/001-002-build.log`, `001-002-design-build.log`, `001-002-test.log`, `001-002-design-test.log` — one gate run for the shared branch, after DICT-002's change | ✓ |

---

## Gate Record

(Filled at Step 5.)

---

## Plan Amendments

### 2026-09-11 — Widened to every added shape

- **Section affected:** Scope, Serves, title
- **Original said:** One dictionary-valued case plus a signature dictionary.
- **What changed:** One case per shape the walk gains — dictionary value, nested wrapper, public field, tuple element — each red-first.
- **Why:** The todo was widened to the serializer's whole shape set.
- **Discovery Log:** 2026-09-11 / DICT

### 2026-10-05 — Narrowed to dictionaries and public fields

- **Section affected:** Scope, Serves
- **Original said:** One case each for dictionary value, nested wrapper, public field, and tuple element, plus a signature dictionary.
- **What changed:** Two cases: the consumer-shaped dictionary value and a public field. Tuples are out of scope; nested wrappers and the other two callers are pinned by DICT-002's generator tests.
- **Why:** User narrowed the todo 2026-10-05.
- **Discovery Log:** 2026-10-05 / DICT

### 2026-10-05 — Shares a branch with DICT-002; positive control names the holder

- **Section affected:** Branch, Steps
- **Original said:** Own branch; the gate names each new type as a positive control.
- **What changed:** One branch for 001 and 002, with the red evidence captured at the commit before the fix. The positive control is the carrier's registrar holder, not the DTO names.
- **Why:** Shipping red cases alone would make this plan's own gate veto it. DTO type names survive as property metadata whatever happens, so naming them could never go red.
- **Discovery Log:** 2026-10-05 / DICT-001

---

## Notes

- Why the gate cases come before the fix: the v1.7.0 gate measured `[Remote]` body absence but had no consumer-shaped case that depended on a leaked body, so removing the leak surfaced this only in a consumer. This plan's red observations are the missing evidence, and DICT-002 turns them green.
- The dictionary case checks two things at once. It is red today because the walk misses the value type, and it would be falsely green if a `[Remote]` body leak ever came back and re-rooted the constructor.
- The public-field case sits on a plain DTO rather than on the entity, because a `[Factory]` entity serializes through generated ordinal code that may not carry fields, while a plain DTO goes through the reflection serializer with `IncludeFields` on.
