# Docs Writer -- Event Scope Initializer

Last updated: 2026-04-03
Current step: Documentation Complete

## Documentation Tracking

### Files Updated
| File | What Changed |
|------|-------------|
| `docs/events.md` | Added "Event Scope Initializers" section covering built-in correlation propagation, custom initializers, execution semantics, copy-not-reference rule, and server-only behavior. Updated "Correlation ID Tracking" intro to reference the new initializer mechanism. Updated pseudo-code in "How Events Work" and "Error Handling" to show initializer loop. Added interfaces-reference cross-link in Next Steps. |
| `docs/interfaces-reference.md` | Added `IEventScopeInitializer` subsection under Event Tracking with interface signature, usage example, and behavioral notes. Added `IEventScopeInitializer` row to Summary table. Added Events page cross-link in Next Steps. |

### Files Created
None.

### Deliverables Skipped (N/A)
| Deliverable | Reason |
|-------------|--------|
| `docs/trimming.md` update | No changes needed. Event registrations are already documented as guarded by `IsServerRuntime`. Initializer resolution lives inside the existing guard block -- trimming behavior is unchanged. |
| `docs/aspnetcore-integration.md` update | No changes needed. Correlation ID section describes header propagation and `ICorrelationContext` availability, which is unchanged. The initializer mechanism is an internal implementation detail of how correlation flows to event scopes, not an ASP.NET Core integration concern. |
| Release notes | Not created as part of this step. The event scope initializer feature is on the `debug-logging` branch alongside trace logging (v0.28.0). Release notes for this feature would be created when the version is bumped for this feature specifically. |
