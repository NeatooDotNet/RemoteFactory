# Requirements Documenter -- Event DI Validation Fix

Last updated: 2026-03-27
Current step: Documentation complete, reporting to orchestrator

## Key Context

This was a bug fix with two parts:
1. Bug 1: `IEventTracker` was unconditionally registered as `EventTracker` (which requires `ILogger`), causing DI validation failures on Blazor WASM clients without logging. Fix: conditional registration -- `NullEventTracker` (no-op) for Remote mode, `EventTracker` for Server/Logical.
2. Bug 2: `ClassFactoryRenderer.RenderLocalEventRegistration` was missing the `IsServerRuntime` guard that `StaticFactoryRenderer` already had. Fix: add matching guard for parity.

Both fixes are belt-and-suspenders: the conditional registration prevents the DI validation failure, and the guard ensures the trimmer can eliminate server-only event infrastructure from client assemblies.

## Mistakes to Avoid

None encountered.

## User Corrections

None.

## Documentation Tracking

### Expected Deliverables
From plan acceptance criteria line: "Documentation deliverables identified: `docs/events.md` should note `NullEventTracker` behavior in Remote mode (Step 9)"

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` | Added "Event Registration Guards" subsection under Critical Rule 2 (guard emission). Documents that both class and static factory `[Event]` registrations are wrapped in `IsServerRuntime` guards. Updated `last_updated` date. | Plan Rule 6 (NEW): class factory event registrations now have `IsServerRuntime` guard, establishing parity with static factory. Gap 3 from requirements review. |
| `docs/events.md` | Added mode-specific `IEventTracker` implementation table in EventTracker section. Explains `NullEventTracker` for Remote mode, `EventTracker` for Server/Logical. Updated "Events in Different Modes" section to list which `IEventTracker` implementation resolves in each mode. | Plan Rule 1 (NEW): Remote mode resolves `NullEventTracker`. Plan acceptance criteria documentation deliverable. |
| `docs/trimming.md` | Added "Event Registrations" subsection under "Static and Interface Factories" documenting that both class and static factory event registrations have `IsServerRuntime` guards. Previously only mentioned static factories. | Plan Rule 6/8 (NEW): class factory event registrations are now also guarded and trimmable. Reviewer note that docs were "slightly incomplete." |

### Developer Deliverables

No source code documentation deliverables needed. The implementation is complete and the Design project does not need code changes for this bug fix -- the existing `ExampleEvents` and `ExampleCommands` in `Design.Domain` already demonstrate event patterns, and the fix is infrastructure-level (DI registration and generator output), not a new pattern.

### Step 9 Part B Needed?

Release notes should be created for this bug fix since it changes runtime behavior (NullEventTracker for Remote mode) and generator output (class factory event IsServerRuntime guard). This is a patch-level fix.

- Release notes: Yes -- bug fix affecting Blazor WASM clients with `[Event]` methods
- README changes: No
- Migration guide: No (fix is transparent; existing code continues to work)
- Architecture docs: No
