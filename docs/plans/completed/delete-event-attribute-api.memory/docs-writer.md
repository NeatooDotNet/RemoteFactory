# Docs Writer — Delete `[Event]` Method Attribute API (v1.5.0)

Last updated: 2026-04-14
Current step: Step 7 Part B — General documentation complete

## Documentation Tracking

### Files Created
| File | Purpose |
|------|---------|
| `/home/keithvoels/neatoodotnet/RemoteFactory/docs/release-notes/v1.5.0.md` | Full v1.5.0 release notes: breaking deletions, "What's Still Here" callout, migration guide (Task.Run + child scope, graceful shutdown tracker pattern, correlation snapshot-and-copy, tenant/ambient context) |

### Files Updated
| File | What Changed |
|------|-------------|
| `/home/keithvoels/neatoodotnet/RemoteFactory/docs/release-notes/index.md` | Added v1.5.0 highlights row + All Releases entry at top |
| `/home/keithvoels/neatoodotnet/RemoteFactory/docs/release-notes/v1.4.0.md` | `nav_order: 1` → `nav_order: 2` (v1.5.0 is now newest) |
| `/home/keithvoels/neatoodotnet/RemoteFactory/skills/RemoteFactory/SKILL.md` | Removed `[Event]` from description keywords; changed "Static Factory stateless commands and events" → "stateless commands"; removed "Do [Event] methods need CancellationToken?" row and "IEventTracker" row; added fire-and-forget → Task.Run + IServiceScopeFactory row; updated reference description for static-factory.md |
| `/home/keithvoels/neatoodotnet/RemoteFactory/skills/RemoteFactory/references/factory-events.md` | Rewrote intro — no more "two competing pipelines" comparison; factory events are the only event-shaped abstraction. Updated "Using a Factory Event for Fire-and-Forget Work" anti-pattern to show Task.Run + child scope instead of `[Event]`. Updated "When to Use What" table rows for fire-and-forget. |
| `/home/keithvoels/neatoodotnet/RemoteFactory/skills/RemoteFactory/references/static-factory.md` | Deleted "Event Handlers (Fire-and-Forget)" section, `[Event]` skill-static-event-handlers snippet, `[Event]` CancellationToken rule, "Graceful Shutdown with IEventTracker" section (two embedded snippets), updated intro, replaced with pointer to factory-events.md. Updated "When to Use Static Factory" list. |
| `/home/keithvoels/neatoodotnet/RemoteFactory/skills/RemoteFactory/references/anti-patterns.md` | Deleted "9. [Event] Missing CancellationToken" section; renumbered 10-13 → 9-12; removed `[Event]` row from summary table. |
| `/home/keithvoels/neatoodotnet/RemoteFactory/README.md` | Line 282 feature bullet: "Fire-and-forget events via `[Event]` attribute" → "Domain events via `[FactoryEventHandler<T>]` + `IFactoryEvents.Raise` + `IFactoryEventRelay`". |

### MarkdownSnippets Status

Ran `mdsnippets` from repo root — completed clean in 343ms. Skill snippet `skill-static-execute-commands` re-embedded with updated line range (source file shifted after Skill/ cleanup). No broken region references remain in the skill files.

### Deliverables Skipped (N/A)

None — every Task 1-4 deliverable addressed.

## Follow-ups

None required. The v1.5.0 release notes link to the plan file (`../plans/delete-event-attribute-api.md`) and a GitHub release URL that will be populated at tag-push time; commits section intentionally leaves auto-generation to GitHub release notes.
