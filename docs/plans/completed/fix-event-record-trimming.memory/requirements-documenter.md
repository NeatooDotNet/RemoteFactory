# Requirements Documenter — Fix Event Record Trimming

Last updated: 2026-04-13
Current step: Step 7 Part A — COMPLETE

## Key Context

- Plan: `docs/plans/fix-event-record-trimming.md`. Verified by architect (6A) and requirements-reviewer (6B).
- Implementation adds `DtoConstructorRegistry.PreserveType<T>()` and extends the generator to emit preservation calls for every `[FactoryEventHandler<T>]` event type plus recursively-reachable nested records/DTOs, emitted UNCONDITIONALLY (no `IsServerRuntime` guard).
- Three preservation layers: (1) registry-based (Primary), (2) `Raise<T>` call-site annotation (Secondary), (3) `RegisterHandler<TEvent>` call-site annotation (Supplemental).
- Known limitation: `Dictionary<K,V>` value types are NOT walked. Documented gap with workaround.
- Potential user-visible `IL2091` warning when user forwards `Raise<T>` through their own generic passthrough without annotating their `T` with `[DynamicallyAccessedMembers(All)]`.
- Version bumped from v1.1.0 to v1.2.0 (non-breaking bug fix + additive new public API).

## Mistakes to Avoid

(First run — none.)

## User Corrections

(First run — none.)

## Documentation Tracking

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `docs/trimming.md` | Added "Factory Event Type Preservation" subsection covering: automatic preservation for every `[FactoryEventHandler<T>]` event type + nested records; `PreserveType<T>` vs `Register<T>` distinction table; `Dictionary<K,V>` value-type gap with three-option workaround; `IL2091` callout for user code that forwards `Raise<T>` through a generic wrapper with two resolution options | Rules 1-9 in plan; reviewer item #1 |
| `docs/factory-events.md` | Added short "IL Trimming and Event Records" subsection inside Serialization & Transport that cross-links to `trimming.md#factory-event-type-preservation` and summarizes three-layer preservation | Reviewer item #2 |
| `skills/RemoteFactory/references/trimming.md` | Added "DTO and Event Record Preservation" section covering both factory-return DTO preservation (was missing entirely from the skill) and factory event preservation, `PreserveType<T>` vs `Register<T>` table, `Dictionary<K,V>` gap with workaround, `IL2091` user-code callout with two resolution options. Self-contained — no cross-refs to .cs or docs/ files; only references its sibling skill references | Reviewer item #3 |
| `skills/RemoteFactory/references/factory-events.md` | Added "IL Trimming" section summarizing three-layer preservation and cross-linking to `references/trimming.md` | Reviewer item #4 |
| `src/Design/CLAUDE-DESIGN.md` | Appended to DTO Constructor Registry subsection: "Factory event type preservation" paragraph describing the automatic `PreserveType<T>` emission per `[FactoryEventHandler<T>]`, recursive nested-record walk, unconditional emission, and the three-layer call-site annotation strategy. Appended "Known gap" paragraph documenting the `Dictionary<K,V>` limitation with workaround. Added Design Completeness Checklist entry for the new `OrderShippedEvent` + `ShippingAddress` demonstration | Rules 1-9; reviewer item #5 |
| `docs/release-notes/v1.2.0.md` | NEW FILE. Full release notes: Overview, What's New (`PreserveType<T>` primitive, automatic preservation, three-layer strategy, preservation-for-diagnostic-classes), Breaking Changes section stating structurally none + `IL2091` potential warning, Bug Fixes (event records fail to round-trip under trimming), Migration Guide with `IL2091` fix and `Dictionary<K,V>` workaround, Links, Commits | Reviewer item v1.2.0 release notes |
| `docs/release-notes/index.md` | Added v1.2.0 entry to Highlights table (top) and All Releases list (top); did not modify earlier entries | Release notes maintenance per CLAUDE.md |
| `docs/release-notes/v1.1.0.md` | Bumped `nav_order: 1` → `nav_order: 2` to make v1.2.0 the newest at `nav_order: 1` | Release notes nav_order rule in CLAUDE.md |
| `src/Directory.Build.props` | Bumped `<FileVersion>` and `<PackageVersion>` `1.1.0` → `1.2.0` | Release version bump per CLAUDE.md |

### Files Verified (already updated by orchestrator — no further change needed)

- `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` — already includes `OrderShippedEvent` + `ShippingAddress` + `OrderShippedHandlers` + a comment block explicitly documenting automatic preservation AND the `Dictionary<K,V>` gap with workaround (lines 119-142). No additional edits needed.

### Developer Deliverables

**None.** All documentation-layer changes are in markdown files that this agent may edit directly. The Design project code was already updated upstream of this step. No source code changes (XML doc comments, test additions, reference-app code) were identified as required to make the documentation complete and accurate.

Two items to note (not deliverables, just observations for the orchestrator):

1. **XML doc comments on new public API.** `DtoConstructorRegistry.PreserveType<T>()` SHOULD carry an XML `<summary>` on the source file (`src/RemoteFactory/Internal/DtoConstructorRegistry.cs`). If not already present in the implementation, consider adding one that mirrors the description in release notes. The plan file at line 106-118 shows a suggested comment block. **Not verified in this step** — the agent could not access the source file to confirm. Recommend orchestrator spot-check.
2. **MarkdownSnippets (`mdsnippets`) integration.** None of the code blocks added to `skills/RemoteFactory/references/trimming.md` or `skills/RemoteFactory/references/factory-events.md` use `#region skill-*` extraction. They are all hand-written illustrative snippets (anti-pattern code and short API usage), consistent with the "Partial/illustrative" and "Anti-pattern" categories in `CLAUDE.md`'s skill-code-blocks table. `mdsnippets` does not need to be run.

### Step 8 Part B Needed?

**No.** Release notes are already handled in this Part A (v1.2.0 is required because the plan introduced a new public API). No README updates, migration guide separate from release notes, or architecture doc changes are needed — all the user-facing behavioral changes are captured in `docs/trimming.md`, `docs/factory-events.md`, and `docs/release-notes/v1.2.0.md`. The skill is self-contained and updated in-place.

Step 8 Part B can be skipped.
