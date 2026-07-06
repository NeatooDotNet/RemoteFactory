# TRIM-003 — Verify event-record preservation needs no consumer entries

**Plan #:** 003
**Status:** Draft
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft
**Related Todo:** [../todo.md](../todo.md)

## Scope

Verification plan, expected no-code-change. `FactoryEventBase` has carried inherited `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` since v1.4.0 (`68e7324`), which should make every derived event record trimming-safe with no `[FactoryEventHandler<T>]` and no consumer LinkerConfig entry. But the consuming evidence is ambiguous: zTreatment's LinkerConfig event entries predate v1.4.0, were carried forward during its 1.5.0 migration ("updated to cover fine-grained panel events too"), and were never re-tested against the annotation. Confirm with a publish-trimmed repro matching the consumer's exact shape — event record whose ONLY client-side static reference is a generic `Subscribe<TEvent>(...)` lambda call site in a consumer-implemented `IFactoryEventRelay` aggregator (no handler attribute anywhere), deserialized from `RemoteResponseDto.RelayedEvents` and dispatched by runtime type. If the existing `EventRelaySmokeTest` doesn't already pin this consumer shape, add a `RemoteFactory.TrimmingTests` case for it. Outcome either way is recorded: green → zTreatment PCB-003 deletes its event-record entries on verification alone; red → the gap becomes a new TRIM plan with the repro as its failing test. Does NOT touch generator emission.
