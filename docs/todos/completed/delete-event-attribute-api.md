# Delete `[Event]` Method Attribute API

**Status:** Complete
**Priority:** High
**Created:** 2026-04-15
**Last Updated:** 2026-04-14

---

## Problem

RemoteFactory ships two unrelated event-shaped features that share the word "event":

1. **`[FactoryEventHandler<T>]` + `IFactoryEvents.Raise<T>(...)`** — domain events raised inside a factory method, dispatched in the caller's DI scope, awaited; with optional client-relay piggyback.
2. **`[Event]` method attribute** — a fire-and-forget delegate generator that runs the method in an isolated DI scope via `IServiceScopeFactory.CreateScope()`, tracked by `IEventTracker` for graceful shutdown, propagating ambient context via `IEventScopeInitializer`.

The two features have completely different execution models, but the shared "event" terminology has caused real confusion — including in our own work last session, where we had to repeatedly remember that a CI-flaky `EventTrackerTests` was an `[Event]`-feature test, not a factory-event test.

The `[Event]` API also brings infrastructure surface that is otherwise unjustified: `IEventTracker`, `EventTrackerHostedService`, `IEventScopeInitializer`, `AddRemoteFactoryEventScopeInitializer`, `CorrelationContextScopeInitializer`, NF0401-NF0404 diagnostics. A consumer who needs scope-isolated fire-and-forget work can build it with `Task.Run` + `IServiceScopeFactory.CreateScope()` directly without any RemoteFactory primitive.

## Solution

Delete the entire `[Event]` API surface and its supporting infrastructure. Released as **v1.5.0** (minor bump per pre-1.0-API-stability framing — same convention used for v1.4.0).

Keep `ICorrelationContext` (used by `MakeRemoteDelegateRequest` for relay correlation IDs); delete only the scope-initializer stack that propagated it across event scopes.

---

## Requirements Review

**Verdict:** APPROVED
**Reviewed:** 2026-04-15
**Summary:** Plan is internally consistent; all breaking contradictions with documented requirements are deliberate and scheduled for Step 7. Reviewer flagged 7 gaps in Files Modified coverage (15+ reference-app files under-enumerated, `Design.Tests/StaticFactoryTests.cs` has two `[Event]`-dependent tests, `CorrelationExample.cs` needed a concrete decision — now deleted). Plan updated accordingly before architect validation.

---

## Plans

- [Delete `[Event]` Method Attribute API Plan](../plans/delete-event-attribute-api.md)

---

## Tasks

- [x] Draft plan (Step 1)
- [x] Business requirements review (Step 2) — APPROVED
- [x] Architect validation (Step 3) — Approved after 7 concerns folded in
- [x] Implementation (Step 4) — 14 phases, all green
- [x] Developer code review (Step 5) — Approved, all 16 rules traced
- [x] Verification — architect + requirements (Step 6) — VERIFIED + REQUIREMENTS SATISFIED
- [x] Requirements documentation + general docs (Step 7) — Part A + Part B complete
- [x] Completion (Step 8)

---

## Progress Log

### 2026-04-15
- User directive after PR #64 (factory-events-relay-redesign v1.4.0) merged: "delete the non-factoryevent [events] api. it confused the hell out of us!"
- Decisions captured in conversation:
  - **Version**: v1.5.0 (minor bump, breaking)
  - **`ICorrelationContext`**: keep (used by `MakeRemoteDelegateRequest`); delete only the scope-initializer stack that propagated it
  - **Sequencing**: branched off `main` after PR #64 merged
- Branch: `feat/delete-event-attribute-api-v1.5.0`
- Next: complete plan draft, run requirements review.

---

## Completion Verification

- [x] All builds pass (main + Design + reference-app, Release, net9.0 + net10.0)
- [x] All tests pass

**Verification results** (architect Step 6A, 2026-04-14):
- Build: 0 errors across all 3 solutions (main, Design, reference-app). 2 pre-existing WASM NativeFileReference warnings on `OrderEntry.BlazorClient` (unrelated).
- Tests: 553 unit + 563 integration × 2 TFMs = 2,232 passed; 6 skipped (3 pre-existing perf-demo × 2 TFMs). 72 × 2 TFMs = 144 Design tests passed. Zero failures.

---

## Results / Conclusions

Shipped v1.5.0 — deleted the entire `[Event]` method attribute API surface and its supporting infrastructure, along with NF0401-NF0404 diagnostics and log event IDs 9001-9009.

**What was removed**:
- Attribute: `[Event]` / `EventAttribute`
- Enums: `FactoryOperation.Event`, `AuthorizeFactoryOperation.Event = 512`
- Infrastructure: `IEventTracker`, `EventTracker`, `EventTrackerHostedService`, `IEventScopeInitializer`, `DelegateEventScopeInitializer`, `CorrelationContextScopeInitializer`
- DI extension: `AddRemoteFactoryEventScopeInitializer`
- Generator paths: `EventMethodModel`, `Events` collections on model records, all event-related rendering in both static and class renderers, `BuildEventMethod` helper

**What was kept** (as planned):
- `ICorrelationContext` + scoped DI registration (still serves `MakeRemoteDelegateRequest` for relay correlation IDs)
- `FactoryEventAttribute` on `FactoryEventBase` (distinct from the deleted `EventAttribute`)
- `[FactoryEventHandler<T>]` + `IFactoryEvents.Raise<T>` v1.4 domain-event pipeline
- `IFactoryEventRelay` v1.4 client-side relay
- `IMakeRemoteDelegateRequest.ForDelegateEvent` (v1.4 relay path)
- Diagnostic NF0405 (FactoryEventHandlerMustBeStatic)

**Migration path** for consumers (documented in `docs/release-notes/v1.5.0.md`): manual `Task.Run` + `IServiceScopeFactory.CreateScope()`, with explicit snapshot-and-copy for correlation ID / tenant / ambient context from parent scope, and manual task tracking for graceful shutdown via `IHostApplicationLifetime.ApplicationStopping`.

**Scope highlights**:
- Step 4 implementation: 14 phases, 7 production files deleted, 8 test files deleted, 17 reference-app files deleted, 10+ reference-app files edited, `IEventTestService` relocated to dedicated file.
- Step 7 docs: `CLAUDE-DESIGN.md` cleaned of ~10 stale sections; 9 published doc pages updated or rewritten; `docs/events.md` stubbed as deprecation redirect; 4 skill reference files updated; mdsnippets regenerated cleanly; README feature bullet updated; `docs/release-notes/v1.5.0.md` created with migration guide.
- Version: `src/Directory.Build.props` bumped 1.4.0 → 1.5.0.

No defects found in verification. Ready for PR.
