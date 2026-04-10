# Requirements Documenter — Factory Event Relay

Last updated: 2026-04-09
Current step: Step 7A complete — requirements documentation updated.

## Key Context

This is an open-source library. The Design project is the authoritative source of truth, and Phase 6 (architect) already updated it:
- `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs` (new)
- `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` (updated to class attribute form)
- `src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs` (3 new tests)
- `src/Design/CLAUDE-DESIGN.md` — Pattern 4, Quick Decisions rows, Anti-Patterns 10 & 11 already added

Reviewer (Step 6B) verdict: REQUIREMENTS SATISFIED.

My Step 7A job was the minimal pass: verify reviewer findings are present and fill any remaining gaps in CLAUDE-DESIGN.md for business rules that were implicitly rather than explicitly documented.

## Mistakes to Avoid

(none yet)

## User Corrections

(none yet)

## Documentation Tracking

### Expected Deliverables

- CLAUDE-DESIGN.md must document the relay pattern comprehensively
- All Design.Domain / Design.Tests updates owned by Phase 6 (already complete)
- Published docs (`docs/*.md`) are Step 7B territory — NOT mine

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` | Pattern 4 "Key points" expanded: explicit mention of `RaiseOptions.ServerOnly` composition with other flags (rule 6), null-not-empty `RelayedEvents` (rule 9), strict ordering "result first, events after" (rule 12), WeakReference cleanup (rule 18), Logical mode no-registration (rule 7). | Reviewer noted these were implicit; now explicit in the quick reference. |
| `src/Design/CLAUDE-DESIGN.md` | Design Completeness Checklist: added two checked items for `FactoryEventHandlerPattern.cs` (server-side static handler) and `FactoryEventRelayPattern.cs` (client-side relay with Register/Unregister). | Keeps the completeness checklist in sync with Phase 6 additions. |
| `src/Design/CLAUDE-DESIGN.md` | Design Files table: added rows for `FactoryEventHandlerPattern.cs`, `FactoryEventRelayPattern.cs`, and `FactoryEventRelayTests.cs`. | Navigation aid for future agents/users. |
| `src/Design/CLAUDE-DESIGN.md` | Common Mistakes Summary: added items 11 and 12 mirroring Anti-Patterns 10 and 11 (raising events outside a factory method; stacking `[Factory]` on a handler class). | Keeps the summary list in sync with the full anti-pattern entries. |
| `src/Design/CLAUDE-DESIGN.md` | Front matter: bumped `design_version` to 1.3, `last_updated` to 2026-04-09. | Reflect the relay feature addition. |

Already present (from Phase 6, verified):
- Pattern 4: Factory Event Handler (Mediator + Client Relay) section with event type, server-side raiser, server-side handler, client-side relay handler examples
- Quick Decisions Table rows: client-side handler, server-side handler, `[Factory]` requirement, ServerOnly flag, multiple event types per class
- Anti-Pattern 10: Raising Factory Events Outside a Factory Method
- Anti-Pattern 11: Decorating a `[FactoryEventHandler<T>]` Class with `[Factory]`

### Developer Deliverables

None. Phase 6 completed all source code changes to the Design project. Reviewer confirmed:
- 506 / 538 / 47 tests passing per TFM
- No source code gaps
- No skill updates required in this step (skills are a separate workflow via `mdsnippets`)

### Step 9 Part B Needed?

**Yes.** The reviewer flagged a pre-existing documentation gap: `docs/*.md` (Jekyll-published docs) does not document the factory events mediator at all (the parent feature from commit `1750f52` shipped without updating docs). The relay feature inherits this gap. Recommended Step 7B work:

- Add `docs/factory-events.md` (or extend `docs/events.md`) covering:
  - `IFactoryEvents` injection and `Raise()` semantics
  - `[FactoryEventHandler<T>]` class attribute (static → server, instance → client relay)
  - `RaiseOptions` flags including `ServerOnly`
  - NF0501 / NF0502 diagnostics
  - `IFactoryEventRelay.Register` / `Unregister` lifecycle for client viewmodels
- Update `docs/attributes-reference.md` with `[FactoryEventHandler<T>]` row
- Update `docs/trimming.md` to mention `RelayedFactoryEvent` transport type (optional)
- Release notes entry for the feature in `docs/release-notes/` (version bump needed)

These are all documentation-only changes the documenter (Step 7B) can make directly without developer involvement.
