# PHASE-002 — Code Review (per-plan, findings-only)

**Date:** 2026-08-15
**Reviewer:** `code-reviewer`
**Result:** 1 veto-tier, 8 callout-tier
**Disposition:** veto closed; 5 callouts fixed; 3 recorded or routed.

---

## Veto-tier

### V1 — The NF0504 message test cannot go red for the claim its bullet makes

The severity test stacks the **unphased** attribute first, so the surviving phase is
`Immediate` — which is simultaneously the hardcoded default constant, the value the
malformed-argument fallback returns, and what a `messageFormat` with the `{2}` placeholder
deleted would print. The assertion stays green against all three wrong implementations, and
the `phaseName.Length > 0 ? phaseName : phaseValue.ToString(…)` expression that computes the
argument had both branches uncovered in the non-default direction.

Veto-tier by this arc's own standard rather than a generic one: plan review B-V2 vetoed a
planned `Contains("DispatchPhase.AfterCommit")` for exactly this reason, B-V1 vetoed the cache
bullet for it, and it is the one claim in the plan that was neither pinned nor declared
unpinnable in the red-proof log.

**Closed.** `RelayHandler_DuplicateEventType_PhasedFirst_KeepsThatPhaseAndNamesItInTheMessage`
asserts the message names `AfterCommit`, that it does *not* name `Immediate`, and that the
emitted registration matches — pinning source-order-wins at the same time. Red-proofed by
hardcoding `Immediate` into the `messageFormat` (RP-5): the new test goes red, the original
stays green, which is the diagnosis confirmed. The test-review gate reached the same finding
independently.

---

## Callout-tier

| # | Finding | Disposition |
|---|---|---|
| C1 | **Culture-sensitive integer rendering into generated C#.** Interpolation formats with `CurrentCulture`; `(DispatchPhase)(-1)` compiles, and on a culture whose negative sign is not ASCII `-` (sv-SE resolves to U+2212 under ICU) the emitted argument is a CS1056 in the consumer's build. The file already passed `InvariantCulture` to both `Convert.ToInt32` calls, where culture cannot matter, and omitted it from the two `ToString()` paths, where it can. | **Fixed** — `InvariantCulture` on both; `RelayHandler_UndefinedPhaseValue_RendersAsACast` is now a `[Theory]` covering a negative value. |
| C2 | **Misplaced XML doc.** `ReadDispatchPhase` was inserted between `TransformRelayHandler`'s doc comment and its method, leaving `ReadDispatchPhase` with two `<summary>` blocks and the file's central transform undocumented. No compiler warning fires for a duplicate `<summary>`. | **Fixed** — doc restored to `TransformRelayHandler`. |
| C3 | **`Convert.ToInt32` can throw inside a transform**, which surfaces as CS8785 and takes down every generated file in the compilation, not just this one. No compiling shape is known to reach it, but the repo already has a non-throwing idiom for reading attribute constructor arguments. | **Fixed** — `is int` pattern matches in both places, matching `FactoryGenerator.cs:226`. Zero behavior change for compiling source; the crash class is gone. |
| C4 | **NF0503's emission count changes in one shape** — a class with a duplicate attribute *and* a matching instance method now emits NF0503 once instead of twice, because the duplicate short-circuits before the instance-method scan. An improvement, but the plan's reasoning named only NF0501/NF0502 as held steady. | **Recorded** in the transform's comment. One warning per class beats one per redundant attribute. |
| C5 | **NF0504 repeats verbatim for a triple declaration**, all at the class location. `ApplicationSyntaxReference` would let it point at the redundant attribute itself. | **Accepted with the reason recorded** — NF0501/NF0502 both use the class location, so this is convention-consistent. The location clause is now pinned (test-review #3). |
| C6 | **`CLAUDE-DESIGN.md` is going stale and PHASE-005's handoff did not name it.** The arc's practice is to maintain that file in-plan — PHASE-003 updated its log-id rows inside its own plan. | **Fixed here** — NF0504 added to the diagnostics table and the prose line, plus a line stating the attribute's phase now reaches registration. |
| C7 | **Attributes split across partial declarations** — `ForAttributeWithMetadataName` fires per syntax node while the transform reads `symbol.GetAttributes()`, so two attributed partials should collide on hint name. Pre-existing; flagged because Step 4 settled the attribute-stacking contract and this is the stacking shape that reasoning does not reach. Inferred, not measured. | **Routed** to new tech-debt plan 008 (both reviewers advised against widening PHASE-007). |
| C8 | Container state — plan `Status: Draft`, Index row `Draft`, checkboxes unchecked. | **Closed at Step 6.** |

## Verified clean (the load-bearing claims)

- **Constraint 1, primitive representation** — the constraint the plan said review must enforce
  because no test can. `PhaseName`/`PhaseValue` are `string`/`int`; the `TypedConstant` and
  `IFieldSymbol` stay local to `ReadDispatchPhase`; `registeredPhaseByEventType` is a local
  dictionary discarded at transform end; `DiagnosticInfo.MessageArgs` is `EquatableArray<string>`,
  so the new message args do not leak either. No `ISymbol`, `TypedConstant`, or `Compilation`
  reaches the transform output, directly or transitively.
- **Trimming shapes** — registration still inside `if (NeatooRuntime.IsServerRuntime)`; assembly
  attribute still names the generated holder; an enum constant lowers to `ldc.i4.<n>` so no new
  type reference reaches the client; DAM annotation present on both overloads.
- **NF0504 × NF0501/NF0502 orderings** — walked exhaustively; the counts hold for every ordering.
  The one exception is C4.
- **Test Evidence honesty** — every cited test exists at the declared tier and location; the only
  edits to pre-existing test files are two comment blocks and one fixture attribute.
