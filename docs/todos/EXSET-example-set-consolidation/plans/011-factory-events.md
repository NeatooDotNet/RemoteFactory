# Factory events page

**Plan #:** 011
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-15
**Plan-review opt-in:** No. Design already holds every event shape the page teaches (`FactoryEventHandlerPattern.cs`, `FactoryEventPhasesPattern.cs`, `FactoryEventRelayPattern.cs`), with their tests. This plan adds regions and one `CancellationToken`.
**Code-review opt-in:** Yes. The page is restated against the event runtime, and C-06 changes every raising example.
**Branch:** EXSET-011-factory-events
**PR:** —

---

## Scope

Re-point every C# block on `docs/factory-events.md` to `src/Design`, or turn it into prose or a short WRONG block. `docs/events.md` is a redirect stub with no C#, and stays.

**Split at Step 2 (2026-10-15):** `trimming.md`, the appendix pages and the LazyLoad removal move to EXSET-016. They share no region with this page.

**New Design regions, on code the event tests already pin:**
- `events-event`, on `OrderCheckoutCompleted`;
- `events-raise`, on CheckoutOrder's `[Remote, Insert]`;
- `events-serveronly`, on its `[Remote, Update]`;
- `events-phase-handlers`, on the three `InvoiceFinalizedEvent` handlers;
- `events-phase-drain`, on `InvoiceFinalizer._Finalize`;
- `events-coalesce`, on `StatementProjection`;
- `events-relay`, on `InMemoryAggregatorRelay`;
- `events-relay-register`, on the relay tests' client registration;
- `events-client-raise`, on the client-raise test.

**Reused:** `attributes-factoryeventhandler`, for the server handler.

**Becomes prose:**
- library signatures (`Raise<T>`, `IFactoryEventRelay`, `RemoteResponseDto`);
- stacked handler attributes;
- the complete example, which the regions above already make up.

**WRONG blocks:**
- `[Factory]` on a handler class: a probe compile gives CS0111;
- a `void` handler: NF0501.

**Findings to fix:**
- C-06: every raising example raises from an `[Insert]`/`[Update]` or a `[Remote, Execute]`, not a `[Create]`;
- I-08: where events are raised;
- T-18: the handler rules say `static`;
- T-19: registration names, and Remote-mode `IFactoryEvents`;
- this page's share of C-19.

Does NOT touch:
- the skill's `factory-events.md` prose (Todo 2);
- `trimming.md` (EXSET-016).

---

## Intent

The page shows events raised where the rulings put them, in a write or a command, never a `[Create]`. Every example is Design code its tests pin.

---

## Framework & Architectural Alignment

- **Rulings:** D3 (writes belong in Insert/Update), D14, R-E and R-M.
- **`.claude/rules/design-snippets.md`.**
- **The attributes reference (EXSET-014),** which this page links for `[FactoryEventHandler<T>]` and `[FactoryEvent]`.

---

## Constraints & Invariants

- **Embeds.** Every C# block on the page is a Design embed or a short WRONG block. Text diagrams stay.
- **Regions.** Each region name is defined once.
- **The whole still works:** both solutions green, and mdsnippets swallows nothing.

---

## Steps

1. The new Design regions. CheckoutOrder's Insert takes and passes its `CancellationToken`, as the page teaches.
2. Rewrite `factory-events.md`.
3. Verify and record: mdsnippets, both solutions, the ledger.

---

## Acceptance

- [ ] Every C# block on `factory-events.md` is a Design embed or WRONG `[explicit-skip: grep]` · Must
- [ ] The page's statements match the event runtime and generator `[explicit-skip: source cited; probe compile for the WRONG blocks]` · Must
- [ ] The embedded regions are pinned by Design tests, including the `CancellationToken` CheckoutOrder now passes `[integration]` · Must
- [ ] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **`factory-events.md`:** 632 lines, no embeds, 19 hand fences.
- **Its raising examples are `[Remote, Create]`** (C-06).
- **Its "Right" handlers are instance methods** (T-18).
- **Its DI table names `AddRemoteFactoryServices` and `FactoryMode`, and calls Remote-mode `IFactoryEvents` "no relay".** The source registers `RemoteFactoryEvents`, which sends to the server (T-19).
- **Probe compile (2026-10-15):**
  - `[Factory]` with `[FactoryEventHandler<T>]` on a static class is CS0111 (both pipelines emit `FactoryServiceRegistrar`);
  - an `async void` handler is NF0501.

---

## Punchlist

---

## Test Evidence

---

## Gate Record

---

## Plan Amendments

---

## Notes

- **Run unattended** under Keith's standing instruction.
