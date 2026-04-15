# Requirements Reviewer — Delete [Event] Method Attribute API

Last updated: 2026-04-14
Current step: Step 2 Pre-Design Review — COMPLETE

## Key Context

- This is a **deliberate breaking-change plan** for v1.5.0. Contradictions with
  existing business requirements are *expected*; the job is to **enumerate
  them** so the Step 7 documenter closes the loop.
- `ICorrelationContext` stays (used by `MakeRemoteDelegateRequest` for relay
  correlation IDs in v1.4.0 `IFactoryEventRelay` path). The correlation scope
  initializer (`CorrelationContextScopeInitializer`) goes — it only existed to
  propagate the correlation ID across `[Event]` isolated scopes.
- `[FactoryEventHandler<T>]` + `IFactoryEvents.Raise` + `IFactoryEventRelay`
  are all **unaffected** by this plan. Verified: none of them use
  `IEventTracker`, `IEventScopeInitializer`, or `EventTrackerHostedService`.

## Verdict

**APPROVED (with gaps to close before implementation)**

The plan is internally consistent, the intended breaking contradictions are
all documented and acknowledged as Step 7 follow-up, and no silent implicit
dependency will break. However the plan **underspecifies the Design project,
Design.Tests, and reference-app files** that actually contain `[Event]` usage.
The architect (Step 3) should expand the Files Modified list using the
evidence below before implementation begins. These are gaps, not
contradictions — hence Approved-with-gaps rather than Vetoed.

## Relevant Requirements Found (the contradictions that must close in Step 7)

### Code-based (Design projects)

| Source | Rule / Guarantee |
|--------|------------------|
| `src/Design/CLAUDE-DESIGN.md:40` | Static Factory pattern is listed with `[Execute]` OR `[Event]` operations |
| `src/Design/CLAUDE-DESIGN.md:197-199` | "When to use `[Event]` delegates instead" guidance for fire-and-forget |
| `src/Design/CLAUDE-DESIGN.md:256` | Quick Decisions: `[Event]` methods need CancellationToken as final parameter |
| `src/Design/CLAUDE-DESIGN.md:267` | Common Mistakes: "handler to fire-and-forget" -> Use `[Event]` |
| `src/Design/CLAUDE-DESIGN.md:273-274` | `AddRemoteFactoryEventScopeInitializer`, `IEventScopeInitializer`, `CorrelationContextScopeInitializer` documented as the tenant/correlation propagation mechanism |
| `src/Design/CLAUDE-DESIGN.md:659-682` | "Event Scope Initialization (IEventScopeInitializer)" subsection — the generator-behavior documentation |
| `src/Design/CLAUDE-DESIGN.md:955,961,1045` | Design-project checklist entries for `[Event]` + `IEventScopeInitializer` |
| `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:283-445` | `ExampleEvents` static factory with `[Remote, Event] _OnOrderPlaced` — the authoritative `[Event]` example |
| `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:34-37,81-83` | Comments cross-referencing `[Event]` as the alternative for fire-and-forget |
| `src/Design/Design.Domain/Services/CorrelationExample.cs` | Entire `CorrelatedOperations` class + XML comments about `CorrelationContextScopeInitializer` / `AddRemoteFactoryEventScopeInitializer` |
| `src/Design/Design.Tests/FactoryTests/StaticFactoryTests.cs:80-130` | Two tests depend on the generated `ExampleEvents.OnOrderPlacedEvent` delegate (`Event_OnOrderPlaced_FiresWithoutBlocking`, `Event_WorksInLocalMode`) |
| `src/Design/Design.Client.Blazor/Pages/Home.razor:91,137,144,232-233` | UI demo injects and invokes `ExampleEvents.OnOrderPlacedEvent` |

### Doc-based (published)

| Source | Rule / Guarantee |
|--------|------------------|
| `docs/events.md` | Entire page documents `[Event]`, `IEventTracker`, `IEventScopeInitializer`, graceful shutdown |
| `docs/factory-events.md` | Compares `[FactoryEventHandler<T>]` vs `[Event]` |
| `docs/attributes-reference.md` | `[Event]` attribute row |
| `docs/interfaces-reference.md` | `IEventTracker`, `IEventScopeInitializer` entries |
| `docs/trimming.md` | `IEventTracker` note about server-only trimming |
| `docs/aspnetcore-integration.md` | `EventTrackerHostedService` registration |
| `docs/factory-operations.md` | `[Event]` as a factory operation |
| `docs/release-notes/v0.24.2.md`, `v0.28.0.md`, `v1.1.0.md`, `v0.6.0.md` | Historical release notes mention these types (keep the historical notes; don't edit them) |
| `skills/RemoteFactory/SKILL.md`, `skills/RemoteFactory/references/factory-events.md`, `skills/RemoteFactory/references/static-factory.md` | Skill content covers `[Event]` |

All of these are contradictions that MUST be reflected in the Step 7
documentation pass (CLAUDE-DESIGN.md edit, docs rewrite, skill regeneration,
release notes v1.5.0 migration guide). The plan already lists this in its
"Documentation (Step 7, after verification)" section — good.

## Implicit Dependencies — Verified Clear

| Question | Answer | Evidence |
|----------|--------|----------|
| Does `[FactoryEventHandler<T>]` / `IFactoryEvents.Raise` depend on `IEventTracker` or `IEventScopeInitializer`? | **No** | Grep of `src/RemoteFactory/FactoryEventsDispatcher.cs`, `RemoteFactoryEvents.cs`, `IFactoryEvents.cs` shows zero references to either type |
| Does `IFactoryEventRelay` (v1.4.0) depend on any deleted type? | **No** | The relay path uses `ICorrelationContext` (KEPT) via `MakeRemoteDelegateRequest`, not the scope-initializer stack |
| Does `ICorrelationContext` have cross-cutting behavior beyond relay correlation ID? | **No** | Grep confirms `ICorrelationContext` consumer is `MakeRemoteDelegateRequest` + user-code `[Service]` injection. The scope initializer was the only downstream consumer, and its removal is correctly scoped by the plan |
| Does `AuthorizeFactoryOperation.Event = 512` have any internal consumer beyond the `[Event]` pipeline? | **No** | Only `FactoryOperation.cs` defines it and `FactoryAttributes.cs` wires it up. `AuthorizeFactoryOperation` is widely used across auth code but no grep hit has `.Event` on the flag. Plan risk #8 states this correctly |
| Is `RemoteEventIntegrationTests` actually an `[Event]`-feature test (i.e. safe to delete)? | **Yes** | Read confirms it tests `SendOrderConfirmationEvent` / `NotifyWarehouseEvent` delegates (the `[Event]` generator output), NOT `IFactoryEvents.Raise`. Safe to delete |
| Is `FactoryEventHandlerCoexistenceTests` testing `[Event]` + `[FactoryEventHandler]` coexistence (i.e. moot post-deletion)? | **Yes** | Read confirms it explicitly uses `OrderEventHandler.NotifyWarehouseEvent` AND `factoryEvents.Raise(new TestOrderEvent(...))` in the same test. Coexistence scope evaporates when `[Event]` is gone. Safe to delete |

## Gaps the Architect Must Close Before Implementation

The plan's Files Modified list under-enumerates the actual surface area. Specifically:

1. **`src/Design/Design.Tests/FactoryTests/StaticFactoryTests.cs`** — two
   tests (`Event_OnOrderPlaced_FiresWithoutBlocking`,
   `Event_WorksInLocalMode`) depend on the deleted `[Event]` in AllPatterns.
   Plan doesn't mention this file. Add "delete these two test methods" or
   "rewrite using `[FactoryEventHandler<T>]`" to the plan.

2. **`src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:34-37,81-83`**
   — contains `[Event]` cross-references in comments. Plan doesn't enumerate.
   Comments must be updated (or deleted) so the Design surface doesn't
   advertise a pattern that no longer exists.

3. **`src/Design/Design.Client.Blazor/Pages/Home.razor:91,137,144,232-233`**
   — plan says "remove any `[Event]` UI demo" generically; architect should
   enumerate the specific blocks: the paragraph at line 91, the injection at
   line 144, the click handler at lines 232-233.

4. **Reference-app files not enumerated by the plan (plan lists 11, grep
   finds 26 with `[Event]` or `IEventTracker`):**
   - `src/docs/reference-app/EmployeeManagement.Domain/Events/DepartmentEventHandlers.cs` (contains `[Event]`)
   - `src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs` (contains `[Event]`)
   - `src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/EventSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/PatternSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Application/Samples/Events/EventCallerSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Application/Samples/Events/EventTrackerSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Application/Samples/Interfaces/EventTrackerSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Application/Samples/Operations/EventOperationSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs`
   - `src/docs/reference-app/EmployeeManagement.Domain/EmployeeManagement.Domain.csproj` (MarkdownSnippets regions list?)
   - `src/docs/reference-app/EmployeeManagement.Application/EmployeeManagement.Application.csproj`

   Plan Risk #5 says "reference-app build must pass after the deletions". The
   under-enumeration here is the biggest risk to that risk actually being met.
   The architect should run grep across `src/docs/reference-app/` for
   `\[Event\]|IEventTracker|IEventScopeInitializer|EventTrackerHostedService|AddRemoteFactoryEventScopeInitializer`
   and reconcile against the plan's Files Modified list before starting Phase 4.

5. **`CorrelationExample.cs` handling.** Plan says "delete or rewrite for
   `IFactoryEvents.Raise` only". This is under-specified — the file's
   `CorrelatedOperations` class contains `[Remote, Event] _OnOrderProcessed`
   which will not compile without `[Event]`. Architect should pick one
   concrete approach (delete file vs. rewrite static factory as Execute-only
   vs. rewrite as `[FactoryEventHandler<T>]`) and update the plan.

6. **CLAUDE-DESIGN.md specific lines to update.** Plan mentions it generically
   but architect should enumerate the line ranges (see "Relevant Requirements
   Found" table above) for the Step 7 documenter to close.

7. **Test Scenario 2 (`[Event]` consumer compile fails with CS0246`).**
   Scenario requires actual compilation against a snippet. The plan's test
   infrastructure (snapshot-based generator tests) may not support "expect
   CS0246 at C# level". Architect should confirm how Scenario 2 is actually
   going to be exercised — if the harness can't fail on CS0246, this scenario
   is not testable and should be restated as "grep absence of
   `EventAttribute` from `src/RemoteFactory/`" (which would be subsumed by
   Scenario 1).

## Migration Guide — Sanity Check

The plan points consumers at `Task.Run` + `IServiceScopeFactory.CreateScope()`.
Edge cases worth documenting in the v1.5.0 release notes:

- **Graceful shutdown** — `[Event]` wired `IHostApplicationLifetime.ApplicationStopping`
  into the CancellationToken and `IEventTracker` tracked in-flight work for
  graceful drain. Consumers migrating must re-wire both (inject `IHostApplicationLifetime`
  into the parent scope; track outstanding tasks themselves) or accept that
  their fire-and-forget work may be killed on shutdown.
- **Correlation ID propagation** — was automatic via
  `CorrelationContextScopeInitializer`. In the manual pattern the caller
  must snapshot `ICorrelationContext.CorrelationId` in the request scope and
  re-set it on the new scope, OR inject `ICorrelationContext` into the parent
  scope and copy the value in.
- **Tenant/ambient context** — whatever the consumer had registered as
  `IEventScopeInitializer` must become explicit snapshot-and-copy code in
  their `Task.Run` body.

These three points should appear in the v1.5.0 migration guide (Step 7).

## Mistakes to Avoid

- Do not approve silently with "plan looks fine" — the file under-enumeration
  is real and will cause reference-app build failures if the architect
  doesn't expand the list.
- Do not veto over documented contradictions — they are the entire point of
  this plan. The reviewer's job is to ensure the Step 7 documentation
  backlog covers them, which the plan's Documentation section does.
- Do not flag `ICorrelationContext` removal as a risk — the plan correctly
  keeps it (only the scope-initializer stack goes).

## User Corrections

None yet this run.

## Recommendations for Architect

1. **Expand Files Modified** using the gap list above, especially the 15+
   reference-app files not enumerated.
2. **Pick a concrete rewrite for `CorrelationExample.cs`** (delete vs.
   rewrite) and commit to it in the plan.
3. **Enumerate the specific `Design.Tests/StaticFactoryTests.cs` test deletions.**
4. **Enumerate the specific CLAUDE-DESIGN.md line ranges** that Step 7 must
   rewrite so the documenter has a concrete checklist.
5. **Reconcile Test Scenario 2** with what the harness can actually verify.
6. **Confirm the v1.5.0 migration guide covers the three edge cases** above
   (graceful shutdown, correlation propagation, tenant context).

---

## Step 6 Verification (2026-04-15)

**Verdict:** REQUIREMENTS SATISFIED

Step 6B scope: Design source of truth (`src/Design/**/*.cs`, `*.razor`, Design.Tests) must match the implementation. Published docs and `CLAUDE-DESIGN.md` line-edits are explicitly Step 7 per the plan (lines 115-116, 270-271) and are NOT in Step 6B scope. This verdict verifies the **code-based Design surface** is consistent with the implementation; doc updates remain a known Step 7 handoff.

### Design Source Scan — Deleted Symbols

Executed against `src/Design/**/*.cs` and `src/Design/**/*.razor` (excluding `obj/`, `Generated/`, and `.md`):

| Symbol | Result in Design code |
|--------|----------------------|
| `[Event]` / `EventAttribute` | **Absent** — 0 matches |
| `IEventTracker` | **Absent** — 0 matches |
| `IEventScopeInitializer` | **Absent** — 0 matches |
| `EventTrackerHostedService` | **Absent** — 0 matches |
| `AddRemoteFactoryEventScopeInitializer` | **Absent** — 0 matches |
| `CorrelationContextScopeInitializer` | **Absent** — 0 matches |
| `DelegateEventScopeInitializer` | **Absent** — 0 matches |
| `FactoryOperation.Event` | **Absent** — 0 matches |
| `AuthorizeFactoryOperation.Event` | **Absent** — 0 matches |
| `NF0401`–`NF0404` | **Absent** — 0 matches |
| `ExampleEvents` / `OnOrderPlacedEvent` / `SendOrderConfirmationEvent` / `NotifyWarehouseEvent` | **Absent** — 0 matches |
| `Event_OnOrderPlaced_*` / `Event_WorksInLocalMode` tests | **Absent** — 0 matches |

File-level:
- `src/Design/Design.Domain/Services/CorrelationExample.cs` — **deleted** (entire `Services/` folder empty per Glob).
- `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` — **retained** but `ExampleEvents` static factory removed; only Class/Interface/Static-Execute demos remain. Grep confirms 0 `[Event]` references in this file.
- `src/Design/Design.Client.Blazor/Pages/Home.razor` — **clean** (0 matches for any deleted symbol).
- `src/Design/Design.Tests/FactoryTests/StaticFactoryTests.cs` — the two `[Event]`-dependent tests (`Event_OnOrderPlaced_FiresWithoutBlocking`, `Event_WorksInLocalMode`) are gone.

### Design Source Scan — Kept Symbols Still Demonstrated

| Kept guarantee | Demonstrated at |
|----------------|----------------|
| `FactoryEventBase` / `FactoryEventAttribute` (v1.4 event type base + inherited trim annotation) | `Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:42-44,153,160`, `FactoryEventRelayPattern.cs:12-19,58-63` |
| `[FactoryEventHandler<T>]` server handler (Pattern 4) + `IFactoryEvents.Raise<T>` dispatch | `FactoryEventHandlerPattern.cs:82,101,160`; tests at `Design.Tests/FactoryTests/FactoryEventHandlerTests.cs:26,46,72` (Raise dispatch; unhandled event; multi-handler) |
| `IFactoryEventRelay` v1.4 client relay | `FactoryEventRelayPattern.cs`; tests at `Design.Tests/FactoryTests/FactoryEventRelayTests.cs:24,107` (register, invoke, NoOp default) |
| `ICorrelationContext` KEPT for relay correlation | `src/RemoteFactory/AddRemoteFactoryServices.cs:60` (scoped registration) + `HandleRemoteDelegateRequest.cs:66` + `MakeRemoteDelegateRequestHttpCall.cs:10` — Design.Tests relay tests exercise this implicitly via `DesignClientServerContainers` |
| `IMakeRemoteDelegateRequest.ForDelegateEvent` | `src/RemoteFactory/RemoteFactoryEvents.cs:23,28` → `MakeRemoteDelegateRequest.cs:21,167` (invoked by the kept relay path) |
| `NF0405` (event type not `FactoryEventBase`) | Library retains it (matches listed in Internal diagnostics; no Design.Tests explicitly exercise it, but the plan's Scenario 1 covers generator tests) |

### Library-Side Scan (Complementary)

Searched `src/RemoteFactory/`, `src/RemoteFactory.AspNetCore/`, `src/Generator/`:
- All deleted symbols: **0 residual references** (the only `EventAttribute` hits resolve to `FactoryEventAttribute`, which is the KEPT v1.4 class).
- Kept symbols intact: `ICorrelationContext`, `IFactoryEventRelay`, `NoOpFactoryEventRelay`, `ForDelegateEvent`, `FactoryEventAttribute`, `FactoryEventBase`, `FactoryEventTypeRegistry`.

### Side-Effect Analysis

| Pipeline | Impact | Evidence |
|----------|--------|----------|
| v1.4 relay flow (`IFactoryEventRelay.Relay`) | **Intact** | `FactoryEventRelayTests.cs`, `RelayTimingTests.cs` untouched; `MakeRemoteDelegateRequest` still constructs with `ICorrelationContext` and optional `IFactoryEventRelay`; `HandleRemoteDelegateRequest` still resolves `ICorrelationContext` for correlation ID propagation on the server |
| `[FactoryEventHandler<T>]` + `IFactoryEvents.Raise<T>` dispatch (Pattern 4) | **Intact** | `FactoryEventHandlerPattern.cs` unchanged; `FactoryEventHandlerTests.cs` still asserts dispatch, unhandled-event behavior, and multi-attribute handlers |
| `ICorrelationContext` (scoped) for request correlation | **Intact** | `AddRemoteFactoryServices.cs:60` registration retained; scope-initializer consumer removed, but direct `[Service]` injection into factory methods still works; `MakeRemoteDelegateRequestHttpCall` still flows the correlation ID to the server |
| Generator pipeline | **Intact** | 0 references to deleted symbols in `src/Generator/`; NF0405 retained for the kept relay-attribute diagnostic |

No unintended side effects detected. The v1.4.0 relay redesign and the `[FactoryEventHandler<T>]` mediator pattern are both preserved exactly as documented in Pattern 4.

### Published-Doc / CLAUDE-DESIGN Work — Deferred to Step 7 (Handoff)

These are **NOT violations of Step 6B** — the plan explicitly schedules them for Step 7. Listed here for the documenter:

- `src/Design/CLAUDE-DESIGN.md` — lines 40, 197-199, 256, 267, 273-274, 659-682, 955, 961, 1045 still reference `[Event]`, `IEventTracker`, `IEventScopeInitializer`, `CorrelationContextScopeInitializer`, `AddRemoteFactoryEventScopeInitializer`, and `AllPatterns.cs:417-427`. Per plan line 271, these are the exact line ranges the documenter must rewrite.
- `docs/events.md` — entire page stale.
- `docs/factory-events.md` — `[Event]` vs `[FactoryEventHandler<T>]` comparison stale.
- `docs/attributes-reference.md` — `[Event]` row.
- `docs/interfaces-reference.md` — `IEventTracker` / `IEventScopeInitializer` rows.
- `docs/trimming.md` — `IEventTracker` trimming note.
- `docs/aspnetcore-integration.md` — `EventTrackerHostedService` registration.
- `docs/factory-operations.md` — `[Event]` factory operation section.
- `docs/release-notes/v1.5.0.md` — must be created with migration guide (graceful shutdown, correlation propagation, tenant context) per Step 2 migration sanity check.
- `skills/RemoteFactory/SKILL.md`, `references/factory-events.md`, `references/static-factory.md` — skill regeneration (after reference-app MarkdownSnippets edits).
- Historical release notes (`v0.24.2.md`, `v0.28.0.md`, `v1.1.0.md`, `v0.6.0.md`) — leave as historical record.

### Issues Found

None. Design source of truth is clean and consistent with the implementation.

### Final Verdict

**REQUIREMENTS SATISFIED.** The v1.5.0 deletion is correctly mirrored in the Design projects: all deleted symbols are absent from Design code, all kept behavioral guarantees (v1.4 relay, `[FactoryEventHandler<T>]` dispatch, `ICorrelationContext` for correlation, `ForDelegateEvent`) are still demonstrated by Design.Domain patterns and Design.Tests. Documentation updates (`CLAUDE-DESIGN.md`, `docs/*.md`, skill, v1.5.0 release notes) are known Step 7 work and are not requirements violations at this checkpoint.
