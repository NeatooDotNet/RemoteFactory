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

**Why the TRIM-008 remedy does not apply.** TRIM-008's defect is "the assembly attribute names a *consumer* type". Here the attribute correctly names the generated `TrimSaveTargetFactory`. And DAM is not the only root — the registrar's own unguarded `AddScoped<{X}Delegate>` closures root `LocalSave` independently, so a forwarding holder would change nothing. Confirmed by measurement: the static leg was already fixed when this probe ran, and these markers were unaffected.

**Baseline inheritance.** This plan does **not** need its own pre-fix probe. The 2026-08-13 measurement above *is* the baseline, captured before any fix to this leg existed, with markers proven visible by a self-check against the untrimmed assembly. Harness targets, per-leg ports, and the probe script all land with TRIM-008.

---

## Open questions for the design turn

- Is the remedy generator-side (guard the delegate registrations, restructure the emitted guard so the fold is not inside `MoveNext`, keep server-only work out of async `Local*` bodies) or configuration-side (an ILLink feature/substitution the generator emits)? Unknown; do not assume.
- Does the same retention affect **async `[Execute]`** static-factory bodies and **async relay handlers** once TRIM-008 lands? The harness's `_DoWork`/`_ProcessRecord` are `Task`-returning but not `async`, so TRIM-008's green result may not generalize. Worth an explicit async target before claiming those legs clean.
- Does `LocalSave`'s routing keep `LocalInsert`/`LocalUpdate`/`LocalDelete` rooted even if their own registrations were guarded?
- CI gate: which of the new Save/Can\* markers can be asserted absent once this lands, and what is the durable positive control for them.
