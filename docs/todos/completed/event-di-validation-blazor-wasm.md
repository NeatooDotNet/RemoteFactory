# [Event] DI Validation Failure on Blazor WASM Client

**Status:** Complete
**Priority:** High
**Created:** 2026-03-27
**Last Updated:** 2026-03-27
**Plan:** [event-di-validation-fix.md](../plans/event-di-validation-fix.md)

---

## Problem

User report: When a `[Factory]` static class uses `[Remote, Event]` methods, the generated factory registrar registers event delegate types on both client and server. The delegates themselves are fine — on the client they become remote call proxies. However, the generated server-side event infrastructure requires `IHostApplicationLifetime` and `IEventTracker`, which are only available in ASP.NET Core hosting — not in Blazor WASM.

The problem surfaces at startup when `AddNeatooRemoteFactory(NeatooFactory.Remote, assembly)` runs on the Blazor WASM client. DI validation walks the entire dependency graph and fails because it can't construct the event infrastructure services. The error cascades: it reports unrelated services (like repositories) as unresolvable because the graph validation fails early.

User's workaround: Register a no-op `IHostApplicationLifetime` on the Blazor WASM client.

To reproduce:
1. Add a `[Remote, Event]` method to any `[Factory]` static class in a shared domain models assembly
2. Reference that assembly from a Blazor WASM client project
3. Client startup throws `AggregateException` — "Some services are not able to be constructed"

The user believes this requires IL trimming to reproduce.

## Solution

`[Event]` server-only services should be conditionally registered (behind `NeatooRuntime.IsServerRuntime` or similar), so the client-side DI container never tries to validate them. The `IEventTracker` registration in `AddRemoteFactoryServices.cs:68` is currently unconditional.

---

## Clarifications

### Architect Q&A (2026-03-27)

**Q1:** Should the fix for Bug 2 include adding the `NeatooRuntime.IsServerRuntime` guard to the class factory's `RenderLocalEventRegistration` to match the static factory pattern? Or is aligning the conditional registration of `IEventTracker` (Bug 1) sufficient?

**A1:** Fix both. Add the `IsServerRuntime` guard to the class factory renderer for consistency with the static factory renderer, AND make `IEventTracker` registration conditional. Belt-and-suspenders approach confirmed.

**Architect status:** Ready to proceed.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-27
**Verdict:** APPROVED

### Relevant Requirements Found

1. **IsServerRuntime guard pattern for trimming (Critical Rule 2, `src/Design/CLAUDE-DESIGN.md` lines 444-479; `docs/trimming.md` lines 15-38).** Internal and `[Remote]` methods get `if (NeatooRuntime.IsServerRuntime)` guards so the IL trimmer can eliminate server-only code. Static factory event registrations already follow this pattern (`src/Generator/Renderer/StaticFactoryRenderer.cs:300`). Class factory event registrations do not (`src/Generator/Renderer/ClassFactoryRenderer.cs:1598-1632`). Bug 2 proposes adding the missing guard, which aligns with the established pattern.

2. **Event infrastructure is server-only (`docs/events.md` lines 499-509; `src/Design/CLAUDE-DESIGN.md` lines 40, 60-61).** Events in Remote mode serialize to the server; the local event registration (scope isolation, `Task.Run`, `IHostApplicationLifetime`, `IEventTracker`) only runs on Server/Logical mode. The generated code already branches on `remoteLocal` (class factory: `ClassFactoryRenderer.cs:1562-1571` for remote vs `1571-1579` for local). Bug 2's guard adds the additional trimming-level protection within the local branch.

3. **IEventTracker registration is unconditional (`src/RemoteFactory/AddRemoteFactoryServices.cs:68`).** `EventTracker` requires `ILogger<EventTracker>` (constructor at `src/RemoteFactory/Internal/EventTracker.cs:16`). On Blazor WASM clients without logging configured, DI validation fails when walking the dependency graph. Bug 1 proposes making this registration conditional, which is consistent with the principle that event infrastructure is server-only.

4. **EventTracker is used by generated local event registrations and by `EventTrackerHostedService` (`src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs:42-48`).** The ASP.NET Core package resolves `IEventTracker` via `GetRequiredService`. On the server side (which always has logging), this works. On the client side in Remote mode, `IEventTracker` is not needed by the generated remote event stubs (they serialize to the server via `IMakeRemoteDelegateRequest.ForDelegateEvent`). Making registration conditional on server/logical mode is safe.

5. **CancellationToken is required on [Event] methods (`src/Design/CLAUDE-DESIGN.md` line 148; `docs/events.md` lines 100-104).** The proposed changes do not alter this requirement. The fix is purely about DI registration, not method signatures.

6. **Event delegates get `Event` suffix (`src/Design/CLAUDE-DESIGN.md` lines 612-620).** Not affected by the proposed changes.

7. **Design test containers register `IHostApplicationLifetime` and logging (`src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs:221-222`).** This is why the Design tests pass today -- they explicitly provide these dependencies for all three container modes. The bug only surfaces in real Blazor WASM client startup where these are not registered.

8. **Design Debt table (`src/Design/CLAUDE-DESIGN.md` lines 728-739).** Neither proposed fix touches any deliberately deferred feature. No conflict.

9. **Anti-Patterns 1-9 (`src/Design/CLAUDE-DESIGN.md` lines 158-419).** Neither proposed fix introduces or violates any documented anti-pattern.

### Gaps

1. **No documented requirement for conditional IEventTracker registration.** The events documentation (`docs/events.md`) and CLAUDE-DESIGN.md describe `IEventTracker` as always available (`GetRequiredService<IEventTracker>()` is shown in event caller samples). After Bug 1, client-side code in Remote mode will not have `IEventTracker` registered. If any user code on the client resolves `IEventTracker` directly (not through generated event delegates), it will fail. The architect should decide whether to: (a) register a no-op `IEventTracker` on the client, or (b) make the registration fully conditional and document that `IEventTracker` is server-only.

2. **No existing test for class factory [Event] with Blazor WASM DI validation.** The unit tests (`src/Tests/RemoteFactory.UnitTests/TestTargets/Events/EventTargets.cs`) and integration tests (`src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/EventTargets.cs`) have class factory event targets, but the test containers always provide `IHostApplicationLifetime` and logging. There is no test that validates a Remote-mode container without these services succeeds at DI validation. The architect should require a test for this scenario.

3. **No explicit requirement documenting parity between static and class factory event rendering.** The current asymmetry (static has `IsServerRuntime` guard, class does not) is undocumented. After the fix, parity should be documented as an invariant.

### Contradictions

None found. Both proposed fixes are consistent with documented patterns:
- Bug 1 (conditional `IEventTracker`) aligns with the principle that event infrastructure is server-only.
- Bug 2 (adding `IsServerRuntime` guard to class factory) restores parity with the static factory renderer, following the established trimming pattern documented in `docs/trimming.md` and used throughout `ClassFactoryRenderer.cs` for other method types.

### Recommendations for Architect

1. **For Bug 1 (IEventTracker registration):** Consider registering a no-op `IEventTracker` on Remote-mode clients rather than removing the registration entirely. The `IEventTracker` interface is public and documented in `docs/events.md` as available for monitoring. A no-op avoids breaking user code that injects `IEventTracker` on the client side. Alternatively, if the intent is that `IEventTracker` is strictly server-only, document this clearly.

2. **For Bug 2 (IsServerRuntime guard):** The implementation should mirror the exact pattern in `StaticFactoryRenderer.RenderLocalEventRegistration` (line 300-335): wrap the entire `services.AddScoped<...>` registration inside `if (NeatooRuntime.IsServerRuntime) { ... }`. This is the belt-and-suspenders approach confirmed in Architect Q&A A1.

3. **Test coverage:** Add a test that creates a Remote-mode DI container WITHOUT `IHostApplicationLifetime` and WITHOUT logging, and verify that the container builds successfully and that event delegates resolve (as remote stubs). This directly validates the bug fix scenario.

4. **Design project consideration:** The Design project's `DesignClientServerContainers` registers `IHostApplicationLifetime` and logging for all modes (line 221-222). This masks the bug. Consider whether the client container in tests should NOT register `IHostApplicationLifetime` to match real Blazor WASM behavior and catch regressions.

---

## Plans

- [Fix Event DI Validation Failure on Blazor WASM Client](../plans/event-di-validation-fix.md)

---

## Tasks

- [x] Create todo
- [x] Reproduce the issue (TrimmingTests with [Remote, Event] + ValidateOnBuild)
- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3) — APPROVED
- [x] Architect plan creation (Step 4)
- [x] Developer review (Step 5) — Approved
- [x] Implementation (Step 7) — NullEventTracker, conditional registration, ClassFactory guard
- [x] Verification (Step 8) — VERIFIED + REQUIREMENTS SATISFIED
- [x] Documentation (Step 9) — CLAUDE-DESIGN.md, events.md, trimming.md, release notes v0.24.2

---

## Progress Log

### 2026-03-27
- Created todo from user's bug report
- Discovery: `Design.Client.Blazor` already references `Design.Domain` which has `ExampleEvents` with `[Remote, Event]` — exact reproduction scenario
- Discovery: `IEventTracker` registered unconditionally in `src/RemoteFactory/AddRemoteFactoryServices.cs:68`
- Discovery: Generated static factory event server registration IS guarded by both `remoteLocal` check AND `NeatooRuntime.IsServerRuntime`
- Discovery: Generated class factory event server registration is guarded by `remoteLocal` check but NOT by `NeatooRuntime.IsServerRuntime`
- Discovery: `TrimmingTests` project exists with `PublishTrimmed=true` but doesn't have `[Event]` methods
- Next: Try to reproduce with Design.Server/Design.Client.Blazor

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: Pass (0 errors, both TFMs)
- Tests: Pass (500 unit + 502 integration per TFM, 0 failures)

---

## Results / Conclusions

Two bugs fixed in v0.24.2:

1. **Bug 1 (IEventTracker registration):** Created `NullEventTracker` (no-op, zero dependencies) for Remote mode. `EventTracker` (full implementation with logging) remains for Server/Logical. Conditional registration in `AddRemoteFactoryServices.cs` preserves the public `IEventTracker` API contract.

2. **Bug 2 (ClassFactory IsServerRuntime guard):** Added `if (NeatooRuntime.IsServerRuntime)` guard to `ClassFactoryRenderer.RenderLocalEventRegistration`, matching the existing pattern in `StaticFactoryRenderer`. Enables IL trimmer to eliminate dead server-side event code from client assemblies.

10 new tests added (6 NullEventTracker unit tests, 4 DI registration validation tests). Zero regressions across 1000 unit + 1004 integration test runs. TrimmingTests validated end-to-end without `AddLogging()`.
