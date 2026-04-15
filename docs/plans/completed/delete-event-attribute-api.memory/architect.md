# Architect — Delete `[Event]` Method Attribute API

Last updated: 2026-04-14
Current step: Step 3 Architect Validation — COMPLETE

## Verdict

**Concerns** — Approve once 5 concrete fixes below are folded into the plan. None are fundamental design problems; all are precision/scope issues that will cause compile failures or accidental over-deletion if not addressed before Step 4.

The plan's overall strategy (generator-first deletion order, `ICorrelationContext` retained, NF0401-NF0404 removed, `IEventTestService` relocated) is sound. The reviewer's seven gap flags were closed correctly. The remaining concerns are surface details — line ranges that overshoot, file references using the wrong property name, missing UI lines that would break Home.razor at compile time, and one residual comment.

## Files Examined

Production:
- `src/RemoteFactory/AddRemoteFactoryServices.cs` (DI registration block)
- `src/RemoteFactory/AuthorizeFactoryOperation.cs` (`Event = 512`)
- `src/RemoteFactory/FactoryAttributes.cs` (`EventAttribute` declaration)
- `src/RemoteFactory/FactoryOperation.cs` (`Event` enum value)
- `src/RemoteFactory/FactoryEventAttribute.cs` (KEPT — relay infrastructure)
- `src/RemoteFactory/IFactoryEvents.cs` (XML doc cross-ref to `[Event]`)
- `src/RemoteFactory/RaiseOptions.cs` (XML doc cross-ref + `<see cref="IEventTracker"/>`)
- `src/RemoteFactory/Internal/Log.cs` (event IDs 9001-9009 confirmed)
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs` (`ForDelegateEvent` — KEPT, used by relay path)
- `src/RemoteFactory/Internal/FactoryEventTypeRegistry.cs` (KEPT — `FactoryEventAttribute` ≠ `EventAttribute`)
- `src/RemoteFactory/RemoteFactoryEvents.cs` (KEPT — uses `ForDelegateEvent` for v1.4 relay)
- `src/RemoteFactory/ILLink.Descriptors.xml` (no event entries — clean)
- `src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs` (`EventTrackerHostedService` registration)

Generator:
- `src/Generator/DiagnosticDescriptors.cs` (NF0401-NF0405; only 0401-0404 removed, 0405 kept)
- `src/Generator/FactoryGenerator.cs` (`GetDescriptor` switch lines 127-130)
- `src/Generator/Builder/FactoryModelBuilder.cs` (lines 40-67, 166-175, 452-476)
- `src/Generator/Renderer/StaticFactoryRenderer.cs` (`RenderEventDelegate`, `RenderLocalEventRegistration`, `RenderRemoteEventRegistration`, `BuildEventMethodInvocationParams`)
- `src/Generator/Renderer/ClassFactoryRenderer.cs` (lines 71, 90, 1604-1700, 1805 — significant deletions)
- `src/Generator/Model/StaticFactoryModel.cs` (property is `Events`, NOT `EventMethods`)
- `src/Generator/Model/ClassFactoryModel.cs` (property is `Events`)
- `src/Generator/Model/FactoryGenerationUnit.cs` (XML-doc reference only)
- `src/Generator/Model/EventMethodModel.cs`

Tests:
- `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerTests.cs` (8 IEventTracker hits — confirmed delete-target)
- `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerRegistrationTests.cs`
- `src/Tests/RemoteFactory.UnitTests/Internal/FactoryEventBaseAttributeTests.cs` (KEPT — about `FactoryEventAttribute`)
- `src/Tests/RemoteFactory.IntegrationTests/Events/EventScopeInitializerTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/Events/CorrelationEventPropagationTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/Events/RemoteEventIntegrationTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/Events/FactoryEventHandler/FactoryEventHandlerCoexistenceTests.cs` (resolves `OrderEventHandler.NotifyWarehouseEvent` from EventTargets — confirmed must delete)
- `src/Tests/RemoteFactory.IntegrationTests/Events/FactoryEventHandler/FactoryEventHandlerCorrelationTests.cs` (KEPT — no actual `[Event]`/`IEventTracker` references)
- All other `FactoryEventHandler/*.cs` (KEPT — clean, only substring matches)
- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/EventTargets.cs`
- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/FactoryEventHandlerTargets.cs` (KEPT — but has stale comment at line 190)
- `src/Tests/RemoteFactory.UnitTests/TestTargets/Events/EventTargets.cs`
- `src/Tests/RemoteFactory.TrimmingTests/` (no event-related code — clean)
- `src/Tests/RemoteFactory.AspNetCore.TestServer`/`TestLibrary` (no event-related code)
- `src/Tests/CombinationTestGenerator/CombinationGenerator.cs` + `Generation/TargetClassGenerator.cs`

Design:
- `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` (lines 282-443 examined)
- `src/Design/Design.Tests/FactoryTests/StaticFactoryTests.cs` (lines 79-136)
- `src/Design/Design.Client.Blazor/Pages/Home.razor` (lines 85-242)

Reference app csproj files examined; reviewer's enumeration matches grep.

Examples (`OrderEntry`, `Person`): no event-related code — clean.

## Validation of Business Rules

**Generally tight.** One issue:

- **Rule 4** ("byte-for-byte identical to pre-change output"). This is testable only via snapshot comparison; the project has no documented snapshot harness for non-event factory output. The plan should restate as: "...code generation continues to compile and pass existing non-event factory tests." Otherwise scenario 4 has no executable verification mechanism. (Reviewer raised the same concern for scenario 2; this is the analogous gap on rule 4.)

All other rules (1-3, 5-16) are well-formed and verifiable.

## Validation of Test Scenarios

- **Scenario 4** — same problem as rule 4: "compare generator output with main pre-change snapshot" but there's no snapshot test mechanism in this project. Restate as "all existing non-event factory tests in `RemoteFactory.UnitTests` and `Design.Tests` continue to pass" — that's already covered by rule 16.

All other scenarios are concrete and verifiable.

## Files Plan Misses or Mis-References

### Concern 1 (high severity): `Design.Client.Blazor/Pages/Home.razor` enumeration is incomplete — will fail to compile

Plan lists lines 91, 137, 144, 232-233. Actual lines requiring deletion to keep the file compiling:

- Line 90-91 (the `[Event] = fire-and-forget` comment text) — minor, can stay if rewritten
- **Line 98** (`<button class="btn btn-secondary" @onclick="FireEvent">Fire Event: Order Placed</button>`) — references the `FireEvent` method
- **Lines 106-109** (`@if (eventFired) { <p>Event fired successfully!</p> }`) — UI block bound to `eventFired`
- Line 137 (comment) — cosmetic
- **Line 124** (`private bool eventFired;`) — field used by lines 106-109 and 234
- **Lines 143-144** (`[Inject] private ExampleEvents.OnOrderPlacedEvent OnOrderPlaced { get; set; }`)
- Line 211 (comment "Pattern 3: Static Factory - Commands and **Events**") — cosmetic
- **Lines 228-241** (`private async Task FireEvent() { ... await OnOrderPlaced(999); ... }`) — invokes deleted delegate

If only the plan's enumerated lines are deleted, the @onclick handler at line 98, the UI conditional at 106-109, the field at 124, and the FireEvent method body all reference deleted/missing members → Razor compile error. **Fix**: list all the bolded items above explicitly.

### Concern 2 (high severity): `AllPatterns.cs` line range "283-445" overshoots and would delete `ExampleCommands` (kept)

The plan's "delete the `ExampleEvents` static factory (lines ~283-445)" overshoots:

- Lines 282-297 are the **shared Pattern 3 section banner** that documents BOTH `ExampleCommands` ([Execute]) AND `ExampleEvents` ([Event]). Banner needs **edit** (remove `[Event]`/`Event` references), not delete.
- Lines 299-381 are `ExampleCommands` — **must keep** (Execute pattern, untouched by this plan).
- Lines 383-443 are `ExampleEvents` (the delete-target).
- Line 445 onward is the "SUPPORTING TYPES" section header — keep.

Plan should be restated: "Edit the Pattern 3 banner (lines 282-297) to remove `[Event]` references; delete `ExampleEvents` only (lines 383-443)."

### Concern 3 (medium severity): Property name mismatch in plan

Plan says: "remove `EventMethods` collection / `EventMethodModel` references" in `StaticFactoryModel.cs` and `FactoryGenerationUnit.cs`. The actual property in both `StaticFactoryModel` and `ClassFactoryModel` is named `Events` (singular `Event`+plural). `FactoryGenerationUnit` has only an XML-doc mention. Plan should say `Events` to avoid grep-replace mistakes.

### Concern 4 (low severity): Stale comment in kept file

`src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/FactoryEventHandlerTargets.cs:190` has the comment "Slow handler for TestOrderEvent — used to test IEventTracker and await vs fire-and-forget." `IEventTracker` reference is dead post-deletion. Add to plan's modify list (1-line comment edit).

### Concern 5 (low severity): csproj `<NoWarn>` comments mention `[Event]`

`src/docs/reference-app/EmployeeManagement.Domain/EmployeeManagement.Domain.csproj` line 11 (`<!-- CA1822: Event methods must be instance methods for [Event] attribute -->`) and `EmployeeManagement.Application.csproj` line 15 (same comment). The `<NoWarn>` rules themselves are still useful (CA1822 may apply elsewhere) but the comments are misleading. Add to plan's modify list (comment edits only — do not remove `CA1822` from `<NoWarn>` without separate analysis).

### Things the plan does NOT miss (sanity confirmation)

- `EventTrackerHostedService` removal is clean — no consumer-facing extension method depends on it; only internal-to-`AddNeatooAspNetCore`.
- `IMakeRemoteDelegateRequest.ForDelegateEvent` — KEEP (used by `RemoteFactoryEvents.Raise` for v1.4 relay, NOT just by `[Event]` generated code). Plan correctly does not list it for deletion.
- `FactoryEventAttribute` (singular, on `FactoryEventBase`) — KEEP. Different from the `EventAttribute` being deleted; supports the relay deserialization path. Plan does not list it for deletion ✓.
- `FactoryEventBaseAttributeTests.cs` — KEEP (about `FactoryEventAttribute`, not `[Event]`).
- All `FactoryEventHandler/*.cs` tests outside `Coexistence` — KEEP (substring-only matches in grep, no real dependencies on deleted types).
- `RemoteFactory.TrimmingTests`, `AspNetCore.TestServer`, `AspNetCore.TestLibrary` — clean of any deleted-type references.
- `Examples/Person`, `Examples/OrderEntry` — clean.
- `ILLink.Descriptors.xml` — no event entries to remove.
- Generator's `NF0405` (FactoryEventHandlerMustBeStatic) — KEEP (about `[FactoryEventHandler]`, not `[Event]`).

## Implementation Step Ordering Check

Steps 1-14 are correctly ordered. Generator first (steps 1-2) avoids cryptic error situations where `[Event]` attribute is gone but generator still scans for it. Library types (step 3) before attribute deletion (step 4) means the `EventAttribute(FactoryOperation.Event)` constructor still resolves while `EventTracker` etc. exist as orphans for the brief window between commits — that's fine because the build is checked at step 14, not between intermediate steps.

One observation: **CI behavior between commits.** The plan deletes things across 14 logical phases. If implementation pushes intermediate commits to a branch with CI enabled (`build.yml` runs on every push), CI will fail on most intermediate commits. This is acceptable for a feature branch but should be called out in Risks. **Recommendation**: add to Risks: "CI runs per push. Intermediate commits will fail builds; only the final commit (after step 14) needs to be green. Consider squashing or pushing only at clean checkpoints."

## Architectural Verification (Pre-Handoff Scope Table)

| Surface | Plan claims removed | Verified absent in code post-removal? | Notes |
|---|---|---|---|
| `EventAttribute` class | Yes | Yes — sole definition in `FactoryAttributes.cs:108-111` | clean |
| `FactoryOperation.Event` | Yes | Yes — sole definition in `FactoryOperation.cs:17` | clean |
| `AuthorizeFactoryOperation.Event` | Yes | Yes — sole definition in `AuthorizeFactoryOperation.cs:18` | no consumer hits |
| `IEventTracker` / `EventTracker` | Yes | Yes — single definition each | only test consumers (deleted) and DI block (deleted) |
| `IEventScopeInitializer` / `DelegateEventScopeInitializer` / `CorrelationContextScopeInitializer` | Yes | Yes | only consumers are `EventScopeInitializerTests` (deleted) and DI block (deleted) |
| `EventTrackerHostedService` | Yes | Yes | only consumer is `AddNeatooAspNetCore` (edited) |
| `AddRemoteFactoryEventScopeInitializer` | Yes | Yes — sole definition in `AddRemoteFactoryServices.cs:259-266` | only test consumers (deleted) |
| `EventMethodModel` + `Events` properties | Yes | Yes — properties named `Events` (not `EventMethods`) | concern 3 |
| NF0401-NF0404 | Yes | Yes — `DiagnosticDescriptors.cs` 187-232; `GetDescriptor` switch 127-130 | NF0405 stays |
| Log events 9001-9009 | Yes | Yes — `Log.cs` 475-541 | clean |
| `FactoryEventAttribute` | NO (kept) | KEPT — used by `FactoryEventTypeRegistry` | plan correct |
| `IMakeRemoteDelegateRequest.ForDelegateEvent` | NO (kept) | KEPT — used by `RemoteFactoryEvents.Raise` | plan correct |
| `ICorrelationContext` | NO (kept) | KEPT — registered scoped in DI | plan correct |

## Acceptance Criteria Verification

All 11 ACs are concrete and verifiable. No changes recommended.

## Mistakes to Avoid (Carry Forward)

- Do not assume the generator model property is `EventMethods`; it's `Events`.
- Do not delete `ExampleCommands` when removing `ExampleEvents` from `AllPatterns.cs`.
- Do not delete only the `[Inject]` line in Home.razor without also deleting the `FireEvent` method body, the `eventFired` field, the conditional UI block, and the button binding — they form a tightly coupled set.
- Do not confuse `FactoryEventAttribute` (singular, on the event type, KEPT) with `EventAttribute` (the method attribute, DELETED).
- Do not delete `IMakeRemoteDelegateRequest.ForDelegateEvent` — it's used by the v1.4 relay path even after `[Event]` generated code is gone.

## User Corrections

None.

## Recommendations for Orchestrator (to fold into plan)

1. Update Files Modified — Home.razor: enumerate the full set (lines 90-91, 98, 106-109, 124, 137, 143-144, 211, 228-241).
2. Update Files Modified — AllPatterns.cs: change "lines ~283-445" to "edit Pattern 3 banner lines 282-297 to remove `[Event]` references; delete `ExampleEvents` lines 383-443".
3. Update Files Modified — generator models: replace `EventMethods` with `Events` everywhere in the plan text.
4. Add Files Modified entry — `FactoryEventHandlerTargets.cs:190` comment edit.
5. Add Files Modified entry — both reference-app csproj files: edit comments in `<NoWarn>` block (lines 11 and 15 respectively) without removing `CA1822` from the actual `<NoWarn>` list.
6. Add to Risks — CI runs per-push; intermediate commits will fail; final post-step-14 commit must be green.
7. Restate Rules 4 + Scenario 4 — drop "byte-for-byte snapshot" framing (no harness exists); rely on rule 16 (full suite green).

After these changes, the plan can be approved for Step 4.

---

## Re-verify (2026-04-14)

**Verdict: Approved.**

All 7 prior concerns verified closed by reading the updated plan:

1. **Home.razor enumeration** (plan lines 197-206) — full set listed: 90-91, 98, 106-109, 124, 137, 143-144, 211, 228-241. Verified.
2. **AllPatterns.cs scope** (plan lines 190-194) — banner edit 282-297, keep ExampleCommands 299-381, delete ExampleEvents 383-443, keep 445+. Verified.
3. **Property name `Events`** (plan lines 177-178) — explicitly says "NOT `EventMethods` — actual name is `Events`"; FactoryGenerationUnit noted as XML-doc-only. (Minor inconsistency: implementation step 1 line 255 still has stale `StaticFactoryModel.EventMethods` and `FactoryGenerationUnit.EventMethods` text. Authoritative Files Modified table is correct, so developer will reference the right name. Not blocking.)
4. **Stale comment in kept file** — new "Kept-File Comment Edits" heading (plan lines 158-160) covers `FactoryEventHandlerTargets.cs:190`. Verified.
5. **Csproj comment edits** (plan lines 240-242) — both csproj line 11 / line 15 edits listed; explicitly preserves CA1822 in NoWarn. Verified.
6. **Risk #10 — CI on intermediate commits** (plan line 317) — added with full explanation. Verified.
7. **Rule 4 + Scenario 4** (plan lines 44, 85) — both restated to rely on rule 16 / "no separate snapshot harness exists". Verified.

**Spot-check for new issues:** One minor stale text in implementation step 1 still mentions the old `EventMethods` property name. The authoritative Files Modified — generator section (lines 177-178) is correct, so this won't mislead the developer. Not blocking.

**Status:** Plan is ready for Step 4. Recommend orchestrator set plan status to `Ready for Implementation`.

---

## Step 6 Verification (2026-04-14)

**Verdict: VERIFIED**

All builds clean, all tests pass, all 16 scenarios verified, KEEP list intact, version is 1.5.0. No defects.

### Build Status (Release, both TFMs in single build)

| Solution | Errors | Warnings | Result |
|---|---|---|---|
| `src/Neatoo.RemoteFactory.sln` | 0 | 2 (unrelated WASM `NativeFileReference`, OrderEntry.BlazorClient — pre-existing) | PASS |
| `src/Design/Design.sln` | 0 | 0 | PASS |
| `src/docs/reference-app/EmployeeManagement.sln` | 0 | 0 | PASS |

Both TFMs (net9.0, net10.0) compile in each solution build. The 2 warnings on the main solution are pre-existing WASM mono toolchain warnings (`SqlitePCLRaw.lib.e_sqlite3` native asset linking) on the OrderEntry.BlazorClient project — unrelated to this work.

### Test Status (Release, --no-build, console;verbosity=minimal)

| Assembly | TFM | Passed | Failed | Skipped | Total |
|---|---|---|---|---|---|
| RemoteFactory.UnitTests.dll | net9.0 | 553 | 0 | 0 | 553 |
| RemoteFactory.UnitTests.dll | net10.0 | 553 | 0 | 0 | 553 |
| RemoteFactory.IntegrationTests.dll | net9.0 | 563 | 0 | 3 | 566 |
| RemoteFactory.IntegrationTests.dll | net10.0 | 563 | 0 | 3 | 566 |
| Design.Tests.dll | net9.0 | 72 | 0 | 0 | 72 |
| Design.Tests.dll | net10.0 | 72 | 0 | 0 | 72 |

Skipped tests (3 per TFM in IntegrationTests) are exactly the expected `ShowcasePerformanceTests.ShowcasePerformance_DIObj`, `_NeatooObj`, `_Obj` perf-demo skips. No new skips introduced.

### Scenario-by-Scenario Verdicts

| # | Scenario | Verdict | Evidence |
|---|---|---|---|
| 1 | `class EventAttribute` absent in `src/RemoteFactory/` | VERIFIED | `Grep "class EventAttribute" src/RemoteFactory` = 0 matches |
| 2 | `EventAttribute` type absent | VERIFIED | source absent (scenario 1) → DLL absent (build green) |
| 3 | NF0401-NF0404 absent | VERIFIED | `Grep "NF040[1234]" src/Generator` = 0 matches |
| 4 | Non-event factory generation continues to compile + tests pass | VERIFIED | Subsumed by rule 16; all 553+566+72 tests pass on both TFMs |
| 5 | Mixed-attribute class — events silently gone | VERIFIED | `EventAttribute` deleted → consumer use becomes CS0246; generator no longer scans `[Event]` (no `Events` collection in models) |
| 6 | `[FactoryEventHandler<T>]` unchanged | VERIFIED | All `FactoryEventHandler/*Tests.cs` files (Local, ClientServer, Correlation, Error, Serialization, SharedScope) pass |
| 7 | Client-side relay unchanged | VERIFIED | `ForDelegateEvent` present at `MakeRemoteDelegateRequest.cs:21,28,29,167`; called by `RemoteFactoryEvents.cs:23,28`; v1.4 relay tests pass |
| 8 | Server mode no `IEventTracker` | VERIFIED | `IEventTracker.cs` deleted; type absent in compiled DLL |
| 9 | Logical mode no `IEventTracker` | VERIFIED | same — type does not exist anywhere |
| 10 | `IEventScopeInitializer` line removed from `AddRemoteFactoryServices.cs` | VERIFIED | `Grep "IEventScopeInitializer\|EventScopeInitializer\|AddRemoteFactoryEventScopeInitializer" AddRemoteFactoryServices.cs` = 0 matches |
| 11 | `ICorrelationContext` still scoped-registered | VERIFIED | `AddRemoteFactoryServices.cs:60` — `services.AddScoped<ICorrelationContext, CorrelationContextImpl>();` (and used at line 124) |
| 12 | `EventTrackerHostedService` registration removed | VERIFIED | `EventTrackerHostedService.cs` deleted; `Grep "EventTrackerHostedService\|Microsoft.Extensions.Hosting" ServiceCollectionExtensions.cs` = 0 matches |
| 13 | Log events 9001-9009 removed | VERIFIED | `Grep "EventId\s*=\s*9" src/RemoteFactory/Internal/Log.cs` = 0 matches |
| 14 | No test references deleted types | VERIFIED | grep over `src/Tests` returns only `FactoryEventBaseAttributeTests.cs`, which targets `FactoryEventAttribute` (KEPT, distinct type) |
| 15 | `IEventTestService` preserved + relocated | VERIFIED | New file `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/EventTestService.cs` exists; old `EventTargets.cs` deleted (`ls` shows only `EventTestService.cs`, `FactoryEventHandlerTargets.cs`, `FactoryEventRelayTargets.cs`, `FactoryEventTransactionTargets.cs`); 9 consumer files reference `IEventTestService` |
| 16 | Full suite Release / both TFMs passes | VERIFIED | Test status table above — 0 failures, 3 expected skips per TFM |

### Kept-Items Checklist

| Item | Status | Evidence |
|---|---|---|
| `IMakeRemoteDelegateRequest.ForDelegateEvent` | KEPT ✓ | Lines 21, 28, 29, 167 of `MakeRemoteDelegateRequest.cs` |
| `RemoteFactoryEvents.Raise` calls into `ForDelegateEvent` | KEPT ✓ | Lines 23, 28 of `RemoteFactoryEvents.cs` |
| `ICorrelationContext` scoped DI registration | KEPT ✓ | Line 60 of `AddRemoteFactoryServices.cs` |
| `FactoryEventAttribute` (singular, on `FactoryEventBase`) | KEPT ✓ | `FactoryEventAttribute.cs:18` |
| NF0405 (`FactoryEventHandlerMustBeStatic`) | KEPT ✓ | Confirmed by developer code review |
| `[FactoryEventHandler<T>]` infrastructure | KEPT ✓ | All `FactoryEventHandler/*Tests.cs` pass |
| Test container `ForDelegateEvent` shims | KEPT ✓ | `ClientServerContainers.cs:111`, `DesignClientServerContainers.cs:145`, `ClientServerContainerSamples.cs:133` |

### Working-Tree Verification

`git status --short` confirms:
- 7 production source files DELETED (`IEventTracker.cs`, `IEventScopeInitializer.cs`, `EventTracker.cs`, `CorrelationContextScopeInitializer.cs`, `DelegateEventScopeInitializer.cs`, `EventTrackerHostedService.cs`, `EventMethodModel.cs`) ✓
- 8 test files DELETED ✓
- Multiple reference-app `Samples/Events/*.cs` files DELETED ✓
- 1 NEW file: `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/EventTestService.cs` ✓
- All planned modifications present (FactoryAttributes.cs, FactoryOperation.cs, AuthorizeFactoryOperation.cs, AddRemoteFactoryServices.cs, Log.cs, generator files, csproj edits, AllPatterns.cs, Home.razor, FactoryEventHandlerPattern.cs, StaticFactoryTests.cs, etc.)

### Version

`src/Directory.Build.props` reads:
- `<FileVersion>1.5.0</FileVersion>`
- `<PackageVersion>1.5.0</PackageVersion>` ✓

### Defects

None. No build errors. No test failures. No KEPT-item regressions. No scenario violations.

### Final Verdict

**VERIFIED.** Implementation is clean and complete. Hand off to Step 6 Part B (Requirements Verification by business-requirements-reviewer).
