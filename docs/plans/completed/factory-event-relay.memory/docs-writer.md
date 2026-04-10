# Docs Writer — Factory Event Relay

Last updated: 2026-04-09
Current step: Step 7B complete — Documentation Complete

## Documentation Tracking

### Files Created

| File | Purpose |
|------|---------|
| `docs/factory-events.md` | New comprehensive guide for the `[FactoryEventHandler<T>]` mediator + client relay pattern. Covers server/client handlers, `IFactoryEvents.Raise`, `RaiseOptions` flags (None/AwaitRemote/ContinueOnFail/ServerOnly), data flow, serialization transport (`RemoteResponseDto.RelayedEvents` + `RelayedFactoryEvent`), diagnostics NF0501/NF0502, DI registration matrix, and a complete example. |
| `docs/release-notes/v0.24.0.md` | Release notes for the v0.24.0 minor version bump. Covers both the mediator feature (from commit `1750f52`, previously undocumented in `docs/*.md`) and the client relay feature added in this plan. `nav_order: 1`. |
| `skills/RemoteFactory/references/factory-events.md` | New self-contained skill reference for the factory events pattern. All code is hand-written inline (no mdsnippets placeholders). Distinguishes from the `[Event]` method attribute and includes anti-patterns, diagnostics, and a decision table. |

### Files Updated

| File | What Changed |
|------|-------------|
| `docs/attributes-reference.md` | Added `[FactoryEventHandler<T>]` row to the Quick Lookup table; added a full section between `[Event]` and Execution Control covering both static (server) and instance (client relay) method signatures, multiple-attribute stacking, and generator behavior; added row to Attribute Inheritance table; added cross-reference to `factory-events.md` in Next Steps. |
| `docs/interfaces-reference.md` | Added new `## Factory Events` section before `## Factory Core` with entries for `IFactoryEvents` and `IFactoryEventRelay`; added both interfaces to the Summary table. |
| `docs/events.md` | Added a prominent cross-reference at the top distinguishing the `[Event]` method attribute (isolated-scope fire-and-forget) from the `[FactoryEventHandler<T>]` class attribute (mediator + relay); added link to `factory-events.md` in Next Steps. |
| `docs/release-notes/index.md` | Added v0.24.0 row to the Highlights table; added v0.24.0 and v0.23.0 entries to the All Releases list (v0.23.0 had been missing from that list). |
| `docs/release-notes/v0.23.0.md` | Bumped `nav_order` from 1 to 2 to make room for v0.24.0. |
| `skills/RemoteFactory/SKILL.md` | Added keywords to the description (`[FactoryEventHandler<T>]`, `FactoryEventBase`, `IFactoryEvents`, `IFactoryEventRelay`, "factory event relay", "ServerOnly", "RaiseOptions"); added 5 rows to the Quick Decisions Reference table covering factory events; added the new `references/factory-events.md` file to the Core Patterns list with a description. |
| `skills/RemoteFactory/references/static-factory.md` | Added a cross-reference callout in the Event Handlers section distinguishing the `[Event]` method attribute from `[FactoryEventHandler<T>]`. |

### Deliverables Skipped (N/A)

- **mdsnippets extraction**: The plan intentionally called for inline code in new files. The skill file is fully self-contained per the CLAUDE.md constraint (only markdown, no `.cs` references). The published docs file uses inline examples because the reference application has not been updated with factory-event samples; if/when skill code samples are added to `src/docs/reference-app/`, a follow-up task could migrate `docs/factory-events.md` to MarkdownSnippets regions.
- **`docs/client-server-architecture.md` update**: The existing page describes the client/server boundary at a high level. Factory events are an orthogonal concern that fits better in its own page; no changes needed on that page.
- **New diagnostic pages** (`docs/diagnostics/NF0501.md`, `NF0502.md`): The existing `docs/` site does not have a `diagnostics/` directory with per-diagnostic pages; the v0.6.0 release notes reference such pages but they don't exist. Kept consistent with current practice — diagnostics documented inline in `factory-events.md` and the release notes.

## Cross-Agent Notes (from spawn prompt)

- Step 6B (requirements verification) flagged that `docs/*.md` had zero coverage of factory events (pre-existing gap from the mediator feature in commit 1750f52). This has now been filled by `docs/factory-events.md` plus the attributes/interfaces cross-references and the v0.24.0 release notes that call out the mediator origin commit explicitly.
- The v0.24.0 release notes cover BOTH the previously-undocumented mediator (`1750f52`) and the new relay (`1bbedb4`, `3850fd7`), since the mediator never shipped in a tagged release.

## Concerns / Gaps

1. **Reference-app code samples**: `docs/factory-events.md` uses inline code blocks rather than MarkdownSnippets regions. A future task should add factory event samples to `src/docs/reference-app/EmployeeManagement.Domain/Samples/` and migrate the examples so they're tested by the reference app build.
2. **Diagnostics pages**: No `docs/diagnostics/NF0501.md` or `NF0502.md` pages were created, consistent with the current state of the site (no diagnostics directory). If the project starts adding per-diagnostic pages later, these two should be added.
3. **`docs/factory-modes.md` / `decision-guide.md`**: Not updated. Neither page directly references events today, and the factory-events page has its own decision table. No gap, but worth revisiting if either page grows a "what about events?" question.
