# EXRM-001 — Plan Review

**Date:** 2026-09-08
**Reviewer:** plan-reviewer (budget: deep)
**Object:** `plans/001-static-delegates-obey-remote.md` as drafted at Step 2, with Current State filled
**Verdict:** CONCERNS — one veto-tier finding, fixable by amendment

## Pass A — documented requirements

Docs consulted: `docs/attributes-reference.md`, `docs/trimming.md`, `src/Design/CLAUDE-DESIGN.md`, repo `CLAUDE.md`, `todo.md`.

**Veto-tier:** none. Beyond the authorized reversal of the "decorative" rule, no other documented rule is contradicted. NF0102, NF0103, and NF0106 are untouched; the NF0105 static exemption lives in `BuildClassFactory` only (`FactoryModelBuilder.cs:195`), so `BuildStaticFactory` never fires it and a `[Remote] public static ExecuteOp` combination target compiles.

**Callout A1.** `docs/trimming.md:35` states unconditionally that static-factory `[Execute]` registrations are guarded and the bodies are trimmed. Step 4 makes that false for bare delegates, and the sentence is distinct from the decorative claim. Affects AC-5 (Must) and AC-3 (Must). Reachable by a reader following `trimming.md` after v1.9.0.

## Pass B — codebase

Files examined: generator model, builder, and all three renderers; runtime handler, service registration, event marker delegate, entry call, DTO constructor registry; integration containers, Execute behavior tests, remote Execute service tests and targets, every Events target, trimming targets; Design patterns and tests.

**Reality check.** Direction is right. The two weld lines are the only place remoteness is inferred from Execute; `Transform.cs:168` is NF0103 only. No isTask/isAsync consequence for static delegates. No duplicate-registration or ordering hazard. `FactoryEntryCall.RunAsync` is null-scheduler tolerant. Class-level rendering needs no edit for the public shape. Verified, not assumed: every client-scope Execute target in both solutions carries `[Remote]`, and all four trimming targets do. No sacred test breaks.

**Veto-tier V1.** The wire-refusal allow-list, as scoped, refuses every class-factory and interface-factory remote request. The handler (`HandleRemoteDelegateRequest.cs:92`) is shared by all three shapes; class factories register delegates at `ClassFactoryRenderer.cs:1620-1630`, interface factories at `InterfaceFactoryRenderer.cs:501`; neither would be declared. The plan's constraint "the handler still serves every remote delegate" cannot hold under its own Scope. Affects AC-1 (Must), bullet 4. Reachable by running the existing integration suite after Step 5.

**Callout B1 (Must).** An `[Execute] internal static` on a class factory with no `[Remote]` flips the generated factory interface from public to internal through `ClassFactoryModel.cs:62` and `:66`. No in-repo instance. Affects AC-1 (Must). Reachable by a consumer writing that shape.

**Callout B2 (Must).** Step 7's "assert a remote request happened" has no existing hook; `MakeSerializedServerStandinDelegateRequest` keeps no counter. Affects AC-2 (Must), bullet 5.

**Callout B3 (Must).** Acceptance bullet 5's first clause ("every Remote-mode combination target carries `[Remote]`") is a source-shape assertion, not behavior. Affects AC-2 (Must).

**Callout B4 (lesser).** A bare delegate's `[Service]` resolution is lazy inside the lambda, so a server-only dependency surfaces as a DI failure at call time on the client. Intended, but no Acceptance bullet names it. Affects AC-1 (Must) indirectly.

**Registry precedent.** `DtoConstructorRegistry` is the shape to mirror: process-global, populated from generated registrars, no reflection, idempotent. `RaiseFactoryEventRemote` is registered separately at `AddRemoteFactoryServices.cs:98`.

**Theoretical (not triaged).** A static `[Execute]` whose `[AuthorizeFactory]` method carries `[Remote]` would be forced remote by the three-way rule while static factories render no enforcement; no in-repo instance; preserves today's behavior.

## Triage (user decisions 2026-09-08)

| # | Finding | Affects | Priority | Size | Disposition |
|---|---|---|---|---|---|
| V1 | Allow-list refusal would reject class and interface remote calls | AC-1 | Must | plan | **Amend** — refuse-list of bare static delegates, mirroring `DtoConstructorRegistry`; served as unknown on the wire, real reason logged server-side |
| B1 | Bare `[Execute] internal static` flips the factory interface to internal | AC-1 | Must | punch | **Punch → EXRM-004** as a doc sentence (same rule as every other internal operation); Could-tier pin noted on EXRM-002 |
| B2 | No hook to observe a remote request from the client scope | AC-2 | Must | punch | **Amend** Step 7 — per-scope call count on the serialized stand-in |
| B3 | Bullet 5 asserted generated source, not behavior | AC-2 | Must | punch | **Amend** — bullet 5 is now "one remote request per client-scope call, none per local-scope call" |
| B4 | No bullet for the server-only failure on the client | AC-1 | Must | punch | **Amend** — bullet 7 added, Must |
| A1 | `docs/trimming.md:35` states bodies are always trimmed | AC-5, AC-3 | Must | punch | **Already covered** — in EXRM-004's docs inventory (`trimming.md:35` and `:20-26`); note only |
| — | Bullet 3 tagged `[unit]` for a DI-resolution check | AC-1 | Must | punch | **Amend** — `[integration]` (orchestrator's own correction) |

Theoretical item not triaged. Plan Amendments entry written on the plan; Discovery Log entry on the todo.
