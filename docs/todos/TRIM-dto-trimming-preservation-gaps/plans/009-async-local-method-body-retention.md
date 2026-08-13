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

| Shape | Guard | Reaches server-only code via | `async` | Result |
|---|---|---|---|---|
| static `[Execute]` | `if (IsServerRuntime) { … }` wrapping, in a non-async registrar | direct static call | yes | clean |
| relay handler | same wrapping guard | direct static call | yes | clean |
| interface factory | `if (!IsServerRuntime) throw` early | **the interface** (`GetRequiredService<T>()` then `target.M()`) | yes | clean |
| class factory `Create` | `if (!IsServerRuntime) throw` early | direct call on the concrete type | **no** | clean |
| class factory `Insert`/`Update`/`Delete` | `if (!IsServerRuntime) throw` early | direct call on the concrete type | **yes** | **LEAKS** |

**This narrows the hypothesis; it does not falsify it.** None of the three clean async shapes is the leaking shape. The static and relay legs put their guard in a *wrapping* block inside a non-async method, so folding deletes the registration whole. The interface leg reaches its implementation through an interface, so no implementation body is statically reachable and its absence follows from indirection rather than from folding — that leg cannot detect a folding failure at all.

The leak so far requires **all three together**: an early-throw guard, an `async` method, and a direct call to a concrete type. The single-variable comparison that isolates `async` is still the class-factory pair (`Create` sync clean vs `Insert` async leaking) — same guard, same call shape, same rooting, same DAM.

**For the design turn:** do not treat "async is the cause" as established. The controlled pair says `async` is the differing variable *within the class-factory leg*; whether the operative mechanism is the async state machine, the early-throw guard shape, or their combination is not settled, and the remedy differs for each. Re-derive it against the artifact before building on it.

**Baseline inheritance.** This plan does **not** need its own pre-fix probe. The 2026-08-13 measurement above *is* the baseline, captured before any fix to this leg existed, with markers proven visible by a self-check against the untrimmed assembly. Harness targets, per-leg ports, and the probe script all land with TRIM-008.

---

## Open questions for the design turn

- Is the remedy generator-side (guard the delegate registrations, restructure the emitted guard so the fold is not inside `MoveNext`, keep server-only work out of async `Local*` bodies) or configuration-side (an ILLink feature/substitution the generator emits)? Unknown; do not assume. Whichever it is, check whether the DAM root also has to be addressed — see the corrected paragraph above.
- ~~Does the same retention affect **async `[Execute]`** and **async relay handlers**?~~ **Answered 2026-08-13: no.** Async targets were added to the harness for both legs plus an async interface-factory method, and all trim clean. See the table above for why none of them is the leaking shape.
- Would changing the emitted guard from `if (!IsServerRuntime) throw …` to a *wrapping* `if (IsServerRuntime) { … }` block — the shape the static and relay legs already use and which demonstrably folds away — fix this without touching DAM or the registrations? That is the cheapest candidate the measurement suggests, and it is untested.
- Does `LocalSave`'s routing keep `LocalInsert`/`LocalUpdate`/`LocalDelete` rooted even if their own registrations were guarded?
- CI gate: which of the new Save/Can\* markers can be asserted absent once this lands, and what is the durable positive control for them.
