# Requirements Documenter — delete-event-attribute-api (v1.5.0)

Last updated: 2026-04-14
Current step: Step 7 Part A complete; reporting back to orchestrator.

## Key Context

- v1.5.0 deletes the entire `[Event]` attribute API and its scope-isolation/EventTracker infrastructure.
- KEPT (do not confuse): `IFactoryEvents.Raise<T>`, `[FactoryEventHandler<T>]`, `IFactoryEventRelay`, `FactoryEventAttribute` (on `FactoryEventBase`), `ICorrelationContext`, NF0501/NF0502/NF0503, NF0405.
- Orchestrator confirmed VERIFIED + REQUIREMENTS SATISFIED before spawning.
- Reviewer's memory (in requirements-reviewer.md) flagged specific line ranges in CLAUDE-DESIGN.md — all addressed though line ranges had drifted.
- `docs/events.md` was NOT deleted — rewrote as a short deprecation-redirect stub that points at `docs/factory-events.md` and the v1.5.0 release notes. This preserves Jekyll cross-references from other pages without rotting links; the migration message is clear.

## Mistakes to Avoid

- None yet — first run.

## User Corrections

- None.

## Documentation Tracking

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` | Bumped `design_version: 1.6 -> 1.7`. Removed `[Event]` from Decision Table Static Factory row and "Choose Static Factory when" list. Deleted `[Remote, Event]` example from Pattern 3. Dropped "When to use `[Event]` delegates instead" paragraph; replaced with manual `Task.Run + IServiceScopeFactory.CreateScope()` guidance pointing at v1.5.0 release notes. Deleted 4 rows from Quick Decisions table (`Do [Event] methods need CancellationToken`, `I want a handler to fire-and-forget`, `propagate tenant context to [Event]`, `correlation ID propagated to [Event]`). Replaced fire-and-forget row with manual-pattern row. Deleted `Event Registration Guards` and `Event Scope Initialization (IEventScopeInitializer)` subsections. Deleted Critical Rule 6 (Event Delegate Types Have `Event` Suffix). Updated Design Completeness Checklist (`[Execute] and [Event] -> [Execute]` only; removed CorrelationContext + Event scope initialization + event-handlers-with-CT items). Deleted Common Mistakes items 5 (Forgetting Event suffix) and 8 (Missing CancellationToken on events); renumbered. Removed `CorrelationExample.cs` row from Design Files to Consult. | Rules 1, 2, 3, 4, 5, 8, 9, 10 (Event attribute, scope initializer, event tracker, correlation scope initializer all removed). |
| `docs/factory-events.md` | Replaced opening "two distinct event features" framing + comparison table with a single-surface description. Rewrote "Why Not [Event]?" section as "Transactional vs Fire-and-Forget" pointing at manual pattern + v1.5.0 release notes. Replaced `Events` link in Next Steps with v1.5.0 release notes link. | Comparison table no longer relevant; `[Event]` row would have had nothing to compare against. |
| `docs/events.md` | Rewrote entire file as deprecation-redirect stub pointing at `factory-events.md` and `release-notes/v1.5.0.md`. | File's entire content covered deleted API. Kept stub (instead of deleting) to avoid breaking Jekyll cross-references that I did NOT want to hunt down across archival release notes. Other pages' cross-references to `events.md` were removed in this pass. |
| `docs/attributes-reference.md` | Removed `[Event]` row from Quick Lookup table. Deleted entire `### [Event]` section (snippet + body). Updated `[FactoryEventHandler<T>]` section to point at manual pattern instead of "use `[Event]` instead" for fire-and-forget. Removed `[Event]` from the Attribute Inheritance table's comma-list of non-inherited ops. | Attribute no longer exists. |
| `docs/interfaces-reference.md` | Deleted entire `## Event Tracking > ### IEventTracker` section (including snippet). Deleted `### IEventScopeInitializer` section (including `AddRemoteFactoryEventScopeInitializer` example). Updated `IFactoryEvents` "For fire-and-forget work, use `[Event]` delegates instead" to point at manual pattern + v1.5.0 release notes. Removed `IEventTracker` and `IEventScopeInitializer` rows from Summary table. Replaced Next Steps `Events` link with `Factory Events`. | Interfaces no longer exist. |
| `docs/trimming.md` | Removed `### Event Registrations` subsection (`[Event]` guards, `IEventTracker`, remote event stubs). Edited "Static factories" bullet: "Delegate and event registrations are guarded" → "`[Execute]` delegate registrations are guarded". | Event registrations no longer generated. |
| `docs/factory-operations.md` | Removed `[Event]` row from operations overview table. Replaced entire `## Event Operation` section (including EventTracker subsection) with a new `## Events` section — covers the mediator surface and shows the manual fire-and-forget pattern for cases where the caller's transaction should not participate; points at factory-events.md and v1.5.0 release notes. Replaced Next Steps `Events` link with `Factory Events`. | Event operation no longer exists. |
| `docs/aspnetcore-integration.md` | Removed `## Event Tracking` section (EventTrackerHostedService registration documentation). Replaced Next Steps `Events` link with `Factory Events`. | Service no longer registered. |
| `docs/decision-guide.md` | Updated 3-pattern table "Static Factory" row (removed events/`[Event]`). Updated pattern decision tree (removed `[Event] for side effects`). Updated following-paragraph reference from `events.md` to `factory-events.md`. Replaced "When to Use [Event]?" section with a new "How Do I Handle Domain Events?" section pointing at `[FactoryEventHandler<T>]` mediator and the manual fire-and-forget pattern for cross-transaction work. | Pattern deletion + guidance change. |

### Files Deleted

None. (`docs/events.md` rewritten in place as a redirect stub; not deleted.)

### Developer Deliverables

None identified. The plan's Step 4 already cleaned up reference-app and Design-project source files, and the orchestrator's spawn prompt confirmed both verifications passed. Reference-app `csproj` comment edits (lines 11 and 15 in EmployeeManagement.Domain.csproj and EmployeeManagement.Application.csproj — `<!-- CA1822: Event methods must be instance methods for [Event] attribute -->`) may or may not have been touched in Step 4; the orchestrator should confirm. If they still mention `[Event]`, reword to drop the reference (keep the `<NoWarn>` suppression). Surface this as a potential Step 8 Part B cleanup if an orchestrator review finds them stale.

### Step 8 Part B Needed?

Yes — Step 8 Part B is explicitly required: the plan's "Documentation (Step 7, after verification)" section calls for `docs/release-notes/v1.5.0.md` with breaking-change section + migration guide covering:
- Graceful shutdown (manual tracking of outstanding fire-and-forget tasks)
- Correlation ID propagation (manual snapshot-and-copy)
- Tenant/ambient context (explicit copy inside `Task.Run`)

Additionally, the plan names several skill files for update (`skills/RemoteFactory/SKILL.md`, `references/factory-events.md`, `references/static-factory.md`) — these require reference-app code changes followed by `mdsnippets`. The docs-writer agent handles skill regeneration + release-notes authoring in Step 8 Part B.

Release-notes index page update (`docs/release-notes/index.md`): the docs-writer should add the v1.5.0 entry and bump nav_order on existing releases.
