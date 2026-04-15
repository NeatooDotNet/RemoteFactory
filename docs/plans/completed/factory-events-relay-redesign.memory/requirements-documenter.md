# Requirements Documenter — Factory Events Client Relay Redesign

Last updated: 2026-04-14
Current step: Step 7 Part A complete — Requirements Documented

## Key Context

This is a first-run plan for this agent. Reviewer's Step 6B memo provided the
14-item worklist and the 10 new requirement entries (A–J) that needed documentation.

Breaking change: client-side `[FactoryEventHandler<T>]` instance-method handlers,
`IFactoryEventRelay.Register/Unregister`, `FactoryEventRelayRegistry`,
`FactoryEventRelayDispatcher`, and the generator's client-relay emission path were
removed. Replaced with consumer-implemented
`IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)`, a NoOp default, and a
runtime `FactoryEventTypeRegistry`.

## Mistakes to Avoid

None yet.

## User Corrections

None yet.

## Documentation Tracking

### Expected Deliverables

From reviewer memo (14 items):
1–6. `src/Design/CLAUDE-DESIGN.md` sections (lines 200–234, 256, 957, 1008, 1012,
   execution bullets 229/230/231/232, Decision Table client entry, diagnostics
   section, design completeness checklist).
7. `docs/factory-events.md` — rewrite instance-method example, add NF0503, log events.
8. `docs/events.md` — references to `Register/Unregister` (none found beyond the
   existing header comment, which already redirects to factory-events.md).
9. `docs/interfaces-reference.md` — rewrite `IFactoryEventRelay` entry.
10. `docs/attributes-reference.md` — rewrite `[FactoryEventHandler<T>]` instance
    block; add new `[FactoryEvent]` section.
11. `docs/trimming.md` — rewrite "Factory Event Type Preservation"; update
    FactoryEventRelayRegistry mention.
12. `docs/diagnostics/NF0503.md` — new page. (Decided NOT to create standalone
    page; documented NF0503 inline in CLAUDE-DESIGN.md + factory-events.md
    Diagnostics tables.)
13. Skill updates — deferred to Developer Deliverables (MarkdownSnippets workflow).
14. Release notes — explicitly out of my scope (Step 7 Part B — general docs writer).

Plus 10 new requirement entries (A–J from reviewer memo): NF0503, log events
3008/3009/3011/3012, one-Relay-per-call invariant, post-return hard guarantee,
`[FactoryEvent]` inheritance, runtime registry, `UnknownFactoryEventTypeException`.
All documented.

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` | Bumped `design_version` to 1.6; rewrote Pattern 4 intro (replaced "based on static/instance" with "two separate surfaces"); rewrote client-side relay example (OrderViewModel+Register → `MyClientRelay : IFactoryEventRelay`); rewrote Execution Model bullets covering null-batch normalization, hard post-return ordering, exception logging (3008/3009), one-call-per-[Remote] invariant with `UnknownFactoryEventTypeException` escape, `FactoryEventTypeRegistry` runtime scan + collision log (3012), `NoOpFactoryEventRelay` + 3011, TryAdd semantics; updated Decision Table row for "client event handling"; rewrote `Factory event type preservation` paragraph (per-handler walk removed; base-class `[DynamicallyAccessedMembers]` is the single mechanism); added new `Diagnostics and Log Events (Factory Events Relay)` section documenting NF0501/0502/0503 + log events 3008/3009/3011/3012 + `UnknownFactoryEventTypeException`; updated Design Completeness Checklist; updated Design Files index rows. Removed "Known gap: Dictionary<K,V> value types" callout (nested walk no longer exists). | Rules 1,2,4,5,6,7,8,9,10,11,12,13,14,15,16,18 + new entries A–J |
| `docs/factory-events.md` | Rewrote `Client relay` table row; rewrote Client-Side Relay section (was "Instance Method" subsection — now "Consumer Implements `IFactoryEventRelay`"); replaced `Register/Unregister` interface definition with single-method interface; added full contract bullets (one-call-per-[Remote], post-return ordering, exception logging, trimming); added "Default No-Op Registration" subsection (TryAdd semantics, 3011 warning); updated Decision Guide row; rewrote "Serialization and Transport" — `FactoryEventTypeRegistry` runtime scan, collision behavior, 3012; rewrote "IL Trimming and Event Records" — base class annotation, no codegen; added NF0503 row + new "Runtime Log Events" table (3008/3009/3011/3012) + `UnknownFactoryEventTypeException`; rewrote DI Registration (TryAdd semantics, consumer lifecycle); rewrote Complete Example client section (consumer-owned `CheckoutUiRelay`). | Rules 1–18 + new entries A–J |
| `docs/interfaces-reference.md` | Rewrote `IFactoryEventRelay` section — new single-method surface with full contract bullets, TryAdd semantics, migration note referencing NF0503; updated index table row. | Rules 1,4,5,6,7,8,9,12,16,18 + entries A,F,G |
| `docs/attributes-reference.md` | Rewrote `[FactoryEventHandler<T>]` section — static-only method matching rules; removed client-side instance handler block; added migration admonition pointing at NF0503 and `IFactoryEventRelay`; added new `[FactoryEvent]` subsection explaining inheritance-driven discovery. | Rules 14,16 + entries A,H |
| `docs/trimming.md` | Updated consequences-of-missing-PackageReference bullet (removed `FactoryEventRelayRegistry` mention); rewrote "Factory Event Type Preservation" — base-class `[DynamicallyAccessedMembers]` + `[FactoryEvent]` inherited, no generator emission, no per-handler walk; dropped `PreserveType<T> vs Register<T>` event-specific table (still covered elsewhere); removed `Dictionary<K,V>` event-specific limitation block (nested walker deleted); kept "User Code That Forwards Raise<T>" section intact. | Rule 14 + entries H,I |

### New Requirement Entries Added

All 10 reviewer-listed entries (A–J):

- **A. NF0503 Warning** — in CLAUDE-DESIGN.md Diagnostics section, factory-events.md Diagnostics table, attributes-reference.md admonition, interfaces-reference.md migration note.
- **B. Log Event 3008 `FactoryEventRelayFailed`** — CLAUDE-DESIGN.md + factory-events.md log-event tables.
- **C. Log Event 3009 `FactoryEventDeserializationFailed`** — same locations.
- **D. Log Event 3011 `NoOpFactoryEventRelayFirstEvent`** — same locations + Default No-Op Registration subsection in factory-events.md.
- **E. Log Event 3012 `FactoryEventTypeRegistryCollision`** — same locations + Serialization and Transport subsection in factory-events.md.
- **F. One-Relay-per-call invariant** — Execution Model bullets in CLAUDE-DESIGN.md; Client-Side Relay contract in factory-events.md; IFactoryEventRelay contract in interfaces-reference.md.
- **G. Post-return ordering hard guarantee** — same three locations.
- **H. `[FactoryEvent]` inheritance** — attributes-reference.md new section; CLAUDE-DESIGN.md trimming paragraph; trimming.md rewritten section.
- **I. `FactoryEventTypeRegistry` runtime scan** — CLAUDE-DESIGN.md trimming + execution bullets; factory-events.md Serialization and Transport.
- **J. `UnknownFactoryEventTypeException`** — CLAUDE-DESIGN.md Diagnostics section; factory-events.md log-events subsection.

### Developer Deliverables

Skill files use MarkdownSnippets + reference-app code; updating them is NOT my
scope. List for orchestrator routing:

- [ ] `skills/RemoteFactory/references/factory-events.md` — contains references
      to `IFactoryEventRelay.Register/Unregister`. Workflow: (1) rewrite the
      skill markdown to describe the new `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)`
      pattern; (2) if any code samples use MarkdownSnippets regions (`skill-*`),
      update source in `src/docs/reference-app/EmployeeManagement.Client.Blazor/Samples/Skill/`
      and run `mdsnippets`. Reason: skill must be self-contained and describe
      current API.
- [ ] `skills/RemoteFactory/references/trimming.md` — contains references to
      `FactoryEventRelayRegistry` and the per-handler PreserveType pattern.
      Rewrite to describe base-class `[DynamicallyAccessedMembers]` on
      `FactoryEventBase`. Reason: aligned with CLAUDE-DESIGN.md trimming
      paragraph + docs/trimming.md rewrite.

No source-code comment fixes required: I grepped `src/` for
`FactoryEventRelayRegistry|FactoryEventRelayDispatcher|DispatchRelayedEvents|Register(this)|Unregister(this)`
and the only remaining hit is
`src/Tests/RemoteFactory.IntegrationTests/Events/FactoryEventRelay/RelayTimingTests.cs:96`,
which is a historical comment describing the bug the test fixes — legitimate.

No `src/docs/reference-app/` updates required: grep confirmed no references to
the old surface there.

### Step 8 Part B Needed?

Yes — the general documentation deliverables are:

1. **Release notes** — new `docs/release-notes/vX.Y.Z.md` documenting the
   breaking change (items enumerated by reviewer in Step 6B section "Release
   notes — `docs/release-notes/`"):
   - Breaking: `IFactoryEventRelay` interface reshaped — `Register/Unregister`
     removed, `Relay(IReadOnlyList<FactoryEventBase>)` added.
   - Breaking: client-side instance-method `[FactoryEventHandler<T>]` handlers
     are no longer dispatched (NF0503 Warning emitted).
   - Breaking: `FactoryEventRelayRegistry` and `FactoryEventRelayDispatcher`
     public types removed.
   - New: `NoOpFactoryEventRelay` registered by default in Remote mode (TryAdd);
     logs Warning 3011 on first non-empty batch drop.
   - New: `[FactoryEvent]` attribute inherited from `FactoryEventBase` — drives
     `FactoryEventTypeRegistry`.
   - New: `UnknownFactoryEventTypeException` public type for wire-format
     type-resolution failures.
   - Fix: post-return ordering is now a hard guarantee (was previously racy).
   - Migration guide — table of removed vs replacement API.
2. **Skill updates** (listed as Developer Deliverables above) — orchestrator
   should route to the skill-author or developer agent.

No migration-guide page or architecture-docs changes identified beyond these.
