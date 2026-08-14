# TRIM-008 — Documentation Anchor Inventory (Step 9 working list)

**Built:** 2026-08-13, by reading the files rather than copying the plan's list.
**Why it exists:** plan review A4. This inventory has been wrong twice in this arc — once by *over-listing* (skill class-factory entries flagged as falsified when they described a leg that worked) and once by *under-listing* (abandoned TRIM-005 targeted `docs/trimming.md:36`, the true statement, while missing `:35`, the false one). Enumerating before editing is the correction.

---

## Inventory revision: the plan's "do NOT touch" list is now partly wrong

The plan's Files section lists these as *verified do-NOT-touch, class/interface-scoped, still true*:
`docs/trimming.md:25,27,36`, `skills/.../class-factory.md:318,333-334`, `advanced-patterns.md:227`.

**The 2026-08-13 pre-fix probe falsified the class-factory half of that.** Async generated `Local*` methods retain their server-only bodies (TRIM-009), so every anchor asserting that `[Remote] internal` class-factory bodies are trimmed is **false today** for any aggregate root with async operations — reads as well as writes, both measured 2026-08-13 — which is nearly all of them.

**Disposition: do not edit them in TRIM-008.** TRIM-009 makes them true again, and nothing ships until both land (AC6 held whole; deferred item 2 gates the release on both). Editing them to say "leaks" and reverting after TRIM-009 is churn that would also publish a scarier claim than the shipped state ever has.

**What makes that safe rather than a repeat of this arc's habit:** the anchors are enumerated below, the dependency is named, and the release is already blocked on TRIM-009. If TRIM-009 is ever abandoned or descoped, this table is the checklist that must be worked before any release.

### TRIM-009-dependent anchors (deferred, tracked, release-blocking)

| Anchor | Claim | Status |
|---|---|---|
| `docs/trimming.md:25` | `[Remote] internal` → "Method body trimmed" | False for async ops (read AND write — measured 2026-08-13) |
| `docs/trimming.md:27` | `internal` (no `[Remote]`) → "Method body trimmed. Server-only." | Same |
| `src/Design/CLAUDE-DESIGN.md:648,650` | Same visibility/guard table | Same |
| `src/Design/CLAUDE-DESIGN.md:653` | "`[Remote]` requires `internal` so the IL trimmer can remove method bodies from client assemblies" | Same |
| `skills/RemoteFactory/references/trimming.md:10` | "method bodies trimmed on client" | Same |
| `skills/RemoteFactory/references/trimming.md:11` | Child entity methods "removed from client" | Same |
| `skills/RemoteFactory/references/trimming.md:14` | "no server-only logic, no server-only dependencies, no IP exposure" | Same — the strongest claim in the set |
| `docs/client-server-architecture.md:133` | "removes server-only method bodies … and the decompilable business logic" | Same |
| `skills/RemoteFactory/references/trimming.md` — "What Gets Trimmed, By Factory Shape" table, class-factory row | **Added by TRIM-008.** Says class-factory bodies are removed "from v1.7.0 — synchronous operations were always removed; `async` ones needed the same release" | **True only once TRIM-009 lands.** This is the one place TRIM-008 wrote a forward-looking claim rather than deferring, because the table's whole purpose is a shape-by-shape guarantee and omitting the row would be its own silence. It is release-blocking: if TRIM-009 is descoped, this row must be rewritten before shipping. |

---

## Bucket 1 — Falsified by the defect, made true by TRIM-008 (edit now)

| Anchor | Problem |
|---|---|
| `docs/trimming.md:35` | Static factories: "The trimmer removes the registration lambdas and their captured dependencies." Was false — bodies were DAM-retained. True after the fix, but the stated mechanism omits why it now works. |
| `docs/trimming.md:13` | "method bodies, server-only types, and their transitive dependencies all disappear" — stated unconditionally for all shapes. |
| `src/Design/CLAUDE-DESIGN.md:763-766` | Attribute-target table: Static Factory row says `typeof({Namespace}.{StaticClassName})` — **the consumer's class**. Now factually wrong about emitted output, and it documents the defect as intended design. No relay-handler row at all. |
| `skills/RemoteFactory/references/trimming.md` (new section) | Skill never mentions that `[Execute]`/`[FactoryEventHandler<T>]` have their own preservation story. |

## Bucket 2 — Falsified TRIM-005 artifacts (4, not the 3 the deferred table listed)

| Anchor | Problem | Status |
|---|---|---|
| `.github/workflows/build.yml:109-112` | `(?<!I)` exemption + disproven rationale | **Done** (Step 8) |
| `src/Tests/RemoteFactory.TrimmingTests/README.md:29-31` | Same exemption, same disproven rationale | Edit |
| `src/Tests/RemoteFactory.TrimmingTests/TrimTestCommands.cs:34-36` | "would root the ctor from the **(retained, guarded-dead) method body**" — the body is no longer retained, so the stated reason is wrong even though the conclusion still holds | Edit |
| `src/Tests/RemoteFactory.TrimmingTests/ServerOnlyTypes.cs:4-6` | Says the interface *should be absent* — **this one was always right** and contradicted the other three. No edit; it is now corroborated by measurement. | No change |

## Bucket 3 — Silences (shapes documented not at all)

| Anchor | Gap |
|---|---|
| `skills/RemoteFactory/references/static-factory.md` | No trimming content |
| `docs/attributes-reference.md` — `[Execute]` | No trimming content |
| `ExecuteAttribute` XML doc | No trimming content |
| `FactoryEventHandlerAttribute<T>` XML doc | No trimming content |
| `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` — `ExampleCommands` | No trimming commentary despite being the Design source of truth for static factories |

## Bucket 4 — Accurate today, made inaccurate BY the fix

The bucket the first draft missed. These correctly describe "preserve all methods on the referenced type" — precisely the property the fix removes for two of four shapes.

| Anchor | Problem |
|---|---|
| `docs/trimming.md:222` | "instruct the trimmer to preserve all methods on the referenced type" — still true of the *mechanism*, but the referenced type is now a generated holder, and the sentence reads as though it is the factory. |
| `src/Design/CLAUDE-DESIGN.md:756` | "ensuring each factory type's `FactoryServiceRegistrar` method (and all other methods) survive trimming" — "and all other methods" is exactly the defect, stated as a feature. |

## Verified do-NOT-touch (still true after measurement)

| Anchor | Why it stands |
|---|---|
| `docs/trimming.md:26` | `public` non-`[Remote]` bodies survive — correct, and unaffected |
| `docs/trimming.md:36` | Interface factories unreachable to the trimmer — all iface markers absent, sync and async. **But the leg cannot in principle report on body elimination:** it reaches its implementation through the interface, so those markers are absent by fixture shape either way (deferred item 19 — the `[Service]` fix that would give it a reachable marker does not compile). Left standing because nothing contradicts it, not because it was measured. Corrected 2026-08-13; this row previously read "measured true", which the leg cannot deliver |
| `docs/trimming.md:42` | Feature-switch mechanism — accurate |
| `src/RemoteFactory/NeatooRuntime.cs:5-9` | Describes the switch, claims nothing about which shapes benefit |
