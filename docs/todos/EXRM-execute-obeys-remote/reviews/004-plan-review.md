# EXRM-004 — Plan Review

**Plan:** [`plans/004-contract-in-design-docs-skill.md`](../plans/004-contract-in-design-docs-skill.md)
**Reviewer:** `business-requirements-reviewer` (the plan's declared opt-in; user decision 2026-09-08 — this reviewer only)
**Date:** 2026-09-08
**Budget:** tight
**Verdict:** **CONCERNS** — no documented-rule contradiction and no excluded feature, but five callouts touch the AC-5 Must: one planned sentence describes a combination the generator cannot produce, one rule statement is false for a shape Step 9 simultaneously pins, one Must acceptance bullet is already red today, and two collide with documented conventions.

The reviewer's report arrived in three parts (two truncations, each followed by a capped resend). It is reproduced here in full, lightly reflowed; nothing was dropped.

---

## Veto-tier

None. No Design Debt row (`src/Design/CLAUDE-DESIGN.md:1093-1099`) covers `[Execute]`, `[Remote]`, or NF0105; nothing in the plan implements a deferred feature; the arc's Out of Scope and the Dismissed rows are respected.

## Callouts affecting AC-5 (Must)

**M1 — Step 1's inherited-`[Remote]` sentence describes something that cannot occur.** `[Execute]` is static on both shapes (NF0103), and static methods are never overridden, so no derived member can carry an inherited `[Remote]` over a redeclared `[Execute]`. Worse, the generator reads `methodSymbol.GetAttributes()` (`src/Generator/FactoryGenerator.Types.cs:701`), which returns declared attributes only — Roslyn does not walk overridden members — so `RemoteAttribute`'s `Inherited = true` reaches no generator decision for any operation. *Reachable by:* user action (a reader follows the sentence). *Affects: AC-5 (Must).* Seam: drop the clause from Step 1; the `attributes-reference.md:416` row is a separate correction, not this plan's.

**M2 — "bare `[Execute]` = one unguarded local registration in every mode" is false for `internal static [Execute]` on a class factory, which Step 9 pins in the same plan.** `ClassFactoryRenderer.cs:843-844` passes `isServerOnly: method.IsInternal || method.IsRemote`, so the internal bare case *does* get the `IsServerRuntime` guard and throws on a client. As drafted, Framework Alignment bullet 5, Step 1, Step 2's new trimming rows and Step 9's one-sentence doc statement will contradict each other. *Reachable by:* observed failure (a user writes `internal static` per Step 9's sentence and gets "Server-only method called in non-server runtime"). *Affects: AC-5 (Must).* Seam: state the rule as bare **`public static`** = unguarded, and give the internal case its own row/sentence naming the guard.

**M3 — Acceptance bullet 7's "the skill contains no reference to a path outside its own directory" is already false before any edit.** Every MarkdownSnippets embed in the skill emits an attribution link to the reference app — e.g. `skills/RemoteFactory/references/static-factory.md:54` links `/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/StaticFactorySamples.cs`. Step 5 adds one more. *Reachable by:* the plan's own grep check at Step 10. *Affects: AC-5 (Must).* Seam: scope the bullet to prose/cross-references, explicitly excluding mdsnippets attribution `<sup>` links.

**M4 — Step 5's `skill-*` region for a snippet the docs also embed contradicts the documented region convention.** Root `CLAUDE.md` ("Region naming convention"): `skill-*` = regions created specifically for skill examples; **no prefix** = shared with docs. Step 4 has the docs embedding the new bare sample and Step 5 has the skill embedding it through a `skill-*` region — one region cannot be both, so this forces either a convention violation or a duplicated sample. *Reachable by:* user action (next author follows the convention and cannot find the shared region). *Affects: AC-5 (Must).* Seam: one unprefixed region embedded in both, per convention.

**M5 — the authorization sentence must be scoped to the class shape for the "runs alongside / is consulted" half.** "Forces remote" is true on both shapes by construction (`FactoryModelBuilder.cs:458-460` class, `:527-529` static). Enforcement is pinned only for the class shape — `LocalClassExecuteTests.cs:125,138,198,244,259` — and `FactoryModelBuilder.cs:526` records that static factories render no authorization enforcement (#91, dismissed, not re-raised here). The static `LocalExecuteTests.cs` has no auth tests at all. *Reachable by:* live caller — the snippet immediately above the planned prose, `docs/authorization.md:162-166`, is a **static-factory** `[Remote, Execute]` carrying two `[AspAuthorize]`. *Affects: AC-5 (Must).* Seam: Step 3's authorization sentence — say "forces remote on both shapes; on class factories the policy is also enforced", and do not write a sentence a reader will apply to that adjacent static snippet.

## Should/Could callouts

**S1 — recon error that wrongly constrains the new sample.** Notes claim the Blazor client "registers no domain services (`Program.cs` is `AddNeatooRemoteFactory` only)". `src/docs/reference-app/EmployeeManagement.Client.Blazor/Program.cs:26` calls `RegisterMatchingName(typeof(Employee).Assembly)`, so any Domain `IX`/`X` pair resolves client-side — `ISalaryCalculator`/`SalaryCalculator` (`Domain/Samples/Services/ServiceInjectionSamples.cs:147`) is an existing, client-safe, pure-computation fit. The bare sample should take a `[Service]`; "resolves from whichever container runs it" is half the rule being documented.

**S2 — `src/Design/CLAUDE-DESIGN.md:368`** answers "Should a `[Remote]` method be `internal`?" with "Yes, always — NF0105 error if `public`", with no static carve-out. If AC-6 lands on "kept with a stated reason", that row needs the caveat; Step 10 only adds a new row.

**S3 — no Design sample of `internal static [Execute]` exists**, and `ClassFactoryWithExecute.cs:128` explicitly binds the class-Execute sample to `public static`. Stating Step 9's rule from generator behavior alone is thin under "Design is the source of truth" — add a Design sample or label the sentence as generator behavior with the `[unit]` cite.

**S4 — `docs/factory-operations.md:305,439` and `skills/RemoteFactory/references/class-factory.md:399`** say class-level Execute "must be `public static`". They are in the inventory, but Steps 3/5 treat them only as the two-context intro; they must also be reconciled with Step 9's internal sentence.

**S5 — the skill trimming table's class row carries the same old claim as the static row.** `skills/RemoteFactory/references/trimming.md:141` ("Class-level `[Execute]` | Yes, from v1.7.0") needs splitting by attribute exactly as `:138` does; Step 5 names only "the shape table's static row".

## Theoretical (not triaged)

- `skills/RemoteFactory/SKILL.md:73` ("Can `[Execute]` go on a class factory? Yes, if `public static`…") needs the local/remote distinction, not just visibility. *(Raised again under Q7; folded into S4's amendment since Step 5 edits that table anyway.)*
- Notes' corrected cite "`AllPatterns.cs:383-404`" for the void-return rule looks off — the `DID NOT DO THIS: Allow void-returning [Execute]` block sits at ~402-409.
- "The reference app's bare `_TransferEmployee` fails on the client the moment it is called" overstates: the client passes only `typeof(Employee).Assembly` to `AddNeatooRemoteFactory`, so the Application factory is not registered client-side at all.
- Class-level `[Execute]` is emitted async unconditionally (`ClassFactoryRenderer.cs:842`), so no "class, sync" guard-table cell exists to document.

## Answers to the brief's questions

**Q1.** The combination cannot occur: NF0103 forces `[Execute]` static, statics are never overridden, and `FactoryGenerator.Types.cs:701` reads only declared attributes, so `RemoteAttribute`'s `Inherited = true` (`FactoryAttributes.cs:26`) reaches no generator decision at all. Nothing in Design is contradicted — the sentence is simply a new false claim on the page meant to be the rule's single home.

**Q2.** Yes, it would overclaim. The harness measures three pairs — static sync, static async, class async (`verify-trimmed.sh:395-397`; `reviews/003-evidence/README.md:17-21`) — and every bare half is `public static`/`private static` (`ClassExecuteLegTarget.cs:117`). The unmeasured cell that is also *contrary* is bare `internal static [Execute]` on a class factory, guarded at `ClassFactoryRenderer.cs:844`; interface factories stay explicitly unproven (`verify-trimmed.sh:418-421`).

**Q3.** Scope it (M5). New beyond M5: the enforcement asymmetry is stated in the generator itself — `FactoryModelBuilder.cs:526` ("Static factories render no authorization enforcement today (see issue #91); the auth terms only decide placement") — so the scoping the sentence needs is already written down in-repo and can be cited rather than re-derived.

**Q4.** The only statement of intended scope is `docs/plans/completed/remote-requires-internal.md:230-252`: the exemption was reasoned entirely as "class-factory `[Execute]` must be `public static`, so exclude it", then implemented as `!method.IsStaticFactory` — which `FactoryGenerator.Types.cs:581` (`IsStaticFactory = methodSymbol.IsStatic`) makes *every* static method, not `[Execute]`. No published rule states the broader scope: `CLAUDE-DESIGN.md:368` and `docs/attributes-reference.md:189` mention only the exemption's existence. "Kept with a stated reason" contradicts nothing and is free — it only requires the caveat added at those two sites. Narrowing to `[Execute]`-only contradicts no documented rule either (no doc or Design sample uses `[Remote] public static [Create]/[Fetch]` on a class factory), but it emits a new compile error for that shape — a public-API change the plan's own Constraints exclude and EXRM-005 would have to carry. Stop-and-ask is the right posture; `NF0105Tests.cs:161` pins only the Execute case, so the breadth is currently untested either way.

**Q5.** Covered (M2, S3, S4).

**Q6.** New beyond S1: nothing blocks the sample's location — `MinimalAttributesSamples.cs:88-96` already hosts the docs' `attributes-execute` region and `ExecuteSamples.cs:6` declares itself not a snippet source — so only the region *naming* (M4) is at issue.

**Q7.** Covered (M3, M4, S5), plus `SKILL.md:73` states the class-Execute answer as a visibility rule only and needs the local/remote distinction.

## Read report

Beyond brief: `Client.Blazor/Program.cs` + `.csproj`; `ClassFactoryRenderer.cs:120-148,377-407,790-856`; `FactoryGenerator.Types.cs:694-702`; `FactoryModelBuilder.cs:444-540`; `ClassExecuteLegTarget.cs:55-125`. Also: `CLAUDE-DESIGN.md:363-384,1089-1100`; `skills/.../static-factory.md` (full); `SKILL.md:1-84`; Domain sample-service inventory; integration `Local*ExecuteTests` names. Unused: `docs/client-server-architecture.md`, `docs/decision-guide.md`, `reviews/001`–`003`, `CLAUDE-DESIGN.md:944-948,1074`. Budget went to the generator seams (visibility guard, NF0105 breadth, attribute inheritance) instead.

---

## Orchestrator verification (before triage)

Three claims checked against the tree before any disposition was proposed: **M2** — `ClassFactoryRenderer.cs:844` reads `isServerOnly: method.IsInternal || method.IsRemote`; **S1** — `Client.Blazor/Program.cs:26` calls `RegisterMatchingName(typeof(Employee).Assembly)`, and `ISalaryCalculator`/`SalaryCalculator` sit at `ServiceInjectionSamples.cs:139,147`; **Theoretical 3** — both `Program.cs` files pass only `typeof(Employee).Assembly`, so an Application-assembly factory is never registered on the client. All three hold. **S2** confirmed at `CLAUDE-DESIGN.md:368`.

## Triage

| # | Finding | Affects | Priority | Size | Disposition |
|---|---|---|---|---|---|
| M1 | Step 1's inherited-`[Remote]` clause describes a combination that cannot occur | AC-5 | Must | punch | **Amend** — clause dropped from Step 1. The `attributes-reference.md:416` "derived methods inherit remote execution" row is dismissed on the todo: a `[Remote]`-inheritance claim, not the `[Execute]` contract; the generator reads declared attributes only, so it is inert rather than harmful |
| M2 | Bare `internal static [Execute]` on a class factory is guarded, contradicting the plan's rule sentence and Step 9 | AC-5 | Must | punch | **Amend** — the rule is stated for `public static` on both shapes; `internal static` without `[Remote]` on a class factory is server-only like every other internal operation (guarded, trimmable, not promoted). Step 9's pin asserts guard and non-promotion together; Step 2's trimming rows carry the internal case as its own row |
| M3 | Bullet 7's skill self-containment check is already red: mdsnippets attribution links | AC-5 | Must | punch | **Amend** — bullet 7 scoped to prose and cross-references, mdsnippets `<sup>` attribution links excluded |
| M4 | A `skill-*` region cannot also be the docs' shared region | AC-5 | Must | punch | **Amend** — one unprefixed region in `MinimalAttributesSamples.cs`, embedded by docs and skill |
| M5 | Authorization sentence: enforcement is pinned for the class shape only; the adjacent snippet is static | AC-5 | Must | punch | **Amend** — "forces remote on both shapes; on class factories the policy is also enforced", citing `FactoryModelBuilder.cs:526` and #91; written so it cannot be read onto the static snippet above it |
| S1 | Recon error: the client does register Domain `IX`/`X` pairs; a client-safe service exists | AC-5 | Must (inherited) | punch | **Amend** — Notes corrected; the new bare sample takes `ISalaryCalculator`, so the snippet shows local service resolution, which is half the rule |
| S2 | `CLAUDE-DESIGN.md:368` "Yes, always" needs the static carve-out | AC-5, AC-6 | Must | punch | **Amend** — Step 10 caveats that row in the same edit that adds the new one |
| S3 | No Design sample of `internal static [Execute]`; Step 9 states a rule from generator behavior alone | AC-5 | Must | punch | **Amend** — Step 9 adds the server-only shape to `ClassFactoryWithExecute.cs` with a Design test (runs on the server, server-only on the client), beside the `[unit]` generator pin; Design stays the source of truth |
| S4 | "must be `public static`" at `factory-operations.md:305,439`, `class-factory.md:399`, `SKILL.md:73` must reconcile with the internal case | AC-5 | Must | punch | **Amend** — Steps 3 and 5 name those sentences: `public static` to run where the factory resolves, `internal static` for server-only |
| S5 | Skill trimming table's class row carries the same old claim | AC-5 | Must | punch | **Amend** — Step 5 splits both rows by attribute |

Theoretical items: the `AllPatterns.cs` cite and the "fails on the client" wording are corrected in the same amendment (Notes / Intent / Punchlist); the "no class-sync cell" note shapes Step 2's table (rows by shape and attribute, no sync/async axis). Not triaged, per the workflow.

**User decision (2026-09-08):** Accept all as proposed. Amendments applied to the plan in the same commit; see its Plan Amendments entry of this date.
