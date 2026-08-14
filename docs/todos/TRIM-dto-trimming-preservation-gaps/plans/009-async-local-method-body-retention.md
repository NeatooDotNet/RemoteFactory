# TRIM-009 — Async `Local*` factory-method body retention

**Plan #:** 009
**Date:** 2026-08-13
**Related Todo:** [../todo.md](../todo.md)
**Status:** Stub
**Last Updated:** 2026-08-13
**Plan-review opt-in:** Yes (same grounds as TRIM-008 — a false IP-protection guarantee, and the remedy is unknown at stub time)
**Code-review opt-in:** Yes (behavior-changing generator work, if the remedy turns out to be generator-side)

> **Stub.** Scope and the measured evidence only. Steps, Acceptance, Current State, and Test Evidence flesh out at this plan's turn, per the iterative-todo workflow. Nothing below prescribes a remedy — the cause is measured, the fix is not yet designed.

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
