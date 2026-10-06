# TRIM-007 Code Review (Step 5, opt-in) — 2026-07-13

**Reviewer:** code-reviewer agent, findings-only (no grade). Range `6c892e9..59302c7`. Logs grepped, not re-run.

**Result: 3 veto-tier findings — all doc/prose corrections, all fixed immediately post-review.** The generator work itself was verified clean.

## Verified (generator surface)

- Fourth pipeline branch shape correct: cheapest viable predicate; `WalkDtoGraph` runs only after the base-chain match and accessibility gate (non-event records pay only `GetDeclaredSymbol` + short base walk); no `ISymbol`/`Compilation` leaks into pipeline output; nulls filtered before `Collect()`.
- Plan-review compliance complete: B1 gate walks the full containing chain, rejects `IsFileLocal`, admits `ProtectedOrInternal`, excludes `private protected` — proven by the solution build against UnitTests' private nested event records; B2 FQN match + decoy test; B3 predicate; B4 `SortedSet(Ordinal)` + determinism test; B5 `SanitizeNamespace` edges; A1/A2 doc scope honored where anchored.
- `ExceptWith` defensive step honest; unused registrar parameters match the reflection-invoked contract; no reflection added generator-side; sacred tests intact (comment-only edits faithful; smoke-test edit strengthens coverage).
- Logs: build 0 errors (3 non-production warnings: pre-existing harness CA1062 + 2 WASM workload); 595+595 / 561+561, 0 failed; trimmed harness exit 0.

## Veto-tier findings → fixed

The plan's Acceptance bullet 4 ("no surviving overpromise about inherited DAM") was not met — three authoritative artifacts outside the anchor list still carried the falsified claim:

1. `Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs:13-16,:63-65` (Design source of truth) — **fixed**: preservation now attributed to the generated registrar; runtime-discovery half of `[FactoryEvent]` retained.
2. `docs/factory-events.md:264` (published mirror of the rewritten trimming.md section) — **fixed**; verification pointer updated to `EventSubscribeOnlySmokeTest`.
3. `EventSubscribeOnlySmokeTest.cs:62-68` class summary (self-contradiction within a file this plan edited) — **fixed**.

## Callout-tier findings → disposition

- Skill docs (`skills/RemoteFactory/references/trimming.md:179,239`, `factory-events.md:327`) carried the same overpromise plus the now-false nested-manual-preservation note — **fixed in the same pass** (hand-written prose, not mdsnippets-managed).
- `FactoryEventTypeRegistry.cs:99` IL2026 suppression Justification cited the disproven rationale — **fixed** (suppression itself remains valid).
- `Program.cs:124` harness comment stale framing — **fixed**.
- Hint name uses the raw assembly name while the namespace is sanitized — **accepted-with-reason** (hint names tolerate most characters; only the namespace affects compilation).
- Theoretical CS0101 if a consumer declares `{RootNamespace}.NeatooEventPreservationRegistrar` — **accepted-with-reason** (Neatoo-prefixed name, single emission, consistent with factory-registrar conventions; no diagnostic warranted).
- Pre-existing harness CA1062 — informational, not this plan's regression.

Post-fix sanity build: 0 errors.
