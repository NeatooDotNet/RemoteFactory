# Design Projects, Published Docs, and Skill Updates

**Plan #:** 005
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-14
**Plan-review opt-in:** No (documentation of already-reviewed behavior)
**Code-review opt-in:** No (doc-only; Design code samples are exercised by Design.Tests)

---

## Scope

Update the source-of-truth Design projects with a phased-handler pattern example and
passing tests, update `CLAUDE-DESIGN.md` (pattern narrative and the log-id table), add the
published docs (Jekyll) coverage for dispatch phases, and update the RemoteFactory skill's
factory-events reference — including the proposal's flagged gap that `Immediate` handlers
observe staged (unflushed) state. Skill code samples follow the MarkdownSnippets flow
(reference-app regions + `mdsnippets`) where compilable samples are used. This plan does
NOT change any behavior.

---

## Inherited from PHASE-002 (recorded at its plan review)

PHASE-002 is the plan that makes the attribute's phase real, so these anchors go stale the
day it lands. They are listed concretely because the stub's "document the phase contract"
would not have found them:

- **Prose that becomes conditionally false** — it describes every handler the way only an
  `Immediate` handler now behaves:
  - `docs/attributes-reference.md:218` — "Runs in the caller's DI scope … triggered by
    `IFactoryEvents.Raise` during a factory method. All handlers … sharing the caller's
    `DbContext` and transaction. A throwing handler aborts the chain and propagates to the
    caller." All three clauses are false for an `AfterCommit` handler.
  - `skills/RemoteFactory/references/factory-events.md:115` — "All of them run in the caller's
    scope, sequentially, in unspecified order, **before `Raise` returns**."
- **Diagnostics tables needing the new duplicate-attribute row (NF05xx, Warning):**
  `docs/factory-events.md:370-372`, `skills/RemoteFactory/references/factory-events.md:541-543`,
  `docs/attributes-reference.md:202`.
- **Contract prose worth tightening rather than replacing:** `docs/attributes-reference.md:222`
  and skill `factory-events.md:133` already scope attribute stacking to *several event types* —
  which is the documented basis PHASE-002's diagnostic enforces. Say so explicitly.
- Also inherited from PHASE-003: document "one factory call per scope at a time" as the
  concurrency guidance, and the sync block-drain deadlock caveat.

---

*(Stub — Intent, Alignment, Constraints, Steps, Acceptance filled at Step 2.)*
