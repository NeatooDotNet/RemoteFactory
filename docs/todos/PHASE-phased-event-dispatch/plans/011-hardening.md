# Hardening: Removing a Hazard Rather Than Documenting It, and Cloning the Shadowing Guard

**Plan #:** 011
**Date:** 2026-08-31
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-31
**Plan-review opt-in:** No (both items are guards over behavior PHASE-008 already measured; no new contract, and the blast radius is bounded by the sacred-tests rule)
**Code-review opt-in:** No (one deletion of an uncalled internal method, and generator tests that add no production code — the gate is the right and sufficient eye)

---

## Scope

Close the two hardening rows PHASE-008's gate queued, folding row 012 into this plan.
Replace the `FactoryEventHandlerRegistry.Clear()` documentation-only mitigation with a
mechanical one, and clone the namespace-shadowing compile guard the gate called this arc's
best artifact across the four renderers that never had it. This plan adds **no production
behavior**: it removes an uncalled member and adds generator tests. It does **not** touch
the phase-dispatch runtime, does **not** revisit the `Internal`-usage analyzer PHASE-004
queued as its own candidate, and does **not** widen the emission-qualification work beyond
what a shadowing fixture actually reddens.

---

## Inherited (routed here by the PHASE-008 gate)

- **Row 011 (T1):** nothing mechanically prevents the next author from calling
  `FactoryEventHandlerRegistry.Clear()` and breaking the suite. PHASE-008 measured that a
  single test calling it turns
  `FactoryEntryCallTests.DrainedHandlerInvokingAFactory_NestsWithoutDrainingOrClearingTheDrainInProgress`
  red — that test passes alone. The XML-doc correction was the right disposition for 008
  (pinning it would have meant weakening a sacred test), but documentation is the
  accepted-risk position.
- **Row 012 (T2):** `ClassFactoryRenderer:58`, `InterfaceFactoryRenderer:53`,
  `StaticFactoryRenderer:45`, and `EventPreservationRenderer:71` carry the same bare
  assembly-attribute token the relay leg did, and none has a shadowing compile test. The
  gate called `RelayHandler_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles`
  "the single best artifact this plan produced" and worth cloning per renderer. The row also
  carries an absurd-tier note: `sp.GetRequiredService<T>()` is emitted unqualified and relies
  on an injected `using`, so a consumer's own extension method in their namespace would win
  on lookup.

---

## Intent

- The registry hazard stops depending on someone reading a doc comment. A member that
  cannot be called safely from the only assemblies able to call it should not be callable.
- Every renderer that emits into a consumer's namespace has an executable guard proving a
  hostile namespace layout still compiles — so the qualification work PHASE-008 did on one
  leg cannot silently regress on the other four, and any leg that is *already* correct has
  that recorded as a measurement rather than an assumption.
- Where a leg turns out to need no fix, the test still ships: its value is as a regression
  guard, and saying so is more honest than implying it caught something.

---

## Framework & Architectural Alignment

- **Existing tests are sacred.** No assertion is weakened; this plan only adds tests and
  removes an uncalled production member.
- **Emission qualification** follows the rule PHASE-002 established and PHASE-008 extended:
  type-bearing tokens emitted *inside a consumer's namespace* are `global::`-qualified;
  tokens that are structurally immune are recorded as such at the emission site rather than
  qualified reflexively.
- **Compile-checked over string-checked.** The guard is `outputCompilation.GetDiagnostics()`
  with decoys of the wrong shape, not `Contains` on a qualified string — the false green
  PHASE-002 documented.
- **`Internal` namespace policy:** removing an `internal` member is within the
  may-change-in-any-release contract that namespace carries.
- **Red-proof discipline:** any claim that a new guard discriminates is measured, or labeled
  derived. A guard that passes on first run against unmodified code is a *regression* guard
  and must say so.

---

## Constraints & Invariants

- `FactoryEventHandlerRegistry`'s public surface is unchanged — only the `internal` `Clear()`
  goes. No consumer can observe this: `internal` limits callers to the six
  `InternalsVisibleTo` assemblies, all of which are this repo's own test projects plus
  `Neatoo.RemoteFactory.AspNetCore`.
- Suites stay green with existing assertions intact; totals only grow. Baseline at plan
  start: unit **758×2 TFMs**, integration **595×2 (+5 standing skips)**, Design **98×2**.
- A shadowing fixture that reddens a renderer is a *finding*, not a licence to widen scope
  past qualifying the tokens it names.
- No new public API, no new log event, no new diagnostic.

---

## Steps

1. Fold row 012 into this plan as a tombstone, and record the merge in the Index and
   Discovery Log so the two gate findings stay traceable to where the work happened.
2. Remove `FactoryEventHandlerRegistry.Clear()`, replacing the doc-comment mitigation with
   the absence of the hazard — after confirming nothing in the solution calls it and that
   its stated rationale (a single-threaded host) is unreachable through an `internal` member.
3. Leave the isolation *discipline* documented where it belongs — on the registry type
   itself, describing the keying that makes per-test event types sufficient — so removing
   the escape hatch does not also remove the explanation of why none is needed.
4. Clone the namespace-shadowing compile guard for the class, interface, static, and
   event-preservation legs, each with decoys of the wrong shape so a mis-binding is a
   compile error rather than a silently wrong emission.
5. Fix whatever those guards redden, and record which legs were already correct — including
   *why*, since the answer (the `global::` strip is local to the relay transform) is the
   fact that makes the other legs safe and is worth stating once.
6. Settle the `GetRequiredService<T>` extension-method note: qualify it, or record it as
   accepted with the reason at the emission site.

---

## Acceptance

- [ ] `FactoryEventHandlerRegistry.Clear()` no longer exists, and the suite is green without
      it — the hazard is removed rather than documented.
      `[explicit-skip: a deletion; its absence is checked by compilation, and the behavior it guarded is pinned by the row below]`
- [ ] The registry's keying contract — entries keyed by `(event type, handler class)`, which
      is what makes a per-test event type sufficient isolation — remains pinned and
      documented on the type. `[unit]`
- [ ] A consumer namespace that shadows every unqualified route still produces class-factory
      output that compiles. `[unit]`
- [ ] The same, for the interface-factory leg. `[unit]`
- [ ] The same, for the static-factory leg. `[unit]`
- [ ] The same, for the event-preservation leg. `[unit]`
- [ ] Each new guard is labeled by what it is: a leg that needed a fix says so, and a leg
      that passed unmodified is recorded as a **regression guard** rather than implying it
      caught a defect. `[explicit-skip: honesty of the record, verified at the gate]`
- [ ] The `GetRequiredService<T>` extension-method route is either qualified or accepted with
      its reason stated at the emission site.
      `[explicit-skip: a one-line emission or comment change; covered by the shadowing guards if qualified]`
- [ ] Both solutions build; all three suites green with existing assertions intact.
      `[explicit-skip: meta-bullet, satisfied by the Step 5 gate pre-flight]`

---

## Current State (Pre-Flight)

Walked 2026-08-31 before the first edit.

**`Clear()` is uncalled, and its stated rationale is unreachable.** A solution-wide grep for
`FactoryEventHandlerRegistry.Clear` finds exactly two hits: the method's own body
(`FactoryEventHandlerRegistry.cs:130`) and a doc-comment reference in
`FactoryEventPhaseSchedulerConcurrencyTests.cs:546` describing the test PHASE-008 removed.
Nothing calls it. Its XML says it "stays for a single-threaded host that genuinely needs to
reset the process" — but the member is `internal`, and `RemoteFactory.csproj:3-8` limits
`InternalsVisibleTo` to six assemblies: `FactoryGeneratorTests`,
`Neatoo.RemoteFactory.AspNetCore`, `RemoteFactory.UnitTests`,
`RemoteFactory.IntegrationTests`, `Design.Tests`, `RemoteFactory.TrimmingTests`. Five are
this repo's test projects and the sixth does not call it. **No external host can reach an
`internal` member**, so the rationale describes a caller that cannot exist. Deletion is
therefore not a trade-off against a real use case; it removes a member whose only reachable
callers are the ones documented as forbidden.

**The `global::` strip is local to the relay transform.** Grepping the whole generator for
`StartsWith("global::")` / `Substring("global::".Length)` returns four hits and all four are
in `FactoryGenerator.RelayHandler.cs` (`:282-283`, `:337-338`, `:426-427`) plus the
re-qualifying helper PHASE-008 added in `RelayHandlerRenderer.cs:178`. The other legs take
their type names from `FactoryGenerator.Types.cs`'s `FullyQualifiedFormatWithNullable`
(`:25-27`, applied at `:447` and `:464`), which does **not** strip — so their consumer type
tokens keep `global::`. This is the fact behind the plan's Notes prediction that several of
the four new guards will pass unmodified.

**All four target renderers already qualify the assembly attribute's *argument*.**
`ClassFactoryRenderer:58`, `InterfaceFactoryRenderer:53`, `StaticFactoryRenderer:45`, and
`EventPreservationRenderer:71` all emit `typeof(global::{ns}.{...})`. What is bare in each is
the attribute's own *name* (`Neatoo.RemoteFactory.NeatooFactoryRegistrar`) — the token
PHASE-008 recorded as structurally immune, because assembly attributes bind at
compilation-unit scope, outside any namespace declaration, where nothing consumer-declared is
in scope. So row 012's headline item is immune on all four legs for the same reason it was on
the relay leg, and the guards' real subject is what each renderer emits *inside*
`namespace {ns}`.

**Test-fixture inventory.** `AssemblyAttributeEmissionTests.cs` holds a static-factory fixture
(`StaticFactorySource`, `:315`) and four relay fixtures; there is **no** class- or
interface-factory fixture in this file, so both must be written. Shapes taken from the
sources of truth: class factories are `[Factory] public class X` with `[Create]`/`[Update]`
members (`Save/SaveCodePathTests.cs:24-39`); interface factories are `[Factory]` on the
interface with **no** operation attributes on its methods, plus a separate implementation
(`Design.Domain/FactoryPatterns/AllPatterns.cs:168-215` — adding `[Fetch]` etc. is NF0106).
`GeneratedSourceFor` (`:794`) is the existing per-hint extraction helper.

---

## Test Evidence

All cited tests are in `RemoteFactory.UnitTests.FactoryGenerator.AssemblyAttributeEmissionTests`
unless noted. Suites at close: **unit 762×2 TFMs** (758 → 762), **integration 595×2 (+5
standing skips)**, **Design 98×2** — both solutions built explicitly. Logs:
`reviews/011-build.log` (0 errors), `reviews/011-test.log`, `reviews/011-redproof.log`.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| `Clear()` no longer exists; suite green without it | `[explicit-skip]` | A deletion — enforced by compilation. Nothing referenced it (verified solution-wide before removal) | n/a |
| Registry keying contract remains pinned and documented | `[unit]` | `Internal.FactoryEventPhaseSchedulerConcurrencyTests.RegistryEntriesAreKeyedByEventType_SoPerTestEventTypesAreSufficientIsolation` (PHASE-008; unchanged). Documentation moved onto the type where `Clear()` used to be. **Unmeasured**, as it was in 008 | ✓ |
| Class-factory output compiles under shadowing | `[unit]` | `ClassFactory_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles` — **regression guard**, no sabotage in this plan reddens it | ✓ |
| Interface-factory output compiles under shadowing | `[unit]` | `InterfaceFactory_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles` — **caught a live defect** (CS0738); RP-1 confirms sole coverage | ✓ |
| Static-factory output compiles under shadowing | `[unit]` | `StaticFactory_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles` — **caught a live defect** (CS0029); RP-1 confirms sole coverage | ✓ |
| Event-preservation output compiles under shadowing | `[unit]` | `EventPreservation_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles` — **regression guard**, as above | ✓ |
| Each guard labeled by what it is | `[explicit-skip]` | Two labeled *regression guard* and two labeled as having caught a defect, in their XML and in the red-proof log's UNMEASURED section. RP-1 is the evidence for the split | n/a |
| `GetRequiredService<T>` route settled | `[explicit-skip]` | **Not settled — superseded.** The fixture found the surrounding family (128 bare BCL tokens, plus the missing `using System;` injection) to be far wider than the single route the row named. Queued as row **013** with the measurement. See Amendments A2 and A3 | n/a |
| Both solutions build; three suites green, totals only grow | `[explicit-skip]` | `reviews/011-build.log`, `reviews/011-test.log` — six green summaries | n/a |

**No `MISSING` rows.** One bullet is explicitly superseded rather than met, with the reason
and its replacement row recorded. **Three tests ship unmeasured** and are declared in the
red-proof log: the two regression guards and the inherited registry-keying pin.

---

## Plan Amendments

### 2026-08-31 — A1: row 012 contained a live defect, not a regression-guard exercise

- **Section affected:** Steps 4–5, the four shadowing Acceptance bullets, and the Notes
  prediction.
- **Original said:** clone the shadowing guard across four legs and "expect several to pass
  on first run," because the `global::` strip that caused the relay bug is relay-only.
- **What changed:** all four reddened. The premise was right and the conclusion wrong — the
  other legs never *asked* for qualification: `FactoryGenerator.Types.cs:696` and `:706` took
  the delegate/method return type via `ITypeSymbol.ToString()`, which renders a **minimally
  qualified** name, while `:736` three lines below already used `FullyQualifiedFormat`. Fixed
  by qualifying both, which closes the static leg's CS0029 and the interface leg's CS0738.
- **Why:** this is the same defect class PHASE-008 fixed on the relay leg, reached by a
  different route — there a strip removed the qualification, here it was never requested. A
  consumer type bound to a shadowing decoy is a *wrong-type* binding, not a missing-type
  error, so it is the severe half of what the fixture found.
- **Discovery Log link:** 2026-08-31 — PHASE-011.

### 2026-08-31 — A2: the BCL-token half of the fixture was removed, not fixed

- **Section affected:** Step 6 and the `GetRequiredService<T>` Acceptance bullet.
- **Original said:** settle the extension-method note by qualifying it or accepting it with
  a reason at the emission site.
- **What changed:** neither. The fixture's third decoy (`namespace TestNamespace.System`)
  reddened all four legs on CS0246/CS0234, revealing a **128-occurrence** surface of bare
  `Task` / `CancellationToken` / `Type` / `IServiceCollection` / `IServiceProvider` /
  `Exception` / `System.Diagnostics` across the four renderers — far wider than the single
  `GetRequiredService<T>` route the row named. The decoy was removed and the whole family
  queued as Index row **013**.
- **Why:** severity differs in kind. A shadowed *consumer* type binds to the wrong type and
  may compile; a shadowed *BCL* token fails loudly in the consumer's own build. And a
  128-token sweep across four renderers, with an assertion ratchet far larger than
  PHASE-008's nine sites, is a plan rather than a step — particularly as the last change
  before a close-out audit. This is the Constraints rule applied as written: a fixture that
  reddens a renderer is a finding, not a licence to widen scope.
- **Discovery Log link:** 2026-08-31 — PHASE-011.

### 2026-08-31 — A3: a second finding, folded into the same queued row

- **Section affected:** none of the Steps; discovered while isolating A2.
- **What changed:** nothing in this plan's code. Recorded: the class, interface, and
  event-preservation legs emit `IServiceProvider`, `Exception`, and
  `InvalidOperationException` unqualified **and** no renderer injects `using System;`
  (verified — `RelayHandlerRenderer` injects three usings of its own, the others inject
  none). So those legs depend on the *consumer's* file carrying `using System;`; a consumer
  who omits it gets a factory that does not compile. The fixtures were given `using System;`
  to isolate the shadowing question from this one.
- **Why:** same family as A2 and the same remedy, so it joins row **013** rather than
  splitting a third way.
- **Discovery Log link:** 2026-08-31 — PHASE-011.

---

## Notes

- **Why one plan again.** Same reasoning the user applied at PHASE-008 and which held: the
  gate is per plan, so merging is the only move that cuts the ceremony. These two items are
  smaller than 008's and share a branch cleanly.
- **Expect several of the four shadowing guards to pass on first run.** Pre-flight already
  found that the `global::` strip lives only in `FactoryGenerator.RelayHandler.cs`, so the
  other legs' consumer type tokens keep their qualification. That is a prediction, and it is
  written here so the run either confirms or kills it — the arc has five wrong predictions on
  record and every one of them was informative.
- Both reviews are opted out. The Step 5 gate is mandatory and remains.
