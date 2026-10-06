# TRIM-005 — Plan Review

**Date:** 2026-08-11
**Plan:** [../plans/005-server-only-reference-over-retention.md](../plans/005-server-only-reference-over-retention.md)
**Stage:** Step 2 (pre-implementation; branch `TRIM-005-server-only-guard-shape` sat on the draft commit only)
**Verdict:** **REJECTED — direction**

The plan's craft was sound: intent-level Scope/Steps, no transcription smell, line numbers confined to Current State, red-pin-first discipline, and the causal hypothesis correctly hedged as "prime suspect, to be confirmed at Step 2." The rejection is entirely about **which seam the defect lives at**. The user's 2026-08-11 direction choice (generator fix, not docs correction) survives intact — a generator fix is still available and still right. Only the seam moves.

---

## The falsification

TRIM-005 inherited its diagnosis verbatim from TRIM-004 Plan Amendment 3 / Discovery Log 2026-07-06: *guarded-dead bodies survive ILLink folding because the early-`throw` shape plus the `try`/`catch` region defeats unreachable-code elimination.*

The reviewer probed the publish-trimmed artifact at HEAD (`src/Tests/RemoteFactory.TrimmingTests/bin/Release/net9.0/win-x64/publish/RemoteFactory.TrimmingTests.dll`, built 2026-07-13 13:26, matching `007-publish.log`). **Orchestrator re-ran the probe independently on 2026-08-11 and confirms it:**

| Symbol | Sole reachability | In trimmed DLL |
|---|---|---|
| `ICorrelationContext` | class-factory guarded body | **absent** |
| `IFactoryOnStart` | class-factory guarded body | **absent** |
| `IFactoryOnComplete` | class-factory guarded body | **absent** |
| `Stopwatch` | class-factory guarded body | **absent** |
| `FactoryOperation` | class-factory guarded body | **absent** |
| `LogInformation` | class-factory guarded body | **absent** |
| `_DoWork` | `[Remote][Execute] private static` | **PRESENT** |
| `_ProcessRecord` | `[Remote][Execute] private static` | **PRESENT** |
| `IServerOnlyRepository` | the over-retention | **PRESENT** |
| `DoServerWork` | the over-retention | **PRESENT** |
| `ServerOnlyDirect` | implementation type | absent |
| `ServerOnlyHelper` | implementation type | absent |

**ILLink already fully eliminates the class-factory guarded body at HEAD — exception-handling region and all.** The early-`throw` shape works. The plan's named blocker is not operative, and Steps 3, 4, 5, 7 target a non-defect.

## The actual mechanism

Confirmed by orchestrator against source on 2026-08-11:

- `StaticFactoryRenderer.cs:41` emits `[assembly: NeatooFactoryRegistrar(typeof(global::{Namespace}.{TypeName}))]` — pointing at **the consumer's own `[Factory]` class**.
- `RelayHandlerRenderer.cs:32` does the same for `[FactoryEventHandler<T>]` classes.
- Contrast `ClassFactoryRenderer.cs:54` and `InterfaceFactoryRenderer.cs:48`, which point at the **generated** `{X}Factory` type — safe.
- `FactoryAttributes.cs:157-168` puts `DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)` on both the ctor parameter and the `Type` property.

DAM has no per-method granularity. It exists solely to satisfy one reflective lookup — `AddRemoteFactoryServices.cs:168`, `attr.Type.GetMethod("FactoryServiceRegistrar", Static|NonPublic|Public)` — but by its own contract ILLink must retain **every** method on the target type, bodies included. `TrimTestCommands._DoWork` / `._ProcessRecord` (`TrimTestCommands.cs:24-43`) are `[Remote][Execute] private static` methods whose bodies call `repo.DoServerWork(...)`. That is what holds `IServerOnlyRepository` and `DoServerWork`.

**Severity is higher than TRIM-005 assumed.** For `[Execute]` static factories and `[FactoryEventHandler<T>]` classes, `[Remote]` method **bodies ship to the browser**, DAM-preserved and decompilable — not merely "implementations gone, names linger." That contradicts `docs/trimming.md:7`, which sells IP protection as one of the three problems RemoteFactory solves. The plan's Notes de-rated the work as "bundle size, IP surface, and documentation accuracy — not a correctness fix"; that bar was set against the wrong severity.

---

## Veto-tier findings

**A1 — Step 8's doc anchors correct sentences that were already true, and miss the false ones.**
The plan anchors on `docs/trimming.md:36` and `:42`. Both are **true as written** and need no edit. The false statements are `docs/trimming.md:222` (registrar DAM "preserve all methods"), `:35`, `:13`, `:7`, and `src/Design/CLAUDE-DESIGN.md:756`. Repo `CLAUDE.md` designates the Design projects as the requirements-verification surface, so `CLAUDE-DESIGN.md:756` must be owned by whatever plan lands here. Shipping the plan's anchor list would leave every falsified sentence standing — the exact failure TRIM-007's code review caught three times (`007-code-review.md`).

**B1 — Causal hypothesis falsified; Steps 3, 4, 5, 7 and Acceptance bullet 3 target a non-defect.**
Reshaping the guard across all renderers would churn the generated-code contract for every consumer factory method (808 guard emissions across 277 generated files in the local build tree) and Step 1's red pin would **stay red**, because the retention is held by `_DoWork`/`_ProcessRecord`, not by any guarded body. Step 6 (tighten the CI grep) becomes unreachable.

**B2 — Framework Alignment fences the fix out of the seam where the defect lives.**
"The fix stays entirely inside the generator's renderers" admits the registrar-attribute **target type** change (renderer-side, `StaticFactoryRenderer.cs:41` / `RelayHandlerRenderer.cs:32`) but excludes the DAM breadth at `FactoryAttributes.cs:157-168` and the reflective lookup at `AddRemoteFactoryServices.cs:168` — library code and a documented public-attribute contract. An implementer honoring the Constraint literally could reach "infeasible" and trigger the Abandon fork while a feasible fix sits one file away.

**B3 — The Notes' outcome fork is mis-specified, so the plan has no correct exit.**
The fork models "ILLink cannot eliminate the guarded region." Step 2 proves the opposite — it can and does — which is neither branch. The most available misreading ("already eliminated → nothing to fix → abandon") would close TRIM-005 while leaving the real defect unqueued.

**B4 — Step 4 mischaracterizes two of four renderers, and one Constraint is false for them.**
`StaticFactoryRenderer.cs:182` and `RelayHandlerRenderer.cs:82` use the **inverse** polarity — `if (NeatooRuntime.IsServerRuntime) { … }` around a *registration*, with no `InvalidOperationException` anywhere. So the Constraint "invoking a guarded factory method in a non-server runtime still throws `InvalidOperationException`" is **not true today** for static factories (the local delegate is simply never registered) or relay handlers. Following Step 4 literally would introduce a throw into a registration path that has never thrown, with no test pinning the old behavior.

## Callout-tier findings

- **A2** — `docs/trimming.md:7`'s IP-protection claim is a materially stronger overpromise than the plan's framing acknowledged (see Severity above).
- **A3** — `skills/RemoteFactory/references/class-factory.md:318,333-334` and `advanced-patterns.md:227` repeat the trimmable-body claim and are absent from Step 8's anchors. Repo `CLAUDE.md` requires the skill to be self-contained and distributable separately. Same skill-reference gap TRIM-007's review raised.
- **B5** — Pre-flight guard inventory undercounts. Current State names three class-factory sites; there are **five** (add `ClassFactoryRenderer.cs:1045-1052` `RenderSaveLocalMethod` and `:1320-1327` `RenderCanLocalMethod`). Total across renderers is **eight**, not four. `:1052`'s trailing `AppendLine()` sits outside the `if` — a real emission asymmetry.
- **B6** — Framework Alignment's guard-placement claim is wrong for interface factories: `InterfaceFactoryRenderer.cs:261` emits the guard **unconditionally**, with no `IsInternal || IsRemote` test. The stated invariant doesn't describe the code it claims to freeze.
- **B7** — "Rooted client-side by delegate registration" is incomplete: `LocalCreate` is rooted three ways, dominantly by the registrar DAM on the generated factory type. Not load-bearing for the corrected direction, but would mislead a rooting-based remedy.
- **B8** — Acceptance bullet 2's `[integration]` tier is **not achievable**. There is zero `AppContext.SetSwitch` anywhere in the repo; `NeatooRuntime.IsServerRuntime` reads a process-wide unscoped switch, and `ClientServerContainers.Scopes()` simulates the split by *serialization*, not by flipping it. An in-proc test would mutate global state every parallel test reads — in a suite this todo has already documented as parallel-load flaky. The signal is cleanly observable in the trimmed harness (the only process running with the switch false), which can resolve `TrimTestEntityFactory` concretely. **Corollary: nothing in the repo currently pins the throw at all**, so CI would not catch a regression that deleted it.
- **B9** — Acceptance bullet 1 is under-determined by the harness: no interface-factory target, no `[Execute]`-on-class target, no save/write or `Can*` target, and — most relevant under the corrected diagnosis — no relay-handler target touching a server-only service. The `RelayHandlerRenderer.cs:32` leg would ship fixed but unverified.
- **B10** — Expected-emission tests are shape-insensitive substring checks (16 across `InternalVisibilityTests.cs` and `CanMethodVisibilityTests.cs`), **but** their blocks are sliced with naive `IndexOf` arithmetic delimited by the next method name. Any emission change that reorders or inserts members silently mis-slices and the `DoesNotContain` assertions pass **vacuously** — the same false-green class TRIM-001's test gate caught.
- **Index callout** — the Notes' contingent "successor docs-correction plan" has no Index stub. Conditional, and likely moot under this review.

## Verified plan claims (no finding)

- *"Incremental caching unaffected — render output, not transform-output records."* **Confirmed.** `FactoryGenerator.cs:31-51`, `:63-76` run both `Build` and `Render` inside `RegisterSourceOutput`; the cache key is transform output only. Holds for the corrected direction too. No TRIM-006 collision.
- *Feature-switch plumbing is sound.* `NeatooRuntime.cs:12-16` is a genuine `[FeatureSwitchDefinition]`; the harness sets it via `RuntimeHostConfigurationOption … Trim="true"` under `TrimMode=full`. Substitution and folding demonstrably work.
- Generated `.g.cs` files are **not** git-tracked — no committed-artifact churn.

---

## Advisory remedy (reviewer's, explicitly not prescriptive)

One shape that keeps the reflective lookup working while removing the over-preservation: emit `FactoryServiceRegistrar` onto a **nested generated type** inside the user's partial class and point the attribute at the nested type. Nested types can still reach the outer class's private members, and DAM on a type does not cover its nested types — mirroring the TRIM-007 per-assembly-registrar precedent (`EventPreservationRenderer.cs:71`). Whether that, a per-assembly shim, or a narrower discovery mechanism is right is a keyboard/user call and carries its own risks (factory-preservation regressions, naming/accessibility edges) belonging in a fresh Current State walk.

## Process note

TRIM-005 inherited its diagnosis verbatim from a TRIM-004 Plan Amendment without re-verification — the same failure mode TRIM-003 was created to break ("verifies this with a trimmed repro rather than assuming it", `todo.md:20`), which came back RED. **An amendment-sourced diagnosis carries the same "verify, don't inherit" obligation as a plan-sourced one.** Routed to the close-out retro.
