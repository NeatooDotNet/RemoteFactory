# Requirements Documenter -- Event Scope Initializer

Last updated: 2026-04-03
Current step: Requirements Documented

## Key Context

- All 14 business rules (BR-ESI-001 through BR-ESI-014) are NEW rules for the IEventScopeInitializer mechanism
- The `docs/events.md` file was already updated (by the developer or a prior process) with a complete "Event Scope Initializers" section before this agent ran. Verified the content is accurate.
- The `[GENERATOR BEHAVIOR]` comment in `CorrelationExample.cs` (lines 123-128) was updated by the developer to describe the new mechanism (Gap 5 resolved).
- One remaining inaccuracy in source code: `CorrelationExample.cs` lines 33-35 (remarks on `AuditedOrder` class) still describe the old timing ("captures the correlation ID before Task.Run"). Listed as Developer Deliverable.

## Mistakes to Avoid

None yet (first run).

## User Corrections

None yet (first run).

## Documentation Tracking

### Expected Deliverables

1. Update CLAUDE-DESIGN.md with IEventScopeInitializer section, Quick Decisions entries, checklist item
2. Update docs/events.md with Event Scope Initializers section
3. Note any source code comments that need updating as Developer Deliverables

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` | Added "Event Scope Initialization (IEventScopeInitializer)" section under Critical Rules > Event Registration Guards. Added 2 Quick Decisions entries (tenant context propagation, correlation auto-propagation). Added Design Completeness Checklist item. Updated Design Files to Consult entry for CorrelationExample.cs. Bumped design_version 1.3->1.4. | BR-ESI-001 through BR-ESI-014: all new rules documenting IEventScopeInitializer API, registration, generated code pattern, error handling, and timing semantics. |
| `docs/events.md` | Verified already updated: "Event Scope Initializers" section with Built-in, Custom Initializers, How Initializers Run, Copy Values Not References, Server-Only subsections. Updated pseudo-code and generated delegate example. Updated Correlation ID Tracking cross-reference. | Already done by prior process. Content verified accurate against implementation. |

### Developer Deliverables

- [ ] `src/Design/Design.Domain/Services/CorrelationExample.cs` lines 33-35: Update remarks comment on `AuditedOrder` class from "The generator captures the correlation ID before Task.Run and restores it in the event's new DI scope" to describe the new IEventScopeInitializer mechanism (e.g., "The built-in CorrelationContextScopeInitializer propagates the correlation ID from the request scope to the event scope via the IEventScopeInitializer mechanism.") -- Reason: Comment describes old hardcoded timing behavior; the mechanism now uses IEventScopeInitializer with inside-Task.Run reading.

### Step 8 Part B Needed?

Yes. The following non-requirements documentation deliverables exist:
- Release notes for the new `IEventScopeInitializer` feature (minor version bump, new public API)
