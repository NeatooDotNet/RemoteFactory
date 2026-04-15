# Delete `[Event]` Method Attribute API Plan

**Date:** 2026-04-15
**Related Todo:** [Delete `[Event]` Method Attribute API](../todos/delete-event-attribute-api.md)
**Status:** Complete
**Last Updated:** 2026-04-14

<!-- Valid status values (do not render in plan):
Draft | Under Review (Architect) | Concerns Raised (Architect) | Ready for Implementation |
In Progress | Awaiting Code Review | Code Review Concerns | Awaiting Verification | Sent Back |
Requirements Documented | Documentation Complete | Complete
-->

---

## Overview

Delete the entire `[Event]` method attribute API and its supporting infrastructure (`IEventTracker`, `IEventScopeInitializer`, `EventTrackerHostedService`, `AddRemoteFactoryEventScopeInitializer`, `CorrelationContextScopeInitializer`, NF0401-NF0404 diagnostics, generator paths). The feature created confusion with the `[FactoryEventHandler<T>]` factory-event surface despite serving a completely different purpose (fire-and-forget scope-isolated work vs. transactional domain events).

Released as **v1.5.0** (minor bump per pre-1.0-API-stability framing). `ICorrelationContext` is **kept** — it's used by `MakeRemoteDelegateRequest` for relay correlation IDs, independent of the event-scope plumbing being removed.

---

## Skills

- `skills/knockoff/SKILL.md` — For stub patterns in any tests that need rewriting

Sources of truth: `src/Design/CLAUDE-DESIGN.md`, `src/Design/Design.Domain/`, source code. The RemoteFactory skill is output-only and is not loaded.

---

## Business Rules (Testable Assertions)

### Surface Removal

1. WHEN the codebase is searched after this change, THEN no production source defines or references `EventAttribute`, `[Event]`, `FactoryOperation.Event`, `AuthorizeFactoryOperation.Event`, `IEventTracker`, `EventTracker`, `IEventScopeInitializer`, `DelegateEventScopeInitializer`, `CorrelationContextScopeInitializer`, `EventTrackerHostedService`, `AddRemoteFactoryEventScopeInitializer`, or `EventMethodModel` — NEW

2. WHEN a consumer attempts to compile code with `[Event]` on a method, THEN the C# compiler emits CS0246 (`type or namespace 'Event' could not be found`) — NEW (the attribute type is gone; no NF-prefixed RemoteFactory diagnostic is emitted because the generator no longer scans for `[Event]`)

3. WHEN diagnostics are reviewed post-change, THEN `NF0401`, `NF0402`, `NF0403`, `NF0404` are removed from `DiagnosticDescriptors.cs` and from `FactoryGenerator.GetDescriptor` — NEW

### Generator

4. WHEN the source generator runs against a class with `[Factory]` and methods carrying any other operation attribute (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]`), THEN generated code continues to compile and all existing non-event factory tests in `RemoteFactory.UnitTests`, `RemoteFactory.IntegrationTests`, and `Design.Tests` pass — NEW (only the `[Event]` paths are removed; non-event paths must not regress. Verified by rule 16's full suite green, not a dedicated snapshot harness)

5. WHEN the source generator runs against a class that previously had a mix of `[Event]` methods and other operation methods, THEN the `[Event]` methods are silently dropped (the attribute symbol no longer exists, so the methods are unannotated) and the non-event methods generate exactly as before — NEW

### Remaining Event-Shaped Surface

6. WHEN `IFactoryEvents.Raise<T>(...)` is called with a `FactoryEventBase` descendant on the server, THEN every server-side static-method `[FactoryEventHandler<T>]` handler dispatches sequentially in the caller's DI scope and is awaited — existing (Pattern 4 in CLAUDE-DESIGN.md, unchanged)

7. WHEN a `[Remote]` factory call raises events on the server, THEN the client's registered `IFactoryEventRelay.Relay(...)` is invoked once with the deserialized batch (or empty batch) — existing (v1.4.0 behavior, unchanged)

### DI Registration

8. WHEN `AddNeatooRemoteFactory(NeatooFactory.Server)` is called, THEN `IEventTracker` is NOT registered — NEW (the type no longer exists)

9. WHEN `AddNeatooRemoteFactory(NeatooFactory.Logical)` is called, THEN `IEventTracker` is NOT registered — NEW

10. WHEN `AddNeatooRemoteFactory(...)` is called in any mode, THEN `IEventScopeInitializer` is not registered and the related `services.AddTransient<IEventScopeInitializer, ...>` line is removed from `AddRemoteFactoryServices.cs` — NEW

11. WHEN `AddNeatooRemoteFactory(...)` is called, THEN `ICorrelationContext` is still registered as scoped — existing (used by `MakeRemoteDelegateRequest` for relay correlation; keep)

12. WHEN the `Neatoo.RemoteFactory.AspNetCore` integration is added, THEN no `EventTrackerHostedService` is registered (its registration call is removed from `ServiceCollectionExtensions`) — NEW

### Logging

13. WHEN log events are reviewed post-change, THEN event IDs 9001-9009 (the `EventTracker`/event-scope category) are removed from `Internal/Log.cs`. Other event IDs are unaffected — NEW

### Tests

14. WHEN the test suite is run after deletion, THEN no test method references `EventAttribute`, `IEventTracker`, `IEventScopeInitializer`, or any of the deleted types — NEW

15. WHEN the test suite is run after deletion, THEN `IEventTestService` (the test helper currently defined in `EventTargets.cs`) is preserved — it is shared by `FactoryEventHandlerTargets.cs`. Move it to a new file (`IEventTestService.cs` + `EventTestService.cs`) before deleting `EventTargets.cs` — NEW

16. WHEN `dotnet test src/Neatoo.RemoteFactory.sln -c Release` runs after the deletion, THEN every remaining test passes on net9.0 AND net10.0. Skipped count is the prior 3 (`ShowcasePerformanceTests`); no new skips — NEW

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | EventAttribute deleted | grep for `class EventAttribute` in `src/RemoteFactory/` | 1 | No match |
| 2 | `EventAttribute` type absent from compiled library | Decompile / reflection-inspect `Neatoo.RemoteFactory.dll` for an `EventAttribute` type in namespace `Neatoo.RemoteFactory` | 2 | Type not present (consumers using `[Event]` get C# CS0246 at their build, not a RemoteFactory diagnostic — this scenario verifies the absence, not the C# error) |
| 3 | NF0401-NF0404 descriptors gone | Inspect `DiagnosticDescriptors.cs` and `GetDescriptor` switch | 3 | No matches for NF0401-NF0404 |
| 4 | Non-event factory generation continues to compile and pass existing tests | All existing non-event factory tests in `RemoteFactory.UnitTests`, `RemoteFactory.IntegrationTests`, and `Design.Tests` | 4 | All pass (covered by rule 16 — no separate snapshot harness exists; this scenario subsumes into rule 16 verification) |
| 5 | Mixed-attribute class — events silently gone | Class with one `[Create]` method and one method that USED to have `[Event]` (now bare) | 5 | `[Create]` factory generated; the bare method is just an ordinary internal method, no factory entry generated for it |
| 6 | Server-side `[FactoryEventHandler<T>]` unchanged | Existing tests in `FactoryEventHandlerSerializationTests`, `FactoryEventHandlerCoexistenceTests` (rename / scope-narrow if needed) | 6 | All pass without modification |
| 7 | Client-side relay unchanged | Existing v1.4 `RelayTimingTests`, `FactoryEventRelayTests` | 7 | All pass without modification |
| 8 | Server mode no IEventTracker | `var sp = ...AddNeatooRemoteFactory(NeatooFactory.Server)...; sp.GetService<IEventTracker>()` would not compile (type gone) | 8 | Type `IEventTracker` does not exist |
| 9 | Logical mode no IEventTracker | (Same — type gone) | 9 | Type `IEventTracker` does not exist |
| 10 | IEventScopeInitializer registration line removed | grep `AddRemoteFactoryServices.cs` for `IEventScopeInitializer` | 10 | No matches |
| 11 | ICorrelationContext still scoped-registered | `var sp = ...AddNeatooRemoteFactory(...)...; sp.GetRequiredService<ICorrelationContext>()` resolves | 11 | Returns `CorrelationContextImpl` |
| 12 | EventTrackerHostedService registration removed | grep `RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs` | 12 | No `EventTrackerHostedService` reference |
| 13 | Log events 9001-9009 removed | grep `Log.cs` for `EventId = 90` | 13 | No matches in 9001-9009 range |
| 14 | No test references deleted types | grep all test files for `EventAttribute`, `IEventTracker`, `IEventScopeInitializer`, `EventTrackerTests`, etc. | 14 | No matches |
| 15 | IEventTestService preserved + relocated | New file `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/EventTestService.cs` exists with `IEventTestService` and `EventTestService`; `FactoryEventHandlerTargets.cs` still resolves the type | 15 | Compiles; existing FactoryEventHandler tests pass |
| 16 | Full suite green Release / both TFMs | `dotnet test src/Neatoo.RemoteFactory.sln -c Release` | 16 | All pass; 3 skipped (perf-demo only) |

---

## Approach

**Phase 1 — Delete generator paths.** Strip every code path that scans for or processes `[Event]`. This avoids generator runs producing partial event-related code that fails to compile.

**Phase 2 — Delete library surface.** Remove `EventAttribute`, `FactoryOperation.Event`, `AuthorizeFactoryOperation.Event`, `IEventTracker`, `EventTracker`, `IEventScopeInitializer`, `DelegateEventScopeInitializer`, `CorrelationContextScopeInitializer`, `EventTrackerHostedService`, `AddRemoteFactoryEventScopeInitializer`. Remove DI registrations. Remove `EventTracker`-related log events (9001-9009).

**Phase 3 — Delete tests + relocate `IEventTestService`.** Move the shared test helper before deleting `EventTargets.cs` files. Delete `EventTrackerTests`, `EventTrackerRegistrationTests`, `EventScopeInitializerTests`, `EventGenerationTests`, `CorrelationEventPropagationTests`, `RemoteEventIntegrationTests`, `FactoryEventHandlerCoexistenceTests` (the coexistence becomes moot). Delete `[Event]` blocks from `CombinationTestGenerator`.

**Phase 4 — Delete reference-app + design-project samples.** Delete `src/docs/reference-app/.../Samples/Events/` `[Event]`-based content (keep `[FactoryEventHandler]` content). Delete any `Design.Domain/Services/CorrelationExample.cs` content tied to `[Event]`.

**Phase 5 — Build + test.** Confirm net9.0 + net10.0 Release pass.

**Phase 6 — Bump version.** `src/Directory.Build.props`: 1.4.0 → 1.5.0.

Documentation (Step 7, after verification):
- Update `src/Design/CLAUDE-DESIGN.md` (remove Pattern 5 if present, remove `[Event]` mentions in Decision Tables, remove the NF04xx diagnostic rows).
- Delete `docs/events.md` if it primarily covers `[Event]` (or rewrite to point at `[FactoryEventHandler<T>]`).
- Update `docs/factory-events.md`, `docs/attributes-reference.md`, `docs/interfaces-reference.md`, `docs/trimming.md` to remove `[Event]` cross-references.
- Update `skills/RemoteFactory/references/factory-events.md` (rewrite the comparison table — `[Event]` row goes away; the file's comparison framing changes since there's no longer a "two competing pipelines" choice).
- Add `docs/release-notes/v1.5.0.md` with breaking-change section + migration guide pointing consumers at the manual `Task.Run + IServiceScopeFactory.CreateScope()` pattern.

---

## Domain Model Behavioral Design

N/A — this is framework deletion work, not domain model work.

---

## Design

### Files Deleted (production)

- `src/RemoteFactory/IEventTracker.cs`
- `src/RemoteFactory/Internal/EventTracker.cs`
- `src/RemoteFactory/IEventScopeInitializer.cs`
- `src/RemoteFactory/Internal/CorrelationContextScopeInitializer.cs`
- `src/RemoteFactory/Internal/DelegateEventScopeInitializer.cs`
- `src/RemoteFactory.AspNetCore/EventTrackerHostedService.cs`
- `src/Generator/Model/EventMethodModel.cs`

### Files Deleted (tests)

- `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerTests.cs`
- `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerRegistrationTests.cs`
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Events/EventGenerationTests.cs`
- `src/Tests/RemoteFactory.UnitTests/TestTargets/Events/EventTargets.cs`
- `src/Tests/RemoteFactory.IntegrationTests/Events/EventScopeInitializerTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/Events/CorrelationEventPropagationTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/Events/RemoteEventIntegrationTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/Events/FactoryEventHandler/FactoryEventHandlerCoexistenceTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/EventTargets.cs` (after extracting `IEventTestService`)

### Files Created

- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/EventTestService.cs` — split out `IEventTestService` + `EventTestService` from the deleted `EventTargets.cs` so `FactoryEventHandlerTargets.cs` still resolves the dependency

### Kept-File Comment Edits

- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/FactoryEventHandlerTargets.cs:190` — remove the stale `IEventTracker` reference from the "Slow handler for TestOrderEvent — used to test IEventTracker and await vs fire-and-forget" comment. One-line comment edit; rest of file unchanged.

### Files Modified — public surface

- `src/RemoteFactory/FactoryAttributes.cs` — delete `EventAttribute` class
- `src/RemoteFactory/FactoryOperation.cs` — delete `Event` enum value
- `src/RemoteFactory/AuthorizeFactoryOperation.cs` — delete `Event = 512` flag
- `src/RemoteFactory/AddRemoteFactoryServices.cs` — delete `IEventTracker` registration block, delete `IEventScopeInitializer` registration, delete `AddRemoteFactoryEventScopeInitializer(...)` extension method
- `src/RemoteFactory/IFactoryEvents.cs` — remove `[Event]` cross-reference in XML doc
- `src/RemoteFactory/RaiseOptions.cs` — remove `[Event]` cross-reference in XML doc
- `src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs` — remove `EventTrackerHostedService` registration

### Files Modified — generator

- `src/Generator/DiagnosticDescriptors.cs` — delete NF0401-NF0404 descriptors
- `src/Generator/FactoryGenerator.cs` — delete NF0401-NF0404 cases from `GetDescriptor` switch
- `src/Generator/Builder/FactoryModelBuilder.cs` — delete `[Event]` matching paths (lines around 65 and 173)
- `src/Generator/Model/StaticFactoryModel.cs`, `src/Generator/Model/ClassFactoryModel.cs` — remove the `Events` collection property (NOT `EventMethods` — actual name is `Events` singular+plural).
- `src/Generator/Model/FactoryGenerationUnit.cs` — remove the XML-doc reference to `Events`.
- `src/Generator/Renderer/StaticFactoryRenderer.cs` — delete `[Event]` rendering branches
- `src/Generator/Renderer/ClassFactoryRenderer.cs` — delete any event-method emission
- `src/Tests/CombinationTestGenerator/CombinationGenerator.cs` — delete `[Event]` generation block (~line 657)
- `src/Tests/CombinationTestGenerator/Generation/TargetClassGenerator.cs` — delete `[Event]` generation block (~line 353)

### Files Modified — internal

- `src/RemoteFactory/Internal/Log.cs` — delete `EventTracker` log events 9001-9009

### Files Modified — design projects (expanded after Step 2 reviewer findings)

- `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` —
  - **Edit** lines 282-297 (shared Pattern 3 banner documenting both `ExampleCommands` KEPT and `ExampleEvents`) to remove `[Event]` and `Event` references.
  - **Keep** lines 299-381 (`ExampleCommands` static factory — untouched).
  - **Delete** lines 383-443 (the `ExampleEvents` static factory).
  - **Keep** line 445 onward (SUPPORTING TYPES section).
- `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` — remove `[Event]` cross-references in comments (lines ~34-37, 81-83).
- `src/Design/Design.Domain/Services/CorrelationExample.cs` — **delete the file outright**. The entire `CorrelatedOperations` class demonstrates correlation propagation across `[Event]` isolated scopes, which no longer exists.
- `src/Design/Design.Client.Blazor/Pages/Home.razor` — delete the full tightly-coupled UI set (deleting only the `@inject` lines would leave dangling references and break Razor compile):
  - lines 90-91 (comment + paragraph describing `[Event] = fire-and-forget`)
  - **line 98** (`<button @onclick="FireEvent">Fire Event: Order Placed</button>` — refs deleted method)
  - **lines 106-109** (`@if (eventFired) { <p>Event fired successfully!</p> }` — refs deleted field)
  - **line 124** (`private bool eventFired;` — field used by 106-109 and 234)
  - line 137 (comment)
  - **lines 143-144** (`[Inject] private ExampleEvents.OnOrderPlacedEvent OnOrderPlaced { get; set; }`)
  - line 211 (comment "Pattern 3: Static Factory - Commands and **Events**")
  - **lines 228-241** (`private async Task FireEvent() { ... await OnOrderPlaced(999); ... }` — invokes deleted delegate)
- `src/Design/Design.Tests/FactoryTests/StaticFactoryTests.cs` — delete the two `[Event]`-dependent test methods (`Event_OnOrderPlaced_FiresWithoutBlocking` at ~line 100, `Event_WorksInLocalMode` at ~line 122). Both resolve `ExampleEvents.OnOrderPlacedEvent` which no longer exists.

### Files Modified — reference app (expanded after Step 2 reviewer findings)

Grep across `src/docs/reference-app/` for `[Event]`, `IEventTracker`, `IEventScopeInitializer`, `EventTrackerHostedService`, `AddRemoteFactoryEventScopeInitializer`. Reviewer found ~26 files. The implementation handling is:

**Delete entire file** (file exists solely to demonstrate `[Event]` or related infra):
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/AuditSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/BasicEventSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/CorrelationSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/DomainEventSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/GeneratedCodeSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/EventSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Application/Samples/Events/EventCallerSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Application/Samples/Events/EventTrackerSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Application/Samples/Interfaces/EventTrackerSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Application/Samples/Operations/EventOperationSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Events/AspNetCoreIntegrationSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Events/GracefulShutdownSamples.cs`

**Edit — remove only `[Event]`-touched content, preserve the rest**:
- `src/docs/reference-app/EmployeeManagement.Domain/Events/DepartmentEventHandlers.cs` — remove `[Event]` methods; keep `[FactoryEventHandler<T>]` handlers.
- `src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs` — same.
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs` — remove `[Event]` blocks.
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/MinimalAttributesSamples.cs` — same.
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs` — same.
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs` — same (likely has `AuthorizeFactoryOperation.Event` reference).
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs` — remove `IEventTracker` / `IEventScopeInitializer` entries.
- `src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/PatternSamples.cs` — remove `[Event]` pattern demos.
- `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs` — remove any `Event` flag usage.
- `src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs` — remove `[Event]`-based test helper samples.

**Csproj review + comment edits**:
- `src/docs/reference-app/EmployeeManagement.Domain/EmployeeManagement.Domain.csproj` line 11 — edit the `<!-- CA1822: Event methods must be instance methods for [Event] attribute -->` comment (remove `[Event]` reference; keep `CA1822` in `<NoWarn>`).
- `src/docs/reference-app/EmployeeManagement.Application/EmployeeManagement.Application.csproj` line 15 — same comment edit.
- Both csproj files — check MarkdownSnippets `<Content>` / `<EmbeddedResource>` region lists; remove references to deleted files.

**Regenerate MarkdownSnippets** after deletions: run `mdsnippets` from repository root so skill/doc files no longer embed from deleted source regions.

### Files Modified — version

- `src/Directory.Build.props` — `<FileVersion>` and `<PackageVersion>`: 1.4.0 → 1.5.0

---

## Implementation Steps

1. **Generator first** — Delete `[Event]` paths in `FactoryModelBuilder`, `StaticFactoryRenderer`, `ClassFactoryRenderer`, `EventMethodModel`. Remove the `Events` collection property from `StaticFactoryModel` and `ClassFactoryModel`. Remove the XML-doc reference in `FactoryGenerationUnit`. Build the generator project alone to confirm it compiles.
2. **Delete diagnostics NF0401-NF0404** in `DiagnosticDescriptors.cs` + the `GetDescriptor` switch in `FactoryGenerator.cs`.
3. **Delete library types**: `IEventTracker`, `EventTracker`, `IEventScopeInitializer`, `DelegateEventScopeInitializer`, `CorrelationContextScopeInitializer`, `EventTrackerHostedService`. Delete `AddRemoteFactoryEventScopeInitializer`.
4. **Delete `EventAttribute`** from `FactoryAttributes.cs`. Delete `FactoryOperation.Event` and `AuthorizeFactoryOperation.Event`.
5. **Update `AddRemoteFactoryServices.cs`** — remove the `if (remoteLocal != NeatooFactory.Remote) { TryAddSingleton<IEventTracker, ...>; AddTransient<IEventScopeInitializer, ...>; }` block. Update the `EventTrackerHostedService` registration removal in `RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs`.
6. **Delete log events 9001-9009** from `Internal/Log.cs`.
7. **Update XML doc cross-references** in `IFactoryEvents.cs` and `RaiseOptions.cs` (remove `[Event]` mentions).
8. **Relocate `IEventTestService` + `EventTestService`** out of `EventTargets.cs` into `EventTestService.cs` (integration tests). Delete the integration `EventTargets.cs`.
9. **Delete unit-test `EventTargets.cs`** + `EventGenerationTests.cs` + `EventTrackerTests.cs` + `EventTrackerRegistrationTests.cs`.
10. **Delete integration tests**: `EventScopeInitializerTests.cs`, `CorrelationEventPropagationTests.cs`, `RemoteEventIntegrationTests.cs`, `FactoryEventHandlerCoexistenceTests.cs`.
11. **Strip `[Event]` blocks from `CombinationTestGenerator`** — delete the generation branches that emit `[Event]` test targets so the CombinationGenerator doesn't produce code that fails to compile.
12. **Update Design + reference-app samples** — delete `[Event]`-based files, update `AllPatterns.cs` / `Home.razor` / `CorrelationExample.cs`.
13. **Bump version** `1.4.0 → 1.5.0` in `src/Directory.Build.props`.
14. **Build + test cross-framework Release**: `dotnet build src/Neatoo.RemoteFactory.sln -c Release` + `dotnet test ...`. Confirm Design + Trimming projects also pass.

Documentation (Step 7, not implementation):
- Update CLAUDE-DESIGN.md — remove/rewrite line ranges identified by reviewer: 40 (Static Factory `[Event]` mention), 197-199 (Fire-and-forget guidance), 256 (Quick Decisions), 267 (Common Mistakes), 273-274 (scope-initializer docs), 659-682 (Event Scope Initialization subsection), 955, 961, 1045 (checklist entries).
- Update `docs/factory-events.md`, `docs/attributes-reference.md`, `docs/interfaces-reference.md`, `docs/trimming.md`, `docs/aspnetcore-integration.md`, `docs/factory-operations.md` — remove `[Event]`, `IEventTracker`, `IEventScopeInitializer`, `EventTrackerHostedService` references.
- **Delete `docs/events.md`** — its entire content is `[Event]` + `IEventTracker` coverage.
- **Preserve historical release notes** — `docs/release-notes/v0.24.2.md`, `v0.28.0.md`, `v1.1.0.md`, `v0.6.0.md` mention the deleted types; do NOT edit them (history is archival).
- Update `skills/RemoteFactory/SKILL.md`, `references/factory-events.md`, `references/static-factory.md` — remove the `[Event]` row / sections.
- Add `docs/release-notes/v1.5.0.md` with breaking-change section + migration guide covering the three reviewer-flagged edge cases:
  - **Graceful shutdown**: consumers must wire `IHostApplicationLifetime.ApplicationStopping` into their own cancellation token and track outstanding fire-and-forget tasks themselves (pattern + snippet).
  - **Correlation ID propagation**: manual snapshot-and-copy of `ICorrelationContext.CorrelationId` from parent scope into child scope before resolving services.
  - **Tenant / ambient context**: whatever the consumer had in their `IEventScopeInitializer` becomes explicit copy-code inside the `Task.Run` body.

---

## Acceptance Criteria

- [ ] All 16 test scenarios are satisfied by passing tests / verified absences.
- [ ] `dotnet build src/Neatoo.RemoteFactory.sln -c Release` succeeds on net9.0 and net10.0.
- [ ] `dotnet test src/Neatoo.RemoteFactory.sln -c Release` shows 0 failures (3 skipped pre-existing perf-demo).
- [ ] `dotnet test src/Design/Design.sln -c Release` shows 0 failures.
- [ ] No production source contains `EventAttribute`, `IEventTracker`, `IEventScopeInitializer`, `EventTrackerHostedService`, `AddRemoteFactoryEventScopeInitializer`, or NF0401-NF0404 references.
- [ ] `IEventTestService` test helper preserved and `FactoryEventHandlerTargets.cs` still compiles.
- [ ] `ICorrelationContext` is still registered scoped in all modes.
- [ ] Server-side static-method `[FactoryEventHandler<T>]` behavior unchanged; existing tests pass without modification.
- [ ] Client-side `IFactoryEventRelay` behavior unchanged; v1.4.0 timing tests pass without modification.
- [ ] `src/Directory.Build.props` reads `1.5.0`.
- [ ] PublishTrimmed smoke binary still runs and prints the v1.4.0 success line (relay path unaffected).

---

## Dependencies

- v1.4.0 (factory-events-relay-redesign) merged at commit `2671a9d` — this plan builds on that surface and assumes the relay rework is in place.
- The `IFactoryEventRelay` / `FactoryEventBase` / `FactoryEventTypeRegistry` machinery from v1.4.0 is **kept** untouched.

---

## Risks / Considerations

1. **Consumer migration burden**. Any consumer using `[Event]` for fire-and-forget work must migrate to `Task.Run` + `IServiceScopeFactory.CreateScope()` + manual context propagation. The release notes' migration guide must spell this out.
2. **`CorrelationContextScopeInitializer` removal**. This was the only consumer of `IEventScopeInitializer` for correlation. Removing the scope initializer means correlation IDs no longer auto-propagate to fire-and-forget event scopes — but since `[Event]` itself is gone, there are no such scopes to propagate to. `ICorrelationContext` registration stays for the v1.4 relay path.
3. **`EventGenerationTests`, `EventTrackerTests`, `CorrelationEventPropagationTests`** are sacred-test deletions. CLAUDE.md says existing tests are sacred — but tests that cover an explicitly-deleted feature are not in scope for preservation. The plan's Rule 14 makes this explicit.
4. **`FactoryEventHandlerCoexistenceTests`**. Tests the coexistence of `[Event]` and `[FactoryEventHandler<T>]`. With one of the two gone, the test scope evaporates. Delete the file.
5. **Reference-app build**. `src/docs/reference-app/` is a separate solution; it has heavy `[Event]` sample coverage. Reference-app build must pass after the deletions — the architect should run `dotnet build src/docs/reference-app/EmployeeManagement.sln` as part of verification.
6. **MarkdownSnippets regions**. Some skill files embed code from the reference-app via mdsnippets. After deleting reference-app `[Event]` samples, the skill files' embedded blocks may go stale. Step 7 must regenerate skill content (or delete the affected blocks).
7. **CombinationTestGenerator regeneration**. The combination test generator emits factory-test targets including `[Event]`. After the generator change, the produced test files must be regenerated (or the combination tests already-checked-in must be reviewed and stripped).
8. **`AuthorizeFactoryOperation.Event = 512` flag**. It's a public enum value; removing it is a breaking enum-value-removal. No internal authorization code path uses it now (verified).
9. **Test count regression**. Removing tests will reduce the test count. CI runs that compare test counts (if any) will need their baseline updated.
10. **CI behavior between intermediate commits**. The GitHub Actions `build.yml` runs on every push. Implementation touches the codebase across 14 logical phases; if each phase is pushed separately, most intermediate commits will fail CI (e.g. after deleting `EventAttribute` but before deleting generator paths). This is acceptable for a feature branch and cleans up once Step 14 is green. Consider pushing only at clean checkpoints, or squashing before opening the PR. Only the final post-step-14 commit needs to be green for the PR to merge.
