# Generator: Phase Pass-Through from Attribute to Registration

**Plan #:** 002
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-15
**Plan-review opt-in:** Yes (adds a generator diagnostic — new compile outcomes for source that builds today; raised from "No" at drafting)
**Code-review opt-in:** Yes (generator emission change; incremental-cache equality contract at stake)

---

## Scope

Teach the generator to read the `DispatchPhase` argument from `[FactoryEventHandler<T>]`
and thread it through the relay-handler model into the generated
`FactoryEventHandlerRegistry.RegisterHandler` call, preserving the incremental-cache
equality contract on the handler model and the v1.7.0 forwarding-holder trimming pattern.
Covers generator tests (emitted-source assertions and incremental-cache tracking) for both
the defaulted and explicit-phase cases, plus one end-to-end test proving an
attribute-declared phased handler actually defers and drains. Also resolves the
duplicate-same-event-attribute question PHASE-001 deferred here. This plan does NOT add
runtime behavior (PHASE-001 owns the runtime, PHASE-003 the drain) and does NOT touch
factory-method emission (PHASE-003).

---

## Intent

- A consumer who writes `[FactoryEventHandler<T>(DispatchPhase.AfterCommit)]` gets a handler
  that actually runs at the AfterCommit drain point. Today the argument compiles and is
  silently ignored: every generated registration lands at `Immediate` no matter what was
  written. That is the worst available shape — the API reads as working.
- The attribute becomes the only way a consumer needs to express a phase. Until this plan,
  phased handlers exist only for code that calls the registry's phase-taking overload by
  hand, which is not a consumer-facing API.
- Declaring the same event type twice on one handler class stops being a silent
  first-registration-wins drop and becomes a compile-time signal.

---

## Framework & Architectural Alignment

- **v1.7.0 forwarding-holder trimming pattern** — registrations stay inside the
  `NeatooRuntime.IsServerRuntime` guard and the assembly registrar attribute keeps naming
  the generated holder, never the consumer's handler class.
- **Incremental-generator cache boundary** — `RelayHandlerModel` and `EventHandlerEntry` are
  transform outputs whose equality *is* the cache boundary (see their remarks); anything
  added to them must be value-equatable all the way down.
- **The generator matches runtime attributes by metadata name and never references the
  runtime types** — PHASE-001's shared-source discovery: linking `DispatchPhase` into the
  netstandard2.0 Generator project duplicates a public runtime type in every project that
  references both. The phase travels through the generator as data.
- **Relay-handler diagnostics live in the NF05xx range** and follow the severity convention
  already set by NF0501/NF0502 versus NF0503. The dividing line is what the generator emits,
  not how wrong the source is: NF0501/NF0502 both add no entry and the class emits no file at
  all, so the declaration is dead and Error is right; NF0503 fires where the class still works
  and only a declaration is inert, and Warning was chosen there deliberately to keep the build
  green (`docs/plans/completed/factory-events-relay-redesign.md:76`).

---

## Constraints & Invariants

- Handlers written with no phase argument keep registering at `Immediate`; the existing
  suite passes unmodified.
- No `DispatchPhase` type reference inside the Generator project — no new link, no new
  using, no name-round-trip through the runtime assembly.
- **The phase travels on the transform output as a primitive `string` or `int` — never a
  `TypedConstant`, never any `ISymbol`.** Those drag a `Compilation` into the generator's
  incremental cache, and the caching test cannot see it (see the Notes on what that test
  actually pins). This one is enforced by code review, not by a test.
- **The emitted phase is `global::`-qualified.** The unqualified form is the latent bug
  documented at `RelayHandlerRenderer.cs:38-40`, which shipped for four releases before v1.7.0
  fixed it on the registrar attribute; this plan emits a new type-bearing token into the same
  file, bound only by a hardcoded `using`.
- Registration stays inside the server-runtime guard; nothing new reaches a trimmed client;
  the registrar attribute keeps naming the generated holder. This leg has no positive control
  in the trimming harness by construction (`RelayHandlerLegTarget.cs:23-31` — the registration
  is server-guarded, so the harness can only assert absence), so trimming safety here is an
  argument, not a measurement.
- Generated source stays readable — a phase renders as its named enum member, not an
  opaque numeric cast, for every phase the runtime enum defines.
- No runtime behavior change in this plan: registry, dispatcher, and scheduler are untouched.

---

## Steps

1. Carry the attribute's phase through the handler transform as data (not as the runtime
   enum type), defaulting to the `Immediate` value when the consumer wrote no argument.
2. Keep the transform output value-equatable so the `RelayHandler` branch stays cached.
3. Emit the phase into the generated registration through the registry's phase-taking
   overload, rendering the value as its named member so generated code stays readable.
4. Resolve the deferred duplicate-same-event-attribute question: report a new relay-handler
   diagnostic at **Warning**, and skip the duplicate's entry so the emitted registration
   matches what the diagnostic says. The documented contract already scopes attribute
   stacking to *several event types* (`docs/attributes-reference.md:222`, skill
   `factory-events.md:133`) — the diagnostic enforces a rule the docs already state.
5. Pin emission for the defaulted case, the explicit-phase case, and one class declaring
   several event types at different phases.
6. Extend the incremental-cache fixture so the phase data is populated on a transform output
   — for the collection-shaped-field regression it can genuinely catch, not for the phase
   read, which it cannot (see Notes).
7. Prove the loop end-to-end: an attribute-declared `AfterCommit` handler — with no
   hand-written registry call anywhere in the test — defers at raise time and runs at the
   entry call's drain point.

---

## Acceptance

- [ ] A handler class declaring `[FactoryEventHandler<T>(DispatchPhase.AfterCommit)]` has its
      handler deferred at raise time and run when the entry factory call completes, with no
      hand-written `RegisterHandler` call in the test. `[integration]`
- [ ] A handler class declaring no phase argument still dispatches at raise time, inside the
      factory call. `[integration]`
- [ ] Generated registration for an explicitly phased handler passes the named phase through
      the phase-taking overload, `global::`-qualified — the bare `DispatchPhase.X` form is
      pinned absent, the way `RelayHandler_AssemblyAttribute_DoesNotNameConsumerType` pins the
      bare registrar argument. The defaulted handler registers at `Immediate`. `[unit]`
- [ ] One handler class declaring several event types at different phases registers each at
      its own phase. `[unit]`
- [ ] Declaring the same event type twice on one handler class reports a Warning located at
      the class naming the surviving phase, and emits one registration rather than two. `[unit]`
- [ ] The `RelayHandler` incremental branch stays cached across an unrelated edit, with the
      fixture populating phase data. Pins determinism and guards a future collection-shaped
      phase field; it does **not** pin the phase read — see Notes. `[unit]`
- [ ] Registration remains inside the server-runtime guard and the assembly attribute still
      names the generated holder. `[unit]`
- [ ] The existing suite passes unmodified — no test edited to accommodate the new emission.
      `[explicit-skip: regression meta-bullet, satisfied by the Step 5 full-suite run]`
- [ ] Build/test green on both target frameworks.
      `[explicit-skip: meta-bullet, satisfied by the Step 5 gate logs]`

---

## Current State (Pre-Flight)

Walked 2026-08-15, before any edit. No surprise large enough to reshape the plan.

- **The phase is dropped on the floor at exactly one line.** `FactoryGenerator.RelayHandler.cs:48-211`
  loops `symbol.GetAttributes()` and matches by metadata name (`originalDef.Name ==
  "FactoryEventHandlerAttribute"`, arity 1, namespace `Neatoo.RemoteFactory`) — exactly the
  string-matching the attribute's own XML doc promises, so no type reference to remove. It
  reads `attr.AttributeClass.TypeArguments[0]` for the event type at `:60` and never touches
  `attr.ConstructorArguments`. That is the whole of the gap.
- **`EventHandlerEntry`** (`Model/RelayHandlerModel.cs:58-99`) currently carries two strings,
  two bools, and three `EquatableArray<ParameterModel>`. A phase field has to be a string or
  an int — `DispatchPhase` is unavailable in the netstandard2.0 generator by design — and
  either is value-equatable, so the cache contract is satisfied by construction rather than
  by care.
- **`RelayHandlerRenderer.RenderServerSideHandler`** (`:126-152`) emits the **two-argument**
  `RegisterHandler<T>(typeof(C), async (sp, eventObj, options, ct) => …)` inside
  `if (NeatooRuntime.IsServerRuntime)`. The phase-taking three-argument overload it needs
  already exists and is public (`FactoryEventHandlerRegistry.cs:56`), with the two-arg form
  forwarding to it at `Immediate` (`:32-36`) — so PHASE-001 left this leg ready and the
  emission change is additive.
- **The duplicate-attribute reasoning holds against the actual matching logic.** The
  transform matches handler methods by *shape* (first non-`[Service]`, non-CancellationToken
  parameter of type `T`), not per attribute, so two attributes naming the same `T` resolve to
  the *same* method; two methods matching one event is NF0502, which reports and adds no
  entry at all. A duplicate attribute can therefore only ever produce two identical
  registrations differing in phase — never two distinct handlers. Interim behavior is pinned
  by `FactoryEventPhaseRegistrationTests.RegisterHandler_SameHandlerClassTwoPhases_KeepsTheFirstRegistration`,
  whose comment names PHASE-002 as the decider; that test is in-scope to amend.
- **No existing emission test covers the `RegisterHandler` call.** The Relay Handler region of
  `AssemblyAttributeEmissionTests.cs` (`:530-700`) pins the assembly attribute, the registrar
  holder, and compile-cleanliness — the registration line itself is unasserted. The phase
  assertion is a new assertion class on this leg, not an edit to an existing one. The shared
  fixture carries a documented NF0502 trap (one handler, one event); a multi-phase fixture
  must keep event types distinct.
- **The incremental-cache fixture** (`Core/IncrementalCacheTests.cs`, branch 3) declares two
  attributes for two distinct event types, both unphased. Phasing one populates the new field
  without changing entry count or tripping the NF0502 trap the fixture comments warn about.
- **The end-to-end bullet is reachable**, and the plan review confirmed the preconditions:
  `NeatooRuntime.IsServerRuntime` defaults true and the integration process never sets the
  switch, so the generated guard is open in all three containers; registrar discovery is
  reflective over the assembly attribute (`AddRemoteFactoryServices.cs:171-179`), which every
  `ClientServerContainers` path triggers. Registration happens with no test-side call — which
  is what "no hand-written `RegisterHandler`" needs in order to be falsifiable. Three fixture
  constraints follow and are not optional:
  - **A brand-new event type.** The phase is baked at first registration, process-wide, for the
    run — the static registry still has no test-isolation hook (PHASE-007). Reusing any of the
    events in `FactoryEventPhaseEntryTargets.cs:25-39` or `FactoryEventHandlerTargets.cs` risks
    `PhaseHandlerRegistrations.EnsureRegistered()` winning the race and pinning the wrong phase.
  - **The handler class must be `partial`**, or NF0101 fires with an "Execute delegates" message
    that reads as a non-sequitur here.
  - Containers are cached per `SerializationFormat` (`ClientServerContainers.cs:146`), so
    registration happens once per process — deterministic now that the phase comes from
    generated code rather than a call-order-dependent hand registration.
- **Emit the phase `global::`-qualified.** PHASE-003's code review (C9) qualified the other
  generated type references for namespace-shadowing safety; the registration line should match
  rather than lean on the generated file's `using Neatoo.RemoteFactory;`.

---

## Test Evidence

Unit tests are in `RemoteFactory.UnitTests.FactoryGenerator.AssemblyAttributeEmissionTests`
(namespace elided below); integration tests in
`RemoteFactory.IntegrationTests.Events.Phases.FactoryEventPhaseAttributeTests`.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Attribute-declared `AfterCommit` handler defers and drains, no hand-written registration | `[integration]` | `FactoryEventPhaseAttributeTests.RemoteCreate_AttributeDeclaredPhases_GovernWhenHandlersRun` + `.LogicalCreate_…` | ✓ |
| Handler with no phase argument still dispatches at raise time | `[integration]` | same two tests — the asserted sequence puts `attr-immediate` before `attr-method-done` | ✓ |
| Explicit phase passes through the phase-taking overload, `global::`-qualified, bare form absent | `[unit]` | `RelayHandler_PhasedHandler_RegistersAtTheDeclaredPhase`; `RelayHandler_PhaseArgument_IsGlobalQualified` (negative pin) | ✓ |
| Defaulted handler registers at `Immediate` | `[unit]` | `RelayHandler_UnphasedHandler_RegistersAtImmediate` | ✓ |
| Several event types at different phases each register at their own | `[unit]` | `RelayHandler_SeveralEventTypes_EachRegistersAtItsOwnPhase` | ✓ |
| Duplicate event type reports a Warning naming the surviving phase, one registration not two | `[unit]` | `RelayHandler_DuplicateEventType_ReportsNF0504AsWarning`; `RelayHandler_DuplicateEventType_EmitsOneRegistrationNotTwo` | ✓ |
| `RelayHandler` branch stays cached with phase data populated | `[unit]` | `IncrementalCacheTests.UnrelatedEdit_TransformOutputStaysCached("RelayHandler")` — fixture now declares one phased attribute | ✓ (determinism only, by design — see Notes) |
| Registration stays server-guarded; attribute still names the holder | `[unit]` | `RelayHandler_EmitsAssemblyAttribute`, `RelayHandler_AssemblyAttribute_DoesNotNameConsumerType`, `RelayHandler_RegistrarHolder_ForwardsToUserClass` — pre-existing, unmodified, still green | ✓ |
| Existing suite passes unmodified | `[explicit-skip]` | Step 5 full-suite run; no existing test's assertions were edited (two comments updated, one fixture attribute phased) | n/a |
| Build/test green on both TFMs | `[explicit-skip]` | `reviews/002-build.log`, `reviews/002-test.log` | n/a |

Beyond the bullets, three tests pin decisions the plan records rather than acceptance
signals: `RelayHandler_UndefinedPhaseValue_RendersAsACast` (undefined enum values render
faithfully), `RelayHandler_DuplicateAfterAFailedFirstDeclaration_RepeatsTheOriginalDiagnostic`
(the tracker populates on success only, so NF0501/NF0502 emission counts are unchanged), and
`RelayHandler_PhasedOutput_CompilesWithoutErrors` (the renderer swallows parse failures into a
comment, so string containment alone can pass on uncompilable source).

**Red-proofing:** all four sharp discriminators verified red against deliberate wrong
implementations on both TFMs — see [reviews/002-redproof.log](../reviews/002-redproof.log).
The log also records a first attempt that produced a *false* green (the sabotage failed to
compile, so the tests ran against the correct generator); build-error counts are checked
explicitly in every experiment as a result.

---

## Plan Amendments

*(none yet)*

---

## Notes

- **The duplicate-attribute decision (Step 4), as settled.** `[FactoryEventHandler<T>]` is
  `AllowMultiple = true`, so one class can name the same event twice; the registry dedupes by
  `(event type, handler class type)` and keeps the first. The duplicate can never carry its
  own handler — the transform's method scan is a pure function of the event type, so both
  attributes compute the same match set, and two matching methods is already NF0502. Settled
  at **Warning + skip the duplicate entry**, source order, message naming the surviving phase.
  Two things this leaves on the record:
  - *Why not Error.* The severity split in this generator tracks what gets emitted, not how
    wrong the source is, and a duplicate still produces a working registration. Drafting toward
    Error mis-applied the plan's own stated taxonomy (plan review A-V1). Warning also keeps
    source that compiles at v1.7.0 compiling, so this arc stays a minor release.
  - *Why not last-wins*, which the Discovery Log queued by name: it needs the registry's dedupe
    key widened — a runtime change in PHASE-001's territory, not this plan's — and it makes the
    winner depend on registration order, which for multi-assembly consumers is assembly-scan
    order. Silently picking a winner is the shape this todo exists to remove. Diagnosing is
    strictly better.
  - "A duplicate is always a mistake" holds **given the current `(event type, handler class
    type)` dedupe key**. Widening that key to include phase would make same-class/same-event/
    two-phase representable, and this diagnostic would have to be revisited.
- **What the incremental-cache test actually pins.** It asserts equality of two transform
  outputs across runs, which any deterministic scalar satisfies — including a phase field
  hardcoded to `Immediate`. And because `ReplaceSyntaxTree` reuses the compilation's reference
  manager, even a `TypedConstant` field would likely compare equal and stay green. So it is a
  determinism guard, not a correctness guard, and the primitive-representation rule is a
  Constraint enforced by code review instead (plan review B-V1). Correctness of the phase read
  is pinned by the emission and end-to-end bullets, which can go red.
- **Undefined enum values.** `[FactoryEventHandler<T>((DispatchPhase)99)]` is expressible.
  Faithful pass-through renders the cast and the handler then never drains, since the
  scheduler sweeps only defined phases. Not diagnosing it in this plan — recorded here and in
  the Discovery Log so the choice is visible rather than accidental.
- **Defaulted handlers emit the three-argument overload too** (plan review B-C5): one renderer
  path, and the emission test then pins `Immediate` positively rather than pinning an absence.
  Consequence: the two-argument `RegisterHandler` overload keeps zero generated call sites. It
  stays — it is public API and consumers may call it.
- **A new descriptor needs its `GetDescriptor` switch case** (`FactoryGenerator.cs:150-158`),
  whose default arm throws. Miss it and the first duplicate attribute crashes the generator
  instead of reporting.
- `FactoryEventPhaseRegistrationTests.RegisterHandler_SameHandlerClassTwoPhases_KeepsTheFirstRegistration`
  needs a comment update only. Its assertion exercises the registry API directly, which the
  generator diagnostic cannot reach, so the pinned behavior is unchanged.
- Plan-review opt-in was raised from "No" to "Yes" at drafting, on the strength of Step 4 being
  a contract change. The review returned three vetoes, none of them about Step 4's mechanics —
  worth remembering that the opt-in earned its keep for a reason other than the one that
  triggered it.
