# TRIM-009 — Async `Local*` factory-method body retention

**Plan #:** 009
**Date:** 2026-08-13
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done
**Last Updated:** 2026-08-14
**Plan review:** [`../reviews/009-plan-review.md`](../reviews/009-plan-review.md) — CONCERNS, 8 veto-tier, all closed before implementation
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
| **V3** | guard in a sync wrapper; async body in a `private` core | absent | **PRESENT** | `<LocalFetchAsyncCore>d__` **present** (see note) |

All five positive controls passed on every run, so each "absent" is a real absence rather than an unread artifact.

> **Note on V3's state-machine row, corrected at plan review (finding B1/B2).** The first draft of this table also reported `<LocalFetchAsync>d__` **absent** in V3 and read that as "the fold works once the guard is outside the state machine". **That was a check that could not have gone red.** The V3 knob emits `LocalFetchAsync` as a non-async wrapper, so the compiler never creates a state machine by that name — its absence is a compile-time consequence of the rename, not a trimming result. The row is struck. `<LocalFetchAsyncCore>d__` was measured, but in a separate command that never reached the archived probe; it is re-measured and recorded in the evidence addendum.

### H1 confirmed; H2 falsified in both directions

- **V1 — subtractive, and this is what carries the finding.** Strip *all five* constructs H2 blames — the OCE catch arm and all four lifecycle probes — and the async body *still* leaks. **None of them is necessary.**
- **V2 — additive, and narrower than the first draft claimed.** Graft the OCE catch arm and the (non-awaiting) `IFactoryOnCancelled` probe onto the sync body and it *still* trims clean. That is **2 of the 5** constructs: the three *awaiting* probes cannot be grafted onto a sync method at all, so V2 cannot speak to them. Corrected at plan review (finding B5) — the falsification rests on V1, with V2 as partial corroboration.

**H1 is the mechanism: the fold does not propagate out of the async state machine.** This also falsifies the TRIM-004 story — *"early-throw guard plus try/catch defeats unreachable-code elimination"* — for the **third** time in this arc, and for the first time by a direct additive test rather than by an argument.

**Best-supported mechanism, offered as inference and not as measurement.** In an async method the *entire* user body — guard included — is lowered into `MoveNext` and wrapped in the compiler's own try/catch that funnels exceptions onto the builder. The fold therefore lands *inside* a protected region, and the unreachable remainder is not removed. In the sync method the guard sits *before* the user's `try`, so unreachability begins outside any protected region and the whole remainder, `try` block and all, is eliminated. This is consistent with all four rows; what is *measured* is the behaviour in the table, and any remedy must be re-verified against a published artifact rather than against this paragraph.

### V3 falsifies the remedy the stub predicted

The stub proposed "move the guard out of the async method entirely (a sync wrapper testing `IsServerRuntime` before calling the async body)". **V3 emitted exactly that, and the markers did not move:** `ClassAsyncBody_MARKER`, `IClassLegPort`, and `ClassLegInvoke` are all still present, and `<LocalFetchAsyncCore>d__` survives.

**So guard relocation alone does not clear this leg.** A fix that stopped there would have produced a smaller assembly, deleted one state machine, and left the IP on the client.

**What V3 does *not* establish — corrected at plan review.** The first draft claimed V3 proved the fold works inside the wrapper, and attributed the core's survival to DAM. Neither follows:

- The "fold works" claim rested on `<LocalFetchAsync>d__` being absent, which the rename guarantees regardless of trimming. Struck; see the note above.
- **V3 cannot attribute the core's survival at all.** Both candidate roots were live in V3 — DAM on the factory type, *and* the wrapper's own `return LocalFetchAsyncCore(…)` call site. "DAM roots the core" and "the wrapper's call survived the fold" predict the identical observation, and they imply **different remedies**: only the first is addressed by holder indirection. Separating them is [Step 1](#steps).

This is the arc's own rule — *a check that could never go red is not evidence* — recurring for the fourth time, in the plan written to avoid exactly that. It is recorded rather than quietly rewritten because the correction is the useful part.

---

## Root inventory (read out of the emitted source, not inferred)

**Three** roots reach `LocalFetchAsync`. The first draft of this section said two and called itself exhaustive; the third was found at plan review (finding B3).

1. **DAM** — `[assembly: NeatooFactoryRegistrar(typeof(global::RemoteFactory.TrimmingTests.TrimTestEntityFactory))]`. The attribute names the factory itself, so `PublicMethods | NonPublicMethods` roots every `Local*` **and** any private core beside them.
2. **The delegate registration closure** — `services.AddScoped<FetchAsyncDelegate>(cc => { var factory = …; return (…) => factory.LocalFetchAsync(…); })`, emitted with **no** `IsServerRuntime` guard, unlike the static and relay legs.
3. **The local constructor's method-group assignment** — `ClassFactoryRenderer.cs:220-223` emits `{UniqueName}Property = Local{UniqueName};`, so the ctor body reads `FetchAsyncProperty = LocalFetchAsync;`. That ctor is reachable: `FactoryServiceRegistrar` emits `services.AddScoped<{X}Factory>()`, whose generic parameter carries `DynamicallyAccessedMembers(PublicConstructors)`.

**Root 3 does not change the prediction below** — the method group targets the **wrapper**, exactly as the closure does, so the core still loses its last non-DAM reference when the wrapper's post-guard call folds away. It is recorded because an inventory that claims to be empirical, and that Step 1's stop condition is read against, has to be right.

**Not a root:** `ITrimTestEntityFactory` declares only `Create` and `FetchAsync` — the public entry points — never `Local*`. Verified in the emitted interface. (Noted for completeness, though this was never the plausible one; the ctor assignment was.)

**The sync `LocalCreate` carries both of those same roots and still trims clean.** So this was never a rooting problem in the sync case, and "de-root it" is not a description of the defect — it is one of two things the fix has to do, because rooting only becomes fatal once elimination stops working.

---

## Approach

**Two changes. Neither has been measured working — V3 measured the first one *not* sufficient on its own, which is a different thing.**

1. **Sync wrapper for guarded async `Local*`** — the guard moves to a non-async wrapper; the async body moves to a private core. This is the lever H1 implies: get the fold out from inside the state machine's protected region.
2. **Holder indirection for class factories** — the assembly attribute names a generated single-method holder rather than `{X}Factory`, exactly as [TRIM-008](./008-registrar-dam-over-preservation.md) already did for the static and relay legs. This removes the DAM root, which V3 leaves live and therefore could not rule in or out.

**The prediction that makes this small:** with the wrapper in place, roots 2 and 3 die too without being touched. Both reference the **wrapper**; the wrapper's `return LocalXCore(…)` sits after the guard, so the fold removes it and the core loses its last non-DAM reference. If that holds, **the delegate registrations need no guarding** and neither does the ctor assignment.

**This is a prediction, and the arc's rule applies to it.** Each half is measured; the combination is not. Step 1 is to measure the combination against a published artifact before anything else is built on it — the same discipline that just caught the stub's own predicted remedy.

**Why holder indirection does not break prebuilt consumers.** It changes *which type the attribute names*, not the breadth of the DAM. A library compiled by an older generator keeps naming its factory and keeps exactly today's behaviour — no registration is lost and no diagnostic is needed. This is the compatibility argument TRIM-008 made and shipped; TRIM-009 reuses it rather than re-deriving it.

**Rejected, with reason:** narrowing the DAM on `NeatooFactoryRegistrarAttribute` to `PublicMethods`. Rejected at TRIM-008's plan review and still rejected — a prebuilt library whose registrar is `internal static` would silently stop registering on a trimmed client, with no diagnostic. TRIM-009 changes nothing about `FactoryAttributes.cs`.

---

## Steps

1. **Measure the combination (V4) before building on it.** Wrapper + holder together, published trimmed, probed. Expected: `ClassAsyncBody_MARKER`, `IClassLegPort`, `ClassLegInvoke`, and all three `SaveLeg*Body_MARKER` absent; positive controls unchanged. **If the closure or ctor-assignment root survives the wrapper, stop and re-design** — do not proceed on the assumption it will work.

   **The stop condition needs a liveness check, not just absence** (finding B7). `AddRemoteFactoryServices` resolves the registrar with `GetMethod(...)` then `method?.Invoke(...)`, so a holder that fails to forward produces **no diagnostic and no exception** — every marker would go absent and V4 would read as a flawless result. V4 does not count as green unless it also carries a **named** positive control for the class-factory holder type (full name, as `verify-trimmed.sh` already does for the other two) **and** the harness resolves the class factory and exits 0.
2. Emit the sync wrapper / private core split for guarded async `Local*` methods across **all five** emission sites in `ClassFactoryRenderer`, named rather than numbered because the first draft's three-site list and its rationale were both wrong (finding B4):

   | Renderer method | Shape | `async` when |
   |---|---|---|
   | `RenderReadLocalMethod` | read | `IsAsync \|\| IsDomainMethodTask` — **the only site the experiment wired** |
   | `RenderClassExecuteLocalMethod` | class-level `[Execute]` | **always** — see Step 2a |
   | `RenderLocalMethod` | write | `IsAsync \|\| IsDomainMethodTask` |
   | `RenderSaveLocalMethod` | `LocalSave` | `IsAsync`; emitted `public virtual` |
   | `RenderCanLocalMethod` | `Can*` | `IsAsync` |

   The struck rationale claimed the Save/Can\* leg was reached by the write and `Can*` sites. Measured in the emitted `TrimSaveTargetFactory`: the `Can*` methods are **synchronous** (`public Authorized LocalCanCreate(…)`) and `LocalSave` is its own async site. **Leaving `LocalSave` unwrapped would probably still clear the markers** — its surviving body references the wrappers, whose folds kill the cores — which means Step 1 could come back green while a guarded async body still ships, invisible to the gate. That is why the site list is enumerated here instead of inferred from a passing probe.

2a. **Class-level `[Execute]` needs its own decision, and it is release-blocking** (finding A3). `RenderClassExecuteLocalMethod` emits `public async` **unconditionally**, with the same guard, resolving `[Service]`s in the generated body and calling the consumer's `public static` method directly — so H1 applies in full. It is a **Design source-of-truth pattern** (`Design.Domain/FactoryPatterns/ClassFactoryWithExecute.cs`), and the harness has **no target for it**, so AC6's "proven in the trimmed harness, not inferred" cannot be satisfied for this shape today. Add a harness target and fix it with the rest; do not close AC6 while it is unmeasured.

3. Emit the registrar holder for the class-factory leg and retarget its assembly attribute, reusing TRIM-008's proven shape and a leg-distinct **third** prefix.

3a. Update the `NeatooFactoryRegistrarAttribute` XML contract in `FactoryAttributes.cs` (finding A1). The plan previously declared that file untouched, which would ship a knowingly-false contract in the very remarks written to stop this defect recurring. Today it says the `Type` "must be a GENERATED registrar type. Never a consumer's own class" — after this plan that is **necessary but not sufficient**, since the class leg already named a generated type and still leaked. The historical note listing two legs also becomes three.
4. Pin both in generator unit tests, including the regression assertion that the attribute **does not** name the factory type — the check whose absence let the static leg ship broken. Prove each new assertion RED before green. **This is an inversion of an existing passing test, not new coverage:** `AssemblyAttributeEmissionTests.cs:42` currently asserts the attribute names `global::TestNamespace.MyEntityFactory`. Original intent is preserved — the attribute is still emitted and still names the correct type; what changes is that the correct type is now a generated single-method holder. Naming it as an inversion is what the sacred-tests rule requires.
5. Flip the CI gate: the eight markers TRIM-008 deliberately asserted **PRESENT** as a TRIM-009 tripwire become absence assertions, each with a durable positive control. **The prose flips too** — `verify-trimmed.sh` carries a block explaining why `IClassLegPort`/`ClassLegInvoke` are excluded from the absence list, a controlled-pair block that still says "TRIM-009 must separate them from inside the generator" (now done), a KNOWN-BROKEN block, and a summary line. All become false with the fix.
6. Retire the load-bearing asymmetry note in `TrimTestEntity.cs`. Its stated reason — that giving the async half `IServerOnlyRepository` would surface that name and redden the static-factory markers for a misleading reason — **expires when this lands**, and a stale do-not-touch comment is its own hazard. The same file's "WHAT THIS PAIR DOES NOT ISOLATE" paragraph is also answered by the experiment and must go with it.
7. Work **two disjoint doc sets**. The first draft named only the first and would have shipped the second false (finding A2).
   - **7a — the nine body-trimming anchors** enumerated in [`../reviews/008-doc-anchor-inventory.md`](../reviews/008-doc-anchor-inventory.md), including the forward-looking skill table row TRIM-008 wrote on the promise this plan would land.
   - **7b — the holder anchors, all written by TRIM-008 and falsified by this plan's Approach.** `CLAUDE-DESIGN.md:760` and `docs/trimming.md:249` both state that for class factories the protection comes from the guard, "**not the choice of attribute target**" — the exact claim TRIM-009 reverses. Also: the `CLAUDE-DESIGN.md` attribute-target table's class-factory row (`typeof({X}Factory)` → holder), `CLAUDE-DESIGN.md:771` ("the **two** holder rows" → three), the "until v1.7.0 the static-factory and event-handler rows named the user's own class" note, and the skill's two-leg framing in `skills/RemoteFactory/references/trimming.md`. Build this list by reading the files, per the inventory's own lesson about listing before editing.
8. Reconcile the container:
   - Close deferred item **18**; fire item **2** (the release hold, which reopens when items 1 and 18 land); discharge item **8**'s residual risk via Step 7.
   - **State what "update AC6" means.** AC6 says "**any** async operation" and "proven in the trimmed harness, not inferred". If Step 2a lands, AC6 closes as written. If class-level `[Execute]` is descoped instead, AC6 must be **narrowed in writing** with the shape named and a Deferred Work row created — silently closing it while a documented Design pattern still leaks is the precise failure AC6 exists to prevent.
   - Add a Deferred Work row for the **interface-factory leg** (finding A4) — it carries the identical guard-inside-async shape and still points its attribute at `{ImplName}Factory`, so it shares both mechanisms and gets neither fix here. Deliberately **not** taken into scope: it would balloon this plan, and deferred item 19 makes the leg structurally unmeasurable anyway. But the skill's "Interface factory | Yes" row must not ship unqualified — deferring the work is fine, shipping a false claim is not.

## Acceptance

- **AC6's third shape closes:** on a publish-trimmed client, an `async [Remote] internal` class-factory operation leaves behind no `[Service]` interface name, no called-member name, and no body literal — measured across read (`Fetch`), write (`Insert`/`Update`/`Delete`), `LocalSave`, and **class-level `[Execute]`** (Step 2a), which is unconditionally async and today has no harness coverage at all. If any of those shapes is descoped, AC6 is narrowed in writing rather than closed over it.
- The sync leg does not regress: `ClassSyncBody_MARKER`, `IServerOnlyRepository`, `DoServerWork`, `ServerOnlyRepository_MARKER`, `ServerOnlyHelper` stay absent.
- Positive controls still pass, so the absences remain falsifiable.
- Full suite green on net9.0 + net10.0, both solutions, plus the harness exiting 0.
- The nine dependent doc anchors are true of shipped behaviour.

## Verification

- **Red before green**, per this arc's standing rule and TRIM-006's two never-failing checks. Every flipped gate assertion must be observed failing against the pre-fix artifact — which for this plan is cheap, because HEAD *is* the pre-fix artifact and the probes are already archived.
- **Baseline inherited, not re-derived.** The 2026-08-13 measurement plus the three variants above are the baseline; markers were proven visible by a self-check against the untrimmed assembly.
- **Publish-only.** `dotnet build` / `run` / `test` prove nothing here; `dotnet test` never runs this project.
- **Generated-output drift needs a real diff** — `**/Generated/` is gitignored, so "nothing else changed" must be measured, not asserted. The inert-knob diff above is the pattern to reuse.
- **Zero incremental-cache delta.** No model, builder, or transform change is contemplated. The first draft offered `git diff --name-only -- src/Generator/` as the check, which can never be empty for this plan — `ClassFactoryRenderer.cs` lives there. The checkable claim is that nothing under `src/Generator/Model/`, `src/Generator/Builder/`, or the transform changes, and that `IncrementalCacheTests` stays green untouched.

## Files

**Generator:** `ClassFactoryRenderer.cs` — the **five** `Local*` emission sites named by method in Step 2, plus new holder emission. `FactoryAttributes.cs` — XML contract only (Step 3a), no API surface change. Deliberately **not** touched: any model, builder, or transform.

**Tests:** `AssemblyAttributeEmissionTests.cs` (inverts the `:42` assertion, adds holder coverage), `verify-trimmed.sh` (eight assertions plus four prose blocks), `TrimTestEntity.cs` (two notes retire), plus a new harness target for class-level `[Execute]` (Step 2a).

**Docs:** the nine body-trimming anchors in `008-doc-anchor-inventory.md`, **plus** the holder anchors in Step 7b — `CLAUDE-DESIGN.md:760,771` and the attribute-target table, `docs/trimming.md:249`, and the skill's two-leg framing.

## Risks

- **The wrapper changes the emitted factory's method shape, and the throw escapes further than the first draft said.** `public async Task<T> LocalX(…)` becomes `public Task<T> LocalX(…)` plus a private core. The *signature* is unchanged, so no public API break. **Scope, verified:** only the guard moves — authorization checks, the `cTarget` cast, and every `GetRequiredService` failure stay in the core and still surface as a faulted `Task`. **But** because the local ctor binds the delegate property to the wrapper and the public entry point is itself non-async (`public virtual Task<T> FetchAsync(…) => FetchAsyncProperty(…)`), the synchronous throw escapes through **`I{X}Factory.FetchAsync`**, not merely through `Local*`. Risk is low — the message is asserted nowhere in the suite (deferred item 4) — but "no public API break" understates it, and it belongs in the release notes.
- **Five emission sites, one experiment.** Only the read path was exercised. The write path differs (`cTarget`, different lifecycle helpers), `LocalSave` is `virtual` and is wrapped by `async` publics — a split there needs more care than the read path — and class-level `[Execute]` is unconditionally async with no harness target at all.
- **`NormalizeWhitespace` has no error signal** — malformed emission yields mangled output, not an exception. Assert on exact fragments.
- **Two legs already carry holders with distinct prefixes**; a third must not collide with `NeatooFactoryRegistrar_` or `NeatooEventHandlerRegistrar_`, and a class carrying several factory attributes must not gain a CS0101 on top of its existing CS0111 (deferred item 15).

## Out of scope

- Deferred items 5, 9, 14, 15, 19 — each needs its own plan.
- Replacing the reflective registrar lookup with `[ModuleInitializer]` registration, which would delete this defect class entirely. Rejected here for load-order hazards; worth its own plan.
- The v1.7.0 release itself — TRIM-009 unblocks it; cutting it is the arc's close-out step.

---

## Current State (2026-08-14, implemented)

**The fix, both halves.** `ClassFactoryRenderer` gained one shared helper, `RenderLocalMethodOpening`, applied at all five guarded `Local*` emission sites. For a guarded `async` method it emits a **non-async wrapper** carrying the `IsServerRuntime` guard and forwarding to a `private async …Core`; for everything else it emits exactly what it emitted before. Alongside it, the class-factory assembly attribute now names a generated single-method holder, `NeatooClassFactoryRegistrar_{ClassName}`, instead of `{X}Factory`.

Both halves are required, and that is measured rather than argued — see the V3 row above for the wrapper alone, and the root inventory for why DAM keeps the private core alive without the holder.

**What did not change:** the delegate registrations and the local ctor's method-group assignment. Both reference the *wrapper*, whose post-guard call folds away, so the core is de-rooted without touching either. That was the prediction Step 1 was written to test, and it held.

## Test Evidence

| Claim | Artifact | Result |
|---|---|---|
| H2 falsified — its constructs are not necessary | `probe-h1h2-v1-async-probes-suppressed.txt` | async minus all four probes and the OCE arm → **still leaked** |
| H2 falsified — its constructs are not sufficient | `probe-h1h2-v2-sync-with-second-catch.txt` | sync plus catch arm and type-test → **still clean** (2 of 5 constructs; the awaiting probes cannot be grafted onto a sync body) |
| Guard relocation alone is insufficient | `probe-h1h2-v3-sync-wrapper-async-core.txt` + its addendum | markers unmoved; `<LocalFetchAsyncCore>d__` survives. **Cannot** attribute that to DAM vs. the wrapper's surviving call — both roots were live |
| Wrapper + holder clears the read and write legs | `probe-v4-wrapper-plus-holder.txt` | 8 markers flipped to absent; `<LocalFetchAsync>d__` / `<LocalInsert>d__` / `<LocalUpdate>d__` / `<LocalDelete>d__` / `<LocalSave>d__` all gone. **Does not cover class-`[Execute]`** — that target postdates this probe |
| …and the class-`[Execute]` leg | `gate-final-passing.log` | all five `Exec*` markers absent |
| The absences are trim results, not build artifacts | `probe-selfcheck-final-all-legs.txt` | every **body** marker PRESENT in the untrimmed build, **including all five class-`[Execute]` markers**. Supersedes `probe-v4-selfcheck-untrimmed.txt`, which covered 12 markers and none from the Exec leg |
| The holder actually forwards (silent-failure check) | `harness-final.log` | every factory resolves, including `Class [Execute] factory resolved: True`. `harness-v4-liveness.log` is the earlier run and stops at the save leg |
| The CI gate passes on the real artifact | `gate-final-passing.log` | exit 0, 10 named positive controls plus the UTF-16 control, no shape asserted PRESENT as a known leak |
| Knob values are recoverable per variant | `knob-values-per-variant.txt` | plan-review finding B6 |
| Apparatus was inert before use | `experiment-knobs.diff` | knobs at default reproduce HEAD's generated tree byte-for-byte |
| Full suite green, both solutions, both TFMs | `test-main-full.log`, `test-design.log` | 614+614 unit, 561+561 integration (5 pre-existing skips), 86+86 Design |

**Assembly size:** 52,224 bytes post-fix (`probe-v4-wrapper-plus-holder.txt`). No archived artifact records HEAD's *pre-fix* trimmed size — the 66,560 figure quoted in the first draft is **V2's** knob variant, not HEAD, and the comparison has been withdrawn rather than restated from memory.

**Red before green, proven not asserted — with a stated limit.** With the holder prefix broken and the wrapper split disabled, exactly three tests went red — `ClassFactory_EmitsAssemblyAttribute`, `ClassFactory_EmitsRegistrarHolder_ForwardingToFactory`, `ClassFactory_GuardedAsyncLocalMethod_SplitsIntoSyncWrapperAndAsyncCore` — while the sync-path control `ClassFactory_GuardedSyncLocalMethod_IsNotSplit` stayed green. **That run was filtered to this one test class (16 tests), so it is not a blast-radius statement** about the other 598. xUnit also aborts each test at its first failing assertion, so the later assertions in those tests — including the `DoesNotContain` regression assertion — were not individually observed red. Recorded rather than papered over.

**Blast radius on existing tests: one**, exactly the inversion plan review predicted at `AssemblyAttributeEmissionTests.cs:42`. Its original intent is preserved — the attribute is still emitted and still names the correct type; the correct type changed.

**Not covered here:** a dedicated emission assertion for the async guarded `Can*` site. The shape needs `[AspAuthorize]` policy auth, whose references `DiagnosticTestHelper.BuildReferences()` does not carry; an attempt using `[AuthorizeFactory<T>]` produced an *unguarded* async Can and was removed rather than kept. The site is exercised by `Design.Domain.Aggregates.SecureOrder` and `RemoteFactory.AspNetCore.TestLibrary`, both of which emit `LocalCan*Core` and pass. Reason recorded at the test file.

## What this plan deliberately did not do

- **The interface-factory leg.** It carries the same inside-the-async guard and still points its attribute at `{ImplName}Factory`, so it shares both mechanisms and received neither fix. Taking it on would have grown a plan whose arc was already flagged as over-running, and deferred item 19 makes the leg structurally unmeasurable from a client-side harness anyway. Deferred as **item 20** — with the published claim corrected from "Interface factory | Yes" to "not established" in both the skill and `CLAUDE-DESIGN.md`, because deferring work is acceptable and shipping a false claim is not.
- **Narrowing the DAM** on `NeatooFactoryRegistrarAttribute`. Rejected at TRIM-008's plan review and still rejected; a prebuilt library with an `internal static` registrar would silently stop registering on a trimmed client.
- **Design-project requirements verification** (Step 7B per `CLAUDE.md`) — deferred to the release step via existing item 11, now stated here rather than left to surface at close-out.
