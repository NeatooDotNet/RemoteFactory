# TRIM-008 — Code Review (per-plan, opt-in)

**Gate:** Step 5, opted in (`Code-review opt-in: Yes`). **Passes:** three (2026-08-13). Findings-only; no grade.
**Evidence set:** [`008-evidence/`](./008-evidence/) — manifest in [`008-test-review.md`](./008-test-review.md).

Every checkable finding was independently re-derived at the keyboard before being accepted. **All of them held.**

---

## Standing conclusions (unchanged across all three passes)

**The generator change is correct and minimal.** `git diff --name-only 25ac975..HEAD -- src/Generator/` returns exactly two files: `StaticFactoryRenderer.cs` and `RelayHandlerRenderer.cs`. No model, builder, transform, or dispatch change. That is a **structural** argument for the byte-identity constraint — stronger than the 732/40/692 measurement, because `Generated/` is gitignored and the measurement is not re-derivable by a later reader while this one-command check is. It also makes the incremental-cache constraint literally true: `RelayHandlerModel`/`TypeInfo` gain nothing and `IncrementalCacheTests` is untouched.

**The holder shape is right.** Top-level `internal static`, one forwarding method, distinct prefix per leg — reusing the proven in-tree `EventPreservationRenderer` shape rather than the nested-type variant that rested on unverified ILLink behaviour. Forwarding rather than hosting is load-bearing: `[Execute]` methods are `private static` and the registrar body calls them, so a hosting sibling would be CS0122. `AddRemoteFactoryServices` reaches the `internal static` holder method via `BindingFlags.Static | NonPublic | Public`.

**`FactoryAttributes.cs` is XML-doc-only**, verified member by member: no new members, no signature, accessibility, or attribute-argument change.

**All five plan-review vetoes were genuinely addressed**, not merely claimed — including the B1 step reordering, visible in commit order.

**No new trim-analysis surface:** 25 `IL2xxx` warnings in the publish log, none naming either holder; all pre-existing library code.

---

## Pass 1 — 3 veto, 8 callout

| # | Finding | Disposition |
|---|---|---|
| V1 | **The rewritten gate silently dropped a marker the old gate carried.** `grep -F "IServerOnlyRepository"` cannot match the bare implementation name that `(?<!I)ServerOnlyRepository` existed to catch — a coverage regression inside this plan's own Step-8 deliverable, narrated as strictly strengthening | **Fixed.** `ServerOnlyRepository_MARKER` restored, measured untrimmed-PRESENT first |
| V2 | **`RelayLegBackend` reported as a present→absent pair**; the probe recorded it absent both times. The blanket header was wrong for 8 of 16 markers | **Fixed.** `[D]`/`[R]`/`[N]` taxonomy |
| V3 | Two forward-looking doc claims added by this plan, unregistered — both implying that naming a *generated* type is what makes a leg safe | **Fixed.** `{X}Factory` is generated and hosts every `Local*`; corrected to "necessary, not sufficient" in both the design doc and the published page. Acceptance bullet scoped to the two shapes this plan delivers |
| C1 | **TRIM-009's stub reached the right conclusion through a wrong premise and a non-sequitur** — DAM *is* live on that leg, and the static-leg measurement could not confirm anything about it | **Fixed.** Both errors recorded rather than silently rewritten; conclusion rebuilt on the verified unguarded-closure root chain, with the holder indirection noted as plausibly *part* of the eventual fix |
| C2–C8 | Holder method name unpinned; win-x64-only exercise; `BuildReferences` widening; nested/global/generic shapes; stale counts; uncaptured red runs; review running ahead of the test gate | **Fixed or accepted with reason** |

## Pass 2 — 4 veto, 7 callout

| # | Finding | Disposition |
|---|---|---|
| V4 | **The `[D]`/`[R]` labels introduced to fix V2 were themselves wrong.** `IServerOnlyRepository` and `DoServerWork` were present pre-fix, so they are discriminators — and the gate under-claimed the very marker whose flip justifies deleting the exemption. The async/class blocks were labelled with a category that did not apply | **Fixed.** Reclassified; `[N]` added for markers with no pre-fix measurement |
| V5 | **The three-condition narrowing was unsupported.** Two conjuncts rested on rows that cannot discriminate (static/relay over-determined; interface non-discriminating by the stub's own admission), and the "single-variable" class-factory pair actually differed in four further ways — auth block, target acquisition, **one-hop vs two-hop rooting**, and an extra catch arm plus lifecycle probes | **Fixed by experiment.** V5c run: `TrimTestEntity` given an async `[Remote][Fetch]` beside its sync `[Remote][Create]`. Sync marker absent, async marker present, both present untrimmed. `async`-shaped emission confirmed as the operative variable |
| V6 | **The gate's success message claimed async class-factory coverage that did not exist** — the most costly place to overstate, since it claims coverage of exactly the experiment that decides TRIM-009's scope | **Fixed**, and made true by V5c |
| V7 | V3's fix landed on the internal design doc but not the published page | **Fixed** |
| C9–C15 | Container never received the async result; no durable controls for the new async targets; the untrimmed-presence non-vacuity claim was unsound; stale numbers; probe header; item-15 exit instruction; archive-age caveat | **All fixed or recorded** |

## Pass 3 — 3 veto, 5 callout

| # | Finding | Disposition |
|---|---|---|
| N1 | **The Test Evidence row contradicted the script it describes**, in two directions, both introduced by the previous round's own fix | **Fixed.** Row reclassified to match the script and this plan's Current State; the unavailable `crux-measurement.txt` citation removed |
| N2 | **AC6 and the release-blocking inventory both understated the defect.** `FetchAsync` is a **read** operation and it leaks, but both said "async **write** operations" | **Fixed.** Widened to any async operation, read or write, both measured |
| N3 | **Two review gates asserted with no records** — every other gated plan in this arc has both files | **Fixed** — this file and [`008-test-review.md`](./008-test-review.md) |
| N4–N8 | Second inseparable co-variate (the four lifecycle type-tests); stale citations for a third round; the class-factory gate block no longer contains a class-factory signal; stale control-count comment; C3's linux risk grew | **All fixed**, and the gate block renamed to "port implementation (behind an interface hop; not a leg signal)" |

---

## Accepted with reason

- **C3 / T3 — the gate has only ever been exercised on `win-x64`; CI publishes `linux-x64`.** The CI-only surface has grown to 8 assert-PRESENT markers and 8 positive controls. Accepted: the merge run is its first linux exercise and a failure there is informative rather than silent. Recorded so a red merge run is not misread as falsifying TRIM-009 — and the pass-3 early-exit fix means a suspect artifact now stops at the controls instead of emitting remediation advice.
- **C6 — `DiagnosticTestHelper.BuildReferences()` widening** changes the compilation every generator test runs under: fixtures whose `Task<T>`/`IServiceCollection`/`ILogger<T>` were previously *error types* now bind. Strengthening, not weakening — but every pre-TRIM-008 green was obtained under a narrower compilation.
- **C7 — nested types, global namespace, generic containing types** were *traced* in the generator rather than assumed. Name derivation is unchanged in kind, the holder binds where the old attribute target did, and the already-broken shapes (deferred items 14/15) gain no new error.
- **N4 (relay registration) — the holder *type* is covered by a full-name control, but DAM roots that type regardless.** What remains uncovered is whether the handler *registration* fires, which a client-side trimmed harness structurally cannot signal. Covered untrimmed by the integration suite.

## Verdict

**The deliverable is done.** The generator change has been stable and correct across three passes; the harness, gate, and tests are now the strongest artifacts in this arc. What blocked Done at pass 3 was bookkeeping — a plan contradicting its own artifact, an understated acceptance criterion, and two missing review records — all closed here.

**TRIM-008 delivers two of AC6's three shapes and must not be closed out as delivering AC6.** The third is TRIM-009, on which the v1.7.0 release remains blocked.
