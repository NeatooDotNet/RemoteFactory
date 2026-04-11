# Docs Writer — Factory Events v1.1.0 Refactor Audit

Last updated: 2026-04-10
Current step: Audit complete for v1.1.0 execution-model flip

## Context

v1.1.0 reversed the v1.0.0 `[FactoryEventHandler<T>]` execution model:
- Shared scope (was: isolated)
- Sequential, awaited (was: parallel-ish, fire-and-forget)
- Exceptions propagate (was: swallowed)
- `Raise<T>` gained `CancellationToken`
- `RaiseOptions.AwaitRemote` removed
- `RaiseOptions.ContinueOnFail` removed
- `RaiseOptions.ServerOnly` kept
- `[Event]` delegate attribute UNTOUCHED — still fire-and-forget, still uses
  `IEventTracker` / `IEventScopeInitializer` / `ApplicationStopping`

## Documentation Tracking

### Files Updated

| File | What Changed |
|------|--------------|
| `docs/factory-events.md` | Fixed stale "static handlers run in isolated scopes as usual" in Logical Mode section (now says "in the caller's scope, sequentially, awaited"). Fixed stale "static handler runs in isolated scope" comment in Complete Example (now correctly describes shared-scope transactional behavior). Updated Raising-an-Event, Anti-pattern, ServerOnly, and Complete Example code samples to thread the factory method's `CancellationToken` through `Raise`. Added explicit `Raise<T>` signature block showing the CT parameter. |
| `docs/interfaces-reference.md` | Clarified `IEventTracker` section to state it is **not** involved in `[FactoryEventHandler<T>]` dispatch — it is an `[Event]`-delegate-only concern. Clarified `IEventScopeInitializer` section same way — it propagates context for `[Event]` delegate scopes only; factory event handlers already share the caller's scope and need nothing. Both clarifications include cross-references to factory-events.md. |
| `docs/release-notes/v1.0.0.md` | Added a "Partially superseded by v1.1.0" callout at the top explaining that the detached/fire-and-forget execution model and `AwaitRemote`/`ContinueOnFail` flags described in this file were reversed in v1.1.0 (shipped the same day). Pointer to v1.1.0 and factory-events.md for current behavior. Historical prose below is unchanged. |
| `src/Design/CLAUDE-DESIGN.md` | Fixed stale "server-side handler in isolated scope" description in Key Files table (line 1002) to match the transactional v1.1.0 behavior. |

### Files Audited and Left Unchanged

| File | Reason |
|------|--------|
| `docs/factory-events.md` (Execution Model section, table header, why-not-Event comparison, Client Relay, RaiseOptions, anti-patterns, decision guide) | Already correctly describes v1.1.0 behavior — shared scope, sequential, unspecified order, awaited, exceptions propagate. |
| `docs/events.md` | Describes `[Event]` delegate methods — correctly still fire-and-forget with `IEventTracker`, `ApplicationStopping`, `IEventScopeInitializer`. Top-of-page pointer to factory-events.md already explicit about the feature split. |
| `docs/attributes-reference.md` | `[FactoryEventHandler<T>]` entry already correctly describes shared-scope, sequential, awaited, transactional semantics with "for fire-and-forget semantics use `[Event]` instead" pointer. `[Event]` entry unchanged. `RaiseOptions` not called out separately but covered via factory-events.md link. |
| `docs/release-notes/v1.1.0.md` | Release notes are complete and accurate — overview, what's-new, breaking-changes, migration guide, design rationale, commits all match the actual change. |
| `docs/release-notes/index.md` | Both highlights table and all-releases list correctly describe v1.1.0 as the breaking flip. |
| `skills/RemoteFactory/SKILL.md` | Quick-decisions table correctly distinguishes `[FactoryEventHandler<T>]` (shared-scope, transactional) from `[Event]` (isolated-scope, tracked by `IEventTracker`). Reference file pointers already correct. |
| `skills/RemoteFactory/references/factory-events.md` | Already fully rewritten for v1.1.0 — three invariants, CT parameter, handler ordering is unspecified, anti-pattern for fire-and-forget-work-in-FactoryEventHandler, decision table. |
| `skills/RemoteFactory/references/static-factory.md` | `[Event]` section correctly describes fire-and-forget, points to factory-events.md for the mediator pattern. |
| `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` | XML doc comments and design-decision narrative already describe the shared-scope/sequential/awaited invariants correctly. |
| `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs` | Class comment correctly describes static=server-handler-in-caller's-scope vs instance=client-relay. Server-side raiser comment correctly describes sequential, awaited, transaction-rollback semantics. |
| `docs/plans/**` | Explicitly out-of-scope per task instructions — historical artifacts. Left the v0 `factory-events-mediator.md` plan untouched even though it still references `AwaitRemote`/`ContinueOnFail`. |

### Deliverables Skipped (N/A)

None — all identified audit targets were addressed.

## Confidence Verdict

New functionality is clearly documented; old functionality is fully purged from
published docs and Design source-of-truth. Historical v1.0.0 release notes
retain their original wording but carry an explicit superseded-by-v1.1.0
callout at the top so readers cannot mistake them for current behavior.
