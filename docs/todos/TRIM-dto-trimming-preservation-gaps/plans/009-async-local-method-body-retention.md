# TRIM-009 — Async `Local*` factory-method body retention

**Plan #:** 009
**Date:** 2026-08-13
**Related Todo:** [../todo.md](../todo.md)
**Status:** Drafted — awaiting plan review
**Last Updated:** 2026-08-14
**Plan-review opt-in:** Yes (same grounds as TRIM-008 — a false IP-protection guarantee, and the remedy is unknown at stub time)
**Code-review opt-in:** Yes (behavior-changing generator work, if the remedy turns out to be generator-side)

> **Promoted from Stub 2026-08-14.** The stub's declared first step — separate H1 from H2 — has been **run and answered** before any design work, and it also **falsified the remedy the stub predicted**. See [Separation experiment](#the-separation-experiment-2026-08-14) and [Approach](#approach). Everything above that section is the stub's original text, preserved.

---

## Scope

Make the class-factory leg deliver RemoteFactory's IP-protection guarantee for **async** factory operations. Today it does not: `[Remote]` method bodies reached from an `async` generated `Local*` method — and with them their `[Service]` interfaces, their called member names, and their string literals — survive on a publish-trimmed client and are decompilable.

This is the shape `docs/trimming.md`, the distributable skill, and `CLAUDE-DESIGN.md` all present as the one that trims correctly, so the doc surface is in scope alongside whatever the code fix turns out to be.

**Not** in scope: the registrar-DAM defect on `[Execute]` and `[FactoryEventHandler<T>]` shapes — that is [TRIM-008](./008-registrar-dam-over-preservation.md), which lands first and does not reach this.

---

## Measured evidence (2026-08-13, pre-fix probe on branch `TRIM-008-registrar-dam-over-preservation`)

Recorded here so this plan starts from a measurement rather than an inherited story — the arc has lost a plan to the latter once already (TRIM-005).

**The observation.** In the harness's Save/Can\* leg (`TrimSaveTarget`), a publish-trimmed client retains `ISaveLegPort`, `SaveLegInvoke`, and all three body literals `SaveLegInsertBody_MARKER` / `SaveLegUpdateBody_MARKER` / `SaveLegDeleteBody_MARKER`.

**The controlled comparison.** Within the *same* assembly, `TrimTestEntityFactory.LocalCreate` and `TrimSaveTargetFactory.LocalInsert/Update/Delete` differ in exactly one respect — `async`:

| | `LocalCreate` | `LocalInsert` / `LocalUpdate` / `LocalDelete` |
|---|---|---|
| Guard | `if (!NeatooRuntime.IsServerRuntime) throw` | identical |
| Server-only reach | `GetRequiredService<IServerOnlyRepository>()` | `GetRequiredService<ISaveLegPort>()` |
| Rooted by | unguarded `AddScoped<CreateDelegate>` closure in `FactoryServiceRegistrar` | unguarded `AddScoped<SaveDelegate>` closure |
| DAM-preserved | yes | yes |
| `async` | **no** | **yes** |
| Post-guard body after trimming | **eliminated** (`IServerOnlyRepository`, `DoServerWork` absent) | **retained** |

**Corroboration.** Surviving state machines in the trimmed DLL: `<LocalInsert>d__15`, `<LocalUpdate>d__16`, `<LocalDelete>d__17`, `<LocalSave>d__21`. `<LocalCreate>d__` does not exist — it is not an async method. The feature-switch fold happens inside `MoveNext`, and the remainder is not eliminated there.

**What is deliberately NOT claimed.** No assertion is made here about *why* ILLink treats the two cases differently. The statement that survives scrutiny is the empirical one in the table. Any causal story about ILLink internals must be re-derived against the artifact before it is built on.

**Why the TRIM-008 remedy does not apply — corrected 2026-08-13 after code review.**

The first draft of this paragraph argued: *"the attribute correctly names the generated `TrimSaveTargetFactory`"*, as though naming a generated type settled it, and cited the static-leg fix leaving these markers unchanged as confirmation. **Both halves were wrong**, and are recorded here rather than quietly rewritten because building on them would have cost a cycle.

- `TrimSaveTargetFactory` is *itself* the type that hosts the leaking bodies — `LocalInsert`, `LocalUpdate`, `LocalDelete`, `LocalSave` are all its members. Its assembly attribute carries `DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)`, so **DAM is live on this leg and is rooting those bodies**. "Generated, not consumer" is not the operative distinction; what makes TRIM-008's holders safe is that a holder has exactly *one* method.
- The static-leg measurement could not confirm anything about this leg. Fixing `TrimTestCommands`'s attribute has no bearing on `TrimSaveTargetFactory`'s attribute, which still names the factory. That was a non-sequitur.

**What actually supports the conclusion**, verified in the emitted code: `ClassFactoryRenderer` emits its delegate registrations with **no `IsServerRuntime` guard** (unlike `StaticFactoryRenderer`), producing an `AddScoped<SaveDelegate>` closure that captures the factory and calls `LocalSave`, which routes to `LocalInsert`. That is an independent root chain. So a forwarding holder alone would not clear these markers — **but the holder indirection is plausibly part of the eventual fix rather than orthogonal to it**, because DAM is rooting them too. A guard-only remedy would ship with DAM still holding `LocalInsert`.

---

## The "async" hypothesis, narrowed by measurement (2026-08-13)

The original framing — "async generated `Local*` methods retain their server-only bodies; sync ones do not" — was drawn from a two-case comparison. Three async targets were then added to the harness and **all trim clean**:

| Shape | Guard | Reaches server-only code via | `async` | Body rooted at all? | Result |
|---|---|---|---|---|---|
| static `[Execute]` | `if (IsServerRuntime) { … }` wrapping, in a non-async registrar | direct static call | yes | **no** — fold deletes the registration | absent (moot) |
| relay handler | same wrapping guard | direct static call | yes | **no** — same | absent (moot) |
| interface factory | `if (!IsServerRuntime) throw` early | **the interface** | yes | yes, but markers are behind the interface hop | **not measurable** |
| class factory `Create` | `if (!IsServerRuntime) throw` early | direct call on the concrete type | **no** | yes | clean |
| class factory `Insert`/`Update`/`Delete` | `if (!IsServerRuntime) throw` early | direct call on the concrete type | **yes** | yes | **LEAKS** |

**Read the first three rows carefully — they are weaker than they look**, and the first draft of this table overstated them as "clean".

- **Static and relay** put their guard in a *wrapping* block inside the non-async `FactoryServiceRegistrar`, so the fold deletes the whole registration and the async body is never rooted. Their markers being absent is real evidence that TRIM-008's fix generalizes to async `[Execute]` (`_DoAsyncWork` is a method on the consumer's class, exactly what the DAM used to retain) — but it says nothing about async fold behaviour, because that mechanism is never reached.
- **Interface factory cannot measure this property at all.** Its markers sit on the implementation, which the generated body reaches through an interface, so they read absent whether or not the body survives. This is structural: an interface factory reaches everything through interfaces. The direct fix — a `[Service]` parameter putting `GetRequiredService<IAsyncLegPort>()` into the generated body — was attempted and **does not compile** (Deferred Work item 19).

**So the wider hypothesis is narrowed, not confirmed** — and the cross-class pair originally offered as "single-variable" was not. `TrimTestEntityFactory.LocalCreate` vs `TrimSaveTargetFactory.LocalInsert` differ in at least four further ways: an `[AuthorizeFactory<T>]` block, target-from-DI vs target-from-parameter, **one-hop vs two-hop rooting** (there is no `InsertDelegate`; `Insert` is reached through `SaveDelegate` → `LocalSave`), and an extra catch arm plus lifecycle probes. That last one matters most, because the arc's own disproven TRIM-004 story blamed exactly *"early-throw guard + try/catch defeats unreachable-code elimination"*.

## The controlled experiment (2026-08-13) — `async` confirmed

Run at TRIM-008's re-review rather than deferred to this plan, because it decides this plan's scope. `TrimTestEntity` gained an `async [Remote][Fetch] FetchAsync` beside its existing sync `[Remote][Create] Create`, each writing its own literal into its own body:

| | `ClassSyncBody_MARKER` | `ClassAsyncBody_MARKER` |
|---|---|---|
| Body | `TrimTestEntity.Create` | `TrimTestEntity.FetchAsync` |
| Declaring type / generated factory / registrar | identical | identical |
| `[AuthorizeFactory<T>]` | none | none |
| Rooting | one hop, own delegate | one hop, own delegate |
| Reached by | direct call on the concrete type | direct call on the concrete type |
| Literal position | in the domain body | in the domain body |
| `async` | **no** | **yes** |
| Untrimmed | PRESENT | PRESENT |
| **Trimmed** | **absent** | **PRESENT** |

Every confound listed above is controlled, and both halves are rooted **twice and identically** — by DAM on `TrimTestEntityFactory` (the assembly attribute names the generated factory, which hosts both methods) and by their own unguarded delegate registration. That closes the "maybe the sync one simply was not rooted" alternative outright.

**`async`-shaped emission is the operative variable for the class-factory leg**, and this plan's scope is correct.

### But the sub-cause is undetermined, and it changes the fix

Say "async-shaped emission", not "the `async` keyword". Five constructs appear in the async body and not the sync one, all emitted *because* the method is async and therefore inseparable from outside the generator:

1. an extra `catch (OperationCanceledException)` arm
2. `if (target is IFactoryOnStartAsync) await …`
3. `if (target is IFactoryOnCompleteAsync) await …`
4. `if (target is IFactoryOnCancelled) …` (inside the OCE arm)
5. `if (target is IFactoryOnCancelledAsync) await …` (inside the OCE arm)

Items 2–5 are **interface type-tests**, a mechanically different ILLink retention path from a state machine: a type-test against a rooted type can keep its branch alive independently of any `MoveNext` fold. So two hypotheses survive the experiment and it cannot separate them:

- **H1 — the fold does not propagate through the state machine.** The switch folds inside `MoveNext` and the remainder survives.
- **H2 — the fold works, and unreachable-code elimination is defeated** by the second catch arm and/or the async lifecycle type-tests. **This is the disproven TRIM-004 story returning in async-only form** — the arc set that story aside for the wrong reason and never re-tested it.

**They imply different remedies.** Under H1 the guard must move out of the async method entirely (a sync wrapper testing `IsServerRuntime` before calling the async body). Under H2 the state machine is innocent and the fix is to restructure catch/probe emission — and note `TrimTestEntity` implements **none** of those four interfaces while `target` is statically typed `TrimTestEntity`, so simply not emitting probes for interfaces the concrete type provably cannot implement may be the whole fix.

**Separate them before choosing a remedy.** From inside the generator it is easy: emit a *sync* `Local*` carrying a second catch arm, or an *async* one without the probes, and re-probe. That is this plan's first step, not its design conclusion.

**One remedy is ruled out already:** de-rooting. DAM on `typeof(TrimTestEntityFactory)` will always root `LocalFetchAsync` regardless of registration changes, so no amount of guarding the delegate registrations removes the root.

**Free variable, controlled and harmless:** the sync half resolves two server-only services and the async half one. It cuts the safe way — the sync body has strictly *more* server-only reach and still folds clean. It is also load-bearing for the harness: giving the async half `IServerOnlyRepository` would surface that name in the trimmed output and turn the gate's static-factory `[D]` markers red for a misleading reason. The asymmetry must stay until this plan lands.

Corroboration from the same run: `IClassLegPort` and `ClassLegInvoke` flipped to PRESENT once `FetchAsync` existed — retained by its in-body `GetRequiredService<IClassLegPort>()` — mirroring `ISaveLegPort`/`SaveLegInvoke` on the save leg. The gate caught that flip on its first run after the target landed, which is the per-leg attribution working.

**Still open:** whether the early-throw guard shape and the direct-concrete-call shape are *necessary* as well. Neither has independent evidence — the static/relay rows cannot discriminate (over-determined) and the interface row cannot go red at all. Do not present them as established conditions.

**For the design turn:** do not treat "async is the cause" as established. The controlled pair says `async` is the differing variable *within the class-factory leg*; whether the operative mechanism is the async state machine, the early-throw guard shape, or their combination is not settled, and the remedy differs for each. Re-derive it against the artifact before building on it.

**Baseline inheritance.** This plan does **not** need its own pre-fix probe. The 2026-08-13 measurement above *is* the baseline, captured before any fix to this leg existed, with markers proven visible by a self-check against the untrimmed assembly. Harness targets, per-leg ports, and the probe script all land with TRIM-008.

---

## Open questions for the design turn

- Is the remedy generator-side (guard the delegate registrations, restructure the emitted guard so the fold is not inside `MoveNext`, keep server-only work out of async `Local*` bodies) or configuration-side (an ILLink feature/substitution the generator emits)? Unknown; do not assume. Whichever it is, check whether the DAM root also has to be addressed — see the corrected paragraph above.
- ~~Does the same retention affect **async `[Execute]`** and **async relay handlers**?~~ **Answered 2026-08-13: no.** Async targets were added to the harness for both legs plus an async interface-factory method, and all trim clean. See the table above for why none of them is the leaking shape.
- Would changing the emitted guard from `if (!IsServerRuntime) throw …` to a *wrapping* `if (IsServerRuntime) { … }` block fix this without touching DAM or the registrations? Worth trying because it is cheap, **but note that no measurement points at it**: the static and relay legs use that shape and are clean, yet their cleanliness is over-determined (post-TRIM-008 they are no longer DAM targets *and* their only reference sits inside the folded block), so they cannot show the guard shape is what does the work. Treat this as an untested idea, not as something the evidence suggests.
- **First step, before any design work: separate H1 from H2.** Emit a *sync* `Local*` with a second catch arm, or an *async* one without the lifecycle type-tests, and re-probe. The controlled experiment cannot do this from outside the generator; from inside it is cheap. Choosing a remedy before separating them is how TRIM-004 → TRIM-005 was lost.
- Do the async lifecycle type-tests (`IFactoryOnStartAsync` / `IFactoryOnCompleteAsync` / `IFactoryOnCancelled` / `IFactoryOnCancelledAsync`) keep their branches alive? `TrimTestEntity` implements none of them and `target` is statically typed, so not emitting probes for interfaces the concrete type provably cannot implement is both a candidate fix and a worthwhile emission improvement regardless of the outcome.
- Does `LocalSave`'s routing keep `LocalInsert`/`LocalUpdate`/`LocalDelete` rooted even if their own registrations were guarded?
- CI gate: which of the new Save/Can\* markers can be asserted absent once this lands, and what is the durable positive control for them.

---

## The separation experiment (2026-08-14)

The stub's declared first step, run before any design work. Evidence: [`../reviews/009-evidence/`](../reviews/009-evidence/).

**Apparatus.** Two compile-time knobs in `ClassFactoryRenderer`, plus a third for the remedy probe. Before any variant was run, both knobs were built at their default (`false`) and the whole generated tree diffed against the pre-edit tree — **identical**, so the apparatus is inert and any later difference is attributable to the knob rather than to the refactor that carried it. The knobs were reverted afterwards and the emission re-diffed back to HEAD: **identical again**. Archived as `experiment-knobs.diff`.

| | Shape emitted | `ClassSyncBody_MARKER` | `ClassAsyncBody_MARKER` | State machines |
|---|---|---|---|---|
| HEAD | as shipped | absent | **PRESENT** | `<LocalFetchAsync>d__` present |
| **V1** | async, **minus** all four awaiting probes **and** the OCE catch arm | absent | **PRESENT** | `<LocalFetchAsync>d__` present |
| **V2** | sync, **plus** an OCE catch arm **and** the `IFactoryOnCancelled` probe | **absent** | PRESENT (untouched) | `<LocalCreate>d__` absent |
| **V3** | guard in a sync wrapper; async body in a `private` core | absent | **PRESENT** | `<LocalFetchAsync>d__` **absent**, `<LocalFetchAsyncCore>d__` **present** |

All five positive controls passed on every run, so each "absent" is a real absence rather than an unread artifact.

### H1 confirmed; H2 falsified in both directions

- **V1 — subtractive.** Strip every construct H2 blames and the async body *still* leaks. H2's constructs are **not necessary** for the leak.
- **V2 — additive.** Graft those same constructs onto the sync body and it *still* trims clean. H2's constructs are **not sufficient** to cause one.

**H1 is the mechanism: the fold does not propagate out of the async state machine.** This also falsifies the TRIM-004 story — *"early-throw guard plus try/catch defeats unreachable-code elimination"* — for the **third** time in this arc, and for the first time by a direct additive test rather than by an argument.

**Best-supported mechanism, offered as inference and not as measurement.** In an async method the *entire* user body — guard included — is lowered into `MoveNext` and wrapped in the compiler's own try/catch that funnels exceptions onto the builder. The fold therefore lands *inside* a protected region, and the unreachable remainder is not removed. In the sync method the guard sits *before* the user's `try`, so unreachability begins outside any protected region and the whole remainder, `try` block and all, is eliminated. This is consistent with all four rows; what is *measured* is the behaviour in the table, and any remedy must be re-verified against a published artifact rather than against this paragraph.

### V3 falsifies the remedy the stub predicted

The stub proposed "move the guard out of the async method entirely (a sync wrapper testing `IsServerRuntime` before calling the async body)". **Half of that prediction held and half failed, and the failing half would have shipped looking correct:**

- **Held:** `<LocalFetchAsync>d__` is *gone* in V3. With the guard outside the state machine, the fold does its job.
- **Failed:** `ClassAsyncBody_MARKER`, `IClassLegPort`, and `ClassLegInvoke` are **still present**, because `<LocalFetchAsyncCore>d__` survives. `DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)` covers **NonPublic**, so relocating the body into a `private` member of the same factory does not escape the DAM root.

A guard-relocation-only fix would have produced a smaller assembly, deleted one state machine, and left the IP on the client.

---

## Root inventory (read out of the emitted source, not inferred)

Two roots reach `LocalFetchAsync`:

1. **DAM** — `[assembly: NeatooFactoryRegistrar(typeof(global::RemoteFactory.TrimmingTests.TrimTestEntityFactory))]`. The attribute names the factory itself, so `PublicMethods | NonPublicMethods` roots every `Local*` **and** any private core beside them.
2. **The delegate registration closure** — `services.AddScoped<FetchAsyncDelegate>(cc => { var factory = …; return (…) => factory.LocalFetchAsync(…); })`, emitted with **no** `IsServerRuntime` guard, unlike the static and relay legs.

**Not a root:** `ITrimTestEntityFactory` declares only `Create` and `FetchAsync` — the public entry points — never `Local*`. Verified in the emitted interface.

**The sync `LocalCreate` carries both of those same roots and still trims clean.** So this was never a rooting problem in the sync case, and "de-root it" is not a description of the defect — it is one of two things the fix has to do, because rooting only becomes fatal once elimination stops working.

---

## Approach

**Two changes, each already measured in isolation, never yet measured together.**

1. **Sync wrapper for guarded async `Local*`** — the guard moves to a non-async wrapper; the async body moves to a private core. V3 proved this alone puts the fold back outside the protected region.
2. **Holder indirection for class factories** — the assembly attribute names a generated single-method holder rather than `{X}Factory`, exactly as [TRIM-008](./008-registrar-dam-over-preservation.md) already did for the static and relay legs. This removes the DAM root that V3 proved is independently retaining the core.

**The prediction that makes this small:** with the wrapper in place, the *closure* root dies too without being touched. The closure calls the **wrapper**; the wrapper's `return LocalXCore(…)` sits after the guard, so the fold removes it and the core loses its last reference. If that holds, **the delegate registrations need no guarding**, and root #2 above requires no code change.

**This is a prediction, and the arc's rule applies to it.** Each half is measured; the combination is not. Step 1 is to measure the combination against a published artifact before anything else is built on it — the same discipline that just caught the stub's own predicted remedy.

**Why holder indirection does not break prebuilt consumers.** It changes *which type the attribute names*, not the breadth of the DAM. A library compiled by an older generator keeps naming its factory and keeps exactly today's behaviour — no registration is lost and no diagnostic is needed. This is the compatibility argument TRIM-008 made and shipped; TRIM-009 reuses it rather than re-deriving it.

**Rejected, with reason:** narrowing the DAM on `NeatooFactoryRegistrarAttribute` to `PublicMethods`. Rejected at TRIM-008's plan review and still rejected — a prebuilt library whose registrar is `internal static` would silently stop registering on a trimmed client, with no diagnostic. TRIM-009 changes nothing about `FactoryAttributes.cs`.

---

## Steps

1. **Measure the combination (V4) before building on it.** Wrapper + holder together, published trimmed, probed. Expected: `ClassAsyncBody_MARKER`, `IClassLegPort`, `ClassLegInvoke`, and all three `SaveLeg*Body_MARKER` absent; positive controls unchanged. **If the closure root survives the wrapper, stop and re-design** — do not proceed on the assumption it will work.
2. Emit the sync wrapper / private core split for guarded async `Local*` methods across **all three** emission sites in `ClassFactoryRenderer` — the read path and both write paths. The experiment only wired the read path; the Save/Can\* leg is reached by the other two.
3. Emit the registrar holder for the class-factory leg and retarget its assembly attribute, reusing TRIM-008's proven shape and a leg-distinct prefix.
4. Pin both in generator unit tests, including the regression assertion that the attribute **does not** name the factory type — the check whose absence let the static leg ship broken. Prove each new assertion RED before green.
5. Flip the CI gate: the eight markers TRIM-008 deliberately asserted **PRESENT** as a TRIM-009 tripwire become absence assertions, each with a durable positive control.
6. Retire the load-bearing asymmetry note in `TrimTestEntity.cs`. Its stated reason — that giving the async half `IServerOnlyRepository` would surface that name and redden the static-factory markers for a misleading reason — **expires when this lands**, and a stale do-not-touch comment is its own hazard.
7. Work the nine **TRIM-009-dependent doc anchors** enumerated in [`../reviews/008-doc-anchor-inventory.md`](../reviews/008-doc-anchor-inventory.md), including the forward-looking skill table row TRIM-008 wrote on the promise this plan would land.
8. Reconcile the container: close deferred item 18, update AC6, record what the experiment retired.

## Acceptance

- **AC6's third shape closes:** on a publish-trimmed client, an `async [Remote] internal` class-factory operation leaves behind no `[Service]` interface name, no called-member name, and no body literal — measured, both read (`Fetch`) and write (`Insert`/`Update`/`Delete`).
- The sync leg does not regress: `ClassSyncBody_MARKER`, `IServerOnlyRepository`, `DoServerWork`, `ServerOnlyRepository_MARKER`, `ServerOnlyHelper` stay absent.
- Positive controls still pass, so the absences remain falsifiable.
- Full suite green on net9.0 + net10.0, both solutions, plus the harness exiting 0.
- The nine dependent doc anchors are true of shipped behaviour.

## Verification

- **Red before green**, per this arc's standing rule and TRIM-006's two never-failing checks. Every flipped gate assertion must be observed failing against the pre-fix artifact — which for this plan is cheap, because HEAD *is* the pre-fix artifact and the probes are already archived.
- **Baseline inherited, not re-derived.** The 2026-08-13 measurement plus the three variants above are the baseline; markers were proven visible by a self-check against the untrimmed assembly.
- **Publish-only.** `dotnet build` / `run` / `test` prove nothing here; `dotnet test` never runs this project.
- **Generated-output drift needs a real diff** — `**/Generated/` is gitignored, so "nothing else changed" must be measured, not asserted. The inert-knob diff above is the pattern to reuse.
- **Zero incremental-cache delta.** No model, builder, or transform change is contemplated; `IncrementalCacheTests` should be untouched, and that claim is checkable with `git diff --name-only -- src/Generator/`.

## Files

**Generator:** `ClassFactoryRenderer.cs` — three `Local*` emission sites (lines ~334, ~832, ~1335 at HEAD) plus new holder emission. Deliberately **not** touched: `FactoryAttributes.cs`, any model, builder, or transform.

**Tests:** `AssemblyAttributeEmissionTests.cs` (class-factory region, new), `verify-trimmed.sh` (eight assertions flip), `TrimTestEntity.cs` (asymmetry note retires).

**Docs:** the nine anchors in the TRIM-009-dependent table of `008-doc-anchor-inventory.md`.

## Risks

- **The wrapper changes the emitted factory's method shape.** `public async Task<T> LocalX(…)` becomes `public Task<T> LocalX(…)` plus a private core. The *signature* is unchanged, so no public API break — but exception timing moves: an exception that used to surface as a faulted `Task` now throws synchronously from the wrapper. The guard already threw synchronously in the sync case, so this aligns the two; it is still a behaviour change and belongs in the release notes.
- **Three emission sites, one experiment.** Only the read path was exercised. The write paths differ (`cTarget`, different lifecycle helpers) and are where the Save/Can\* leg lives.
- **`NormalizeWhitespace` has no error signal** — malformed emission yields mangled output, not an exception. Assert on exact fragments.
- **Two legs already carry holders with distinct prefixes**; a third must not collide with `NeatooFactoryRegistrar_` or `NeatooEventHandlerRegistrar_`, and a class carrying several factory attributes must not gain a CS0101 on top of its existing CS0111 (deferred item 15).

## Out of scope

- Deferred items 5, 9, 14, 15, 19 — each needs its own plan.
- Replacing the reflective registrar lookup with `[ModuleInitializer]` registration, which would delete this defect class entirely. Rejected here for load-order hazards; worth its own plan.
- The v1.7.0 release itself — TRIM-009 unblocks it; cutting it is the arc's close-out step.
