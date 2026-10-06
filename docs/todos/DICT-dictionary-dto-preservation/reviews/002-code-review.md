# DICT-002 code review

**Gate:** Step 5, code-reviewer, per-plan pass (opted in).

## Round 1 — 2026-10-06 — CLEAN, one callout

**Veto-tier:** none.

**Direction and shape.** `DtoTypeWalker.cs` is the only production file touched, and it is the single seam the three callers share. None of the callers changed.

- **Incremental generator:** no non-equatable state was added.
- **netstandard2.0 and Roslyn usage:** sound.
- **Determinism:** preserved.
- **Fields on entities:** walking them is justified, because Named format serializes them. A field-typed DTO passes the same candidate filter a property of that type would, so this roots no new kind of code.

**Regression walk, per shape:**

- **Unchanged:** `Task<T>`, `Node<T>`, nullable annotations, type parameters, error types, non-generic `class Dtos : List<Dto>`, and dedup.
- **Additions, all benign:** `List<List<Dto>>`, `Dto[][]`, and `List<MyStruct?>`.

### Triage

| # | Finding | Affects | Priority | Size | Disposition |
|---|---|---|---|---|---|
| 1 | A consumer generic collection nested in a list or dictionary swapped registrations. `List<PagedList<Dto>>` used to register `PagedList<Dto>` and now registered only `Dto`, which breaks the plan's no-regression constraint | AC-1 | Must | punch | **Fixed.** A collection that passes the DTO candidate check is kept as a candidate as well as unwrapped. Red-first at `c381e77` in `ConsumerGenericCollection_CollectionAndElementBothRegistered` ×3, which covers top-level, list element, and dictionary value. A top-level consumer collection is now registered too, which it never was before |

### Theoretical, not triaged but acted on where they were comments

- The cycle-and-cap branch comment claimed "unchanged" for both cases. It is now narrowed to the self-referential case.
- The file header said "every dictionary". It now says "every generic dictionary".
- `Dto[][]` and `List<MyStruct?>` are additions with no test pinning them. Not acted on.

### Reviewer read report

- **Beyond the brief:**
  - The `verify-trimmed.sh` diff against `main`.
  - The stat of `src/Tests` and `src/Design`.
  - A grep of `src` for consumer generic collections.
- **Named but unused:**
  - `CLAUDE.md`, `CLAUDE-DESIGN.md`, and `docs/trimming.md`. No candidate finding depended on a documented rule.
  - The two caller files.

## Round 2 — 2026-10-06 — CLEAN, no callouts

- **Callout 1 is fixed and verified.**
  - Both comment points are addressed.
  - The test edits only make DICT-002's own file stricter.
- **Adding the wrapper as a candidate changes nothing** for self-enumerables, framework collections, interfaces, abstract bases, `[Factory]` collections, arrays, `KeyValuePair`, or non-generic subclasses.
- **The only new registrations are concrete, non-System generic collections:**
  - at the top level, which is new;
  - as a list element, which restores the pre-DICT behaviour;
  - as a dictionary value, which is new.
- **The pathological depth-cap case** now emits about ten nested registrations instead of one. That is harmless.
- **No new category of type** can reach a trimmed client.
- **Theoretical, not triaged:** walking a registered collection's own members can preserve types the serializer never writes, because an `IEnumerable` is written as an array. The only cost is size, and this was already true for nested collections before DICT-002.
