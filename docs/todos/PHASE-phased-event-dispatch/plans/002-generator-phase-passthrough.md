# Generator: Phase Pass-Through from Attribute to Registration

**Plan #:** 002
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-14
**Plan-review opt-in:** No (mechanical pass-through; the API shape was reviewed in PHASE-001)
**Code-review opt-in:** Yes (generator emission change; incremental-cache equality contract at stake)

---

## Scope

Teach the generator to read the `DispatchPhase` argument from `[FactoryEventHandler<T>]`
and thread it through the relay-handler model into the generated
`FactoryEventHandlerRegistry.RegisterHandler` call, preserving the incremental-cache
equality contract on the handler model and the v1.7.0 forwarding-holder trimming pattern.
Covers generator tests (emitted-source assertions and incremental-cache tracking) for both
the defaulted and explicit-phase cases. This plan does NOT add runtime behavior (PHASE-001
owns the runtime) and does NOT touch factory-method emission (PHASE-003).

---

*(Stub — Intent, Alignment, Constraints, Steps, Acceptance filled at Step 2.)*
