# Docs Writer — factory-events-relay-redesign

Last updated: 2026-04-14
Current step: Step 7 Part B — release notes + index updated. Plan status not advanced (orchestrator owns status updates; skills update still pending, handled separately).

## Documentation Tracking

### Files Updated
| File | What Changed |
|------|-------------|
| `docs/release-notes/index.md` | Added v1.4.0 row to Highlights table; added v1.4.0 entry at top of All Releases list. |
| `docs/release-notes/v1.3.0.md` | nav_order: 1 -> 2 |
| `docs/release-notes/v1.2.0.md` | nav_order: 2 -> 3 |
| `docs/release-notes/v1.1.0.md` | nav_order: 3 -> 4 |
| `docs/release-notes/v1.0.0.md` | nav_order: 4 -> 5 |

### Files Created
| File | Purpose |
|------|---------|
| `docs/release-notes/v1.4.0.md` | v1.4.0 release notes — factory event relay redesign + post-return ordering bug fix. nav_order: 1. |

### Deliverables Skipped (N/A)
- Skill updates (`skills/RemoteFactory/`) — explicitly out of scope; orchestrator handles separately.
- `src/Directory.Build.props` version bump — user's call at release-tagging time.
- API reference docs (`docs/factory-events.md` etc.) — already updated by Step 7 Part A (requirements documenter).

### Notes / Quirks
- No git tag `v1.3.0` exists locally, so `git log v1.3.0..HEAD` could not be run. Implementation commits for 1.4.0 haven't landed yet either (Step 7 precedes Step 8 commit). Release-notes "Commits" section uses conventional-commit intent bullets matching the plan's file changes, not literal SHAs. If orchestrator wants SHAs after Step 8 lands, the section can be updated then.
- Pre-existing v0.x nav_order overlap (many sit at 2) intentionally out of scope per commit d8369b3's precedent. Only the 1.x tier was bumped.
- Noted the minor-bump-with-breaking-change framing explicitly in the release notes intro (per user's instruction on the plan).
