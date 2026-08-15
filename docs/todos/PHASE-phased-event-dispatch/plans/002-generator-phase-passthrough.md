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
  already set by NF0501/NF0502 (structural mistakes are errors) versus NF0503 (a migration
  warning).

---

## Constraints & Invariants

- Handlers written with no phase argument keep registering at `Immediate`; the existing
  suite passes unmodified.
- No `DispatchPhase` type reference inside the Generator project — no new link, no new
  using, no name-round-trip through the runtime assembly.
- Registration stays inside the server-runtime guard; nothing new reaches a trimmed client;
  the registrar attribute keeps naming the generated holder.
- The `RelayHandler` incremental branch stays cached across an unrelated edit, with the new
  phase data actually populated on the transform output rather than left at its default.
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
   diagnostic rather than let the registry's first-wins dedupe silently drop the second
   registration. (Severity is the decision this step owns — see Notes.)
5. Pin emission for the defaulted case, the explicit-phase case, and one class declaring
   several event types at different phases.
6. Extend the incremental-cache fixture so the phase data is populated on a transform
   output, not merely present.
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
      the phase-taking overload; the defaulted handler registers at `Immediate`. `[unit]`
- [ ] One handler class declaring several event types at different phases registers each at
      its own phase. `[unit]`
- [ ] Declaring the same event type twice on one handler class reports a diagnostic located
      at the class. `[unit]`
- [ ] The `RelayHandler` incremental branch stays cached across an unrelated edit while the
      fixture populates phase data. `[unit]`
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
- **The end-to-end bullet is reachable.** Attribute-declared handlers already register through
  the generated registrar in the integration assembly
  (`TestTargets/Events/FactoryEventHandlerTargets.cs`), and PHASE-003's phase tests live in
  `Events/Phases/`. The static registry has no test-isolation hook (PHASE-007), so the new
  phased target needs its own event type — the arc's established workaround.
- **Emit the phase `global::`-qualified.** PHASE-003's code review (C9) qualified the other
  generated type references for namespace-shadowing safety; the registration line should match
  rather than lean on the generated file's `using Neatoo.RemoteFactory;`.

---

## Test Evidence

*(Filled after implementation, before the Step 5 gate.)*

---

## Plan Amendments

*(none yet)*

---

## Notes

- **The duplicate-attribute decision (Step 4).** `[FactoryEventHandler<T>]` is
  `AllowMultiple = true`, so one class can name the same event twice; the registry dedupes
  by `(event type, handler class type)` and keeps the first. A duplicate is always a
  mistake — two distinct handler methods for one event on one class is already the NF0502
  ambiguous-match error, so the second attribute can never carry its own handler. Drafting
  toward **Error** severity, matching NF0501/NF0502. The cost is that source which compiles
  today (with the second attribute silently inert) stops compiling; the benefit is that the
  silent-loss shape this todo exists to remove does not get a new instance. Flagged for the
  plan review and for the user.
- **Undefined enum values.** `[FactoryEventHandler<T>((DispatchPhase)99)]` is expressible.
  Faithful pass-through renders the cast and the handler then never drains, since the
  scheduler sweeps only defined phases. Not diagnosing it in this plan — recorded here so
  the choice is visible rather than accidental.
- Plan-review opt-in was raised from "No" to "Yes" at drafting: the stub judged this a
  mechanical pass-through, which the emission work is, but Step 4 changes compile outcomes
  for existing source and that is a contract change.
