# Requirements Documenter — CanSave Target Parameter

Last updated: 2026-04-06
Current step: Requirements Documented

## Key Context

- This plan reverses a documented design decision: target params no longer suppress CanSave generation
- CanSave now gets two overloads when target-param auth exists: parameterless (non-target auth only) and target (all auth)
- CanInsert/CanUpdate/CanDelete remain suppressed for target-param auth (unchanged)
- Three documentation files were stale and updated directly
- One skill file is stale but cannot be edited directly (Developer Deliverable)

## Mistakes to Avoid

- Do not remove mention of suppression entirely -- CanInsert/CanUpdate/CanDelete are still suppressed
- The skill file (`skills/RemoteFactory/references/advanced-patterns.md`) uses MarkdownSnippets for some content but the stale line (98) is hand-written text, not a snippet

## User Corrections

None.

## Documentation Tracking

### Expected Deliverables

From reviewer findings:
1. CLAUDE-DESIGN.md Quick Decisions Table -- update target entity and CanSave rows
2. docs/authorization.md -- update "CanXxx Suppression" section
3. docs/interfaces-reference.md -- update IFactorySave<T> definition

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` | Updated Quick Decisions Table lines 161-162: target entity row now says "suppresses CanInsert/CanUpdate/CanDelete but CanSave gets two overloads"; CanSave row changed from "Why is CanSave missing" to "How does CanSave work with target-param auth" explaining the two overloads | Plan reverses CanSave suppression; old text was misleading |
| `docs/authorization.md` | Renamed section from "CanXxx Suppression" to "CanXxx Generation with Target Parameters"; updated to explain CanSave is the exception with two overloads; added code example; kept CanInsert/CanUpdate/CanDelete suppression documented | Plan Rules 1-5: CanSave IS now generated with target-param auth |
| `docs/interfaces-reference.md` | Updated IFactorySave<T> code block to show both CanSave overloads; added description of each overload's behavior | Plan Rules 6-7: IFactorySave<T> now has CanSave(CancellationToken) and CanSave(T target, CancellationToken) |

### Developer Deliverables

- [ ] `skills/RemoteFactory/references/advanced-patterns.md` line 98: Update hand-written text from "CanInsert/CanUpdate/CanDelete/CanSave are **not generated**" to "CanInsert/CanUpdate/CanDelete are **not generated**. CanSave is the exception: two overloads are generated -- CanSave() runs non-target auth only, CanSave(target) runs all auth." This is hand-written markdown (not a MarkdownSnippets snippet), so edit directly. No mdsnippets run needed for this specific change, but skill files are outside the editable scope for the requirements documenter.

### Step 8 Part B Needed?

No general documentation deliverables identified beyond the skill file Developer Deliverable above. Release notes are not needed for this change (internal behavioral improvement, not a version release). Step 8 Part B can be skipped unless the orchestrator wants the skill file update treated as a general documentation deliverable.
