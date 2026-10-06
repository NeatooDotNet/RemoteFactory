# TRIM-001 Code Review (Step 5, opt-in) — 2026-07-06

**Reviewer:** code-reviewer agent, findings-only (no grade). Range `3752405..4151dc2` (bed0651 feat + 4151dc2 gate closure). Logs: `001-build.log`, `001-test.log` (grepped, not re-run).

**Result: no veto-tier findings.** Deliverable landed cleanly, shape verified correct.

## Verified

- **Plan-review B1:** `DtoPreserveTypes` is `EquatableArray<string>` on all three transform-output records (`TypeInfo`, `MethodInfo`, `TypeFactoryMethodInfo`); models correctly relax to `IReadOnlyList` (not cache keys).
- **Plan-review B2:** `WalkDtoGraph` buckets roots by ctor shape; `WalkEventRoot`'s root-always-Preserve rule fully retired, zero references remain; stale header comment fixed.
- **Emission placement:** all three registrars emit `PreserveType<T>()` unguarded alongside `Register<T>()` — matching Register's client/server-agnostic placement; no fourth site exists.
- **Semantics preserved:** rejection (structure or no-public-ctor) happens before `visited.Add`, matching prior behavior; the Register path is behaviorally identical to old `WalkFactoryReturn`; the new walk is a strict superset (records now descend).
- **Runtime parity:** Preserve bucket rule exactly matches `RecordBypassConverterFactory.CanConvert`; `PreserveType` deliberately does not populate the ctor registry.
- **Repo rules:** no reflection added; sacred tests untouched (additive-only harness changes); no DDD tutorial prose; build 0 errors (2 pre-existing WASM workload warnings); 2276 tests, 0 failed.
- **Plan-review A1 doc coherence:** no surviving sentence implies `PreserveType` is emitted nowhere; event-path removal sentence stays correctly scoped; CLAUDE-DESIGN/trimming.md/AllPatterns accurate to shipped behavior.

## Callout-tier findings

1. **Pre-existing:** `IsDtoStructureCandidate` excludes by `ns.StartsWith("System")` — a prefix match, so a consumer namespace like `Systems.Domain` would be silently excluded from both buckets (`DtoTypeWalker.cs:97`). Low confidence / negligible likelihood; unchanged by this plan (Constraints preserved exclusions intact). **Disposition:** routed to TRIM-002's draft-time scope — that plan already reworks the candidate checks at this exact seam (tighten to `ns == "System" || ns.StartsWith("System.")`).
2. **For the record only:** `record struct` trimmed round-trip has emission-side coverage only — already ACCEPTED-WITH-REASON at the test gate; no action.
