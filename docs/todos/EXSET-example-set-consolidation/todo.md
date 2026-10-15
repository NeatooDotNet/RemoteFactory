# One Compiled Example Set, and the Audit Against It

**ID:** EXSET
**Type:** Enhancement
**Status:** In Progress
**Priority:** High
**Created:** 2026-10-11
**Last Updated:** 2026-10-15
**Initial split:** 12 plans
**Plan cap:** 18 — counts plan numbers issued, including Abandoned and Retired. Issuing a number above the cap is a stop-and-ask.
**Arc branch:** EXSET — cut from main at Step 1; every plan and punchlist branch PRs into it; it PRs into main at Step 8.

---

## Goal

RemoteFactory has one compiled example set, `src/Design`. Every C# block in `docs/`, `README.md` and the skill is either a snippet embedded from it or a short block labelled WRONG. Design shows every shape in `docs/VisionAlignment.md` section 3 that has no example today, and CI builds and tests it. The docs, the README, `CLAUDE.md` and `CLAUDE-DESIGN.md` agree with VisionAlignment sections 0 and 2. Each of the 86 findings in the RemoteFactory consistency audit has one recorded outcome: fixed here, retired with its snippet source, or handed to the skill rebuild. `src/docs/reference-app` and `src/docs/samples` are gone.

## Acceptance Criteria

- [ ] **AC-1** · Must — CI builds and tests `Design.sln` alongside the main solution.
- [ ] **AC-2** · Must — Design has compiled, tested examples of a context, a read model, a domain-service interface factory, the R-D child write cascade with a public child Create, create-or-fetch, and a plain-record value object beside one that needs a service.
- [ ] **AC-3** · Must — Every C# embed in `docs/`, `README.md` and the skill comes from `src/Design`.
- [ ] **AC-4** · Must — Each of the 86 audit findings has one recorded outcome, and every finding outside the skill's prose is fixed against the rulings.
- [ ] **AC-5** · Should — LazyLoad appears in no doc page, README section or skill reference.
- [ ] **AC-6** · Must — `src/docs/samples` and `src/docs/reference-app` are deleted.
- [x] **AC-7** · Could — A `.claude/rules` file names Design as the snippet source and `.gitattributes` forces LF for `.md`.

## Out of Scope

- The skill's prose rewrite: Todo 2 (R-B). This todo re-points the skill's embeds to Design and removes `references/lazyload.md`; skill-prose findings are recorded as handed to Todo 2.
- Analyzers for a non-public setter, a missing `partial`, and the class-level `[Execute]` return type: Todo 3 (R-L).
- Removing LazyLoad from the library, and Design's `LazyLoadExample` with its tests: #114.
- The Neatoo repository.

## Sources of record

- Rulings: `docs/VisionAlignment.md` sections 0.1 and 0.2 (D3–D19, R-A–R-P).
- Findings: `Neatoo/docs/todos/consistency-audit/audit-remotefactory.md` (86 rows at RF `2a7b438`); the per-finding outcomes live in `audit-ledger.md` in this folder (EXSET-001).

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-ci-rule-ledger](./plans/001-ci-rule-ledger.md) | Design in CI, snippet rule, findings ledger | AC-1, AC-4, AC-7 | Done | #118 |
| 002 | [002-order-aggregate-cascade](./plans/002-order-aggregate-cascade.md) | Order aggregate: R-C, R-D cascade, D14, R-E | AC-2, AC-4 | Done | #121 |
| 003 | [003-service-readmodel-context](./plans/003-service-readmodel-context.md) | Domain service, read model, context | AC-2, AC-4 | Done | #125 |
| 004 | [004-create-or-fetch-value-objects](./plans/004-create-or-fetch-value-objects.md) | Create-or-fetch and value objects | AC-2, AC-4 | Done | #129 |
| 005 | [005-readme-getting-started](./plans/005-readme-getting-started.md) | README, getting started, framing pages | AC-3, AC-4, AC-6 | Done | #130 |
| 006 | [006-aspnetcore-modes-serialization](./plans/006-aspnetcore-modes-serialization.md) | ASP.NET Core integration over HTTP, and setup | AC-3, AC-4 | Done | #132 |
| 007 | [007-factory-operations](./plans/007-factory-operations.md) | Factory operations page | AC-3, AC-4 | Done | #136 |
| 008 | [008-save-interfaces](./plans/008-save-interfaces.md) | Save operation and interfaces reference | AC-3, AC-4, AC-5 | Done | #137 |
| 009 | [009-authorization](./plans/009-authorization.md) | Authorization: Design auth, server authentication | AC-3, AC-4 | Done | #140 |
| 010 | [010-service-injection-skill-embeds](./plans/010-service-injection-skill-embeds.md) | Service injection and interface factory pages | AC-3, AC-4 | Done | #143 |
| 011 | [011-events-trimming-lazyload](./plans/011-events-trimming-lazyload.md) | Events, trimming, LazyLoad pages, appendix | AC-4, AC-5 | Draft | — |
| 012 | [012-retire-reference-app](./plans/012-retire-reference-app.md) | Retire reference app; path and design sweep | AC-4, AC-6 | Draft | — |
| 013 | [013-modes-serialization](./plans/013-modes-serialization.md) | Factory modes and serialization pages (after 006) | AC-3, AC-4, AC-5 | Done | #134 |
| 014 | [014-attributes-reference](./plans/014-attributes-reference.md) | Attributes reference (split from 009) | AC-3, AC-4 | Done | #142 |
| 015 | [015-skill-embeds](./plans/015-skill-embeds.md) | Skill-only embeds (split from 010) | AC-3, AC-4 | In Progress | — |

---

## Punchlist

- [ ] VisionAlignment says "162 embeds"; recon counts 155 markers rendering 165 blocks · `docs/VisionAlignment.md` §1.1, §7 · done when the count matches · AC-4 · Must
- [x] `AuthorizedOrder`, `ParamAuthOrder`, `SecureOrder` `[Remote, Create]` with no server need (D14) · Design `Aggregates/` · done when local or justified, in EXSET-009 · AC-4 · Must → EXSET-009: all three Creates are local `public [Create]`
- [ ] `trimming.md`'s "IFactorySaveMeta Preservation" assumes private setters and `[JsonInclude]` (R-M) · `docs/trimming.md` · done when rewritten against public setters, in EXSET-011 · AC-4 · Must

## Dismissed

- EXSET-005 · `scripts/extract-snippets.ps1` defaults to a `docs/samples` path that no longer exists · nothing calls the script
- EXSET-005 · `VisionAlignment.md:69` still lists `src/docs/samples` · it is the as-found inventory the rulings were made against
- EXSET-004 · `Relay_ServerOnlyFlag_EventNotRelayedToConsumer` waits a fixed 50 ms before its absence check · inherited; Design's `InMemoryAggregatorRelay` has no invocation count to wait on
- EXSET-004 · `ValueObject_SurvivesSerializationRoundTrip` uses only USD, the default · the seed holds only USD, and `Money`'s constructor validates the currency on both sides
- EXSET-002 · `OrderLineList.IsNew`/`IsDeleted` read-only, against R-M's `get; set;` · R-M is for objects whose own state crosses the wire; a collection's properties never cross (#119)

---

## Discovery Log

- **2026-10-15 · EXSET-010 · Re-split.** Recon counted about 30 reference-app embeds in 010's scope. The two docs pages and the skill's `service-injection.md` share regions, so they stay on 010. The skill-only `skill-*` embeds share none, and move to EXSET-015. That is 15 of the 18 plan numbers. Serves AC-3, AC-4.
- **2026-10-15 · EXSET-009 · Amend.** The code review found that `[AuthorizeFactory<T>]` on a static factory's `[Execute]` was never enforced. The generator used the auth methods only to decide whether the call goes remote, so a rule there compiled and ran for anyone. Serves AC-3, the page's refusal outcomes. Keith: fix it in the generator on 009. The local registration now calls the matching auth methods before `[AspAuthorize]` and the method, and a refusal throws `NotAuthorizedException`. It also registers the auth class, as class and interface factories do. `StaticAuthorizeFactoryTests` (integration) fail 4 of 5 without the fix (`reviews/009-static-authorizefactory-unfixed-test.log`). The release notes for the next version owe a line for this.
- **2026-10-15 · EXSET-009 · Stop → Keith.** The defect in the entry below was already fixed on `main` by #127 (v1.10.6), with #91 and #117 (v1.10.5); the arc had never taken `main`. Serves AC-3. Keith: merge `main` into EXSET first (#139, merged); the duplicate library edit is dropped and #138 closed.
- **2026-10-15 · EXSET-009 · Amend.** The plan review found that an anonymous caller passes a class factory's `[AspAuthorize]`. With no authenticated user, `AspAuthorize` returns a null or empty message, which converts to `Authorized(true)`. Serves AC-3 (the page's denial outcomes). Keith: fix it in the library on EXSET-009, with tests and an issue for the record.
- **2026-10-15 · EXSET-009 · Re-split.** Recon found the stub held two deliverables of different shape. One is authorization: Design's auth types are flag-driven scaffolding, `SecureOrder` is untested, and Design.Server has no authentication. The other is the attributes page, a re-pointing job. Serves AC-3 and AC-4. Keith: rework `AuthorizedOrder` onto an injected `ICurrentUser`, give Design.Server bare `AddAuthentication`, and split the attributes page out as EXSET-014 (14 of 18).
- **2026-10-15 · EXSET-013 · Stop → Keith.** The reference probe found that in the Ordinal format (the default), `[Factory]` objects lose reference identity: a shared reference arrives as two copies, and a back reference throws "possible object cycle". Named preserves both. Serves AC-4 (serialization.md's reference claims). Keith: document it per format and file a bug, #133 (`critical`); the probe becomes a known-bug regression test.
- **2026-10-15 · EXSET-006 · Amend.** Over HTTP, the first natural 500 assertion found `HttpRequestException.StatusCode` null on the client: the library built the exception from the message alone. Serves AC-3 (the page's failure table). Keith: fix it in the library on 006, pinned by unit tests; it ships with the arc's merge to main. The same plan's first HTTP run also found Design.Server unable to start in Development, because `RegisterMatchingName` registers a concrete `TenantTokenService` that DI can't build. Design now registers it itself; the library side is #131 (techdebt).
- **2026-10-15 · EXSET-006 · Re-split.** The plan review found two deliverables. One is the HTTP tier with the ASP.NET Core page. The other is the serialization and modes pages: they need only the in-process harness, and they open with a probe that may stop on a library ruling. Keith: split. EXSET-013 takes serialization and modes, and follows 006 because it reuses 006's format and logging regions. Serves AC-3. 13 of 18 plan numbers are issued.
- **2026-10-13 · EXSET-004 · Amend.** The relay collector leaked events across remote calls sharing a server scope (Design's harness shares one; ASP.NET does not). Serves AC-2 (the reshaped relay test). Keith: fix in the library on EXSET-004; the harness's shared scope is #128 (techdebt). Ships to NuGet with the arc's merge to main.

---

## Skipped Steps

- EXSET-014 plan-reviewer: the plan re-points to regions 002–009 built and reviewed, and adds five region markers on existing Design code.
- EXSET-008 plan-reviewer: the plan re-pointed to Design shapes that 002–007 had built and reviewed, and added one region and one test file.
- EXSET-013 plan-reviewer: the scope came out of EXSET-006's reviewed plan (callouts 7–9 carried), with no new Design shape beyond a Logical registration and three pins.
- EXSET-001 test-reviewer: no test or code changed; evidence is in the plan's Test Evidence.

---

## Sibling Todos

---

## Close-Out Audit

---

## Follow-on

- Todo 2 · hand-written skill C# that names sample types EXSET-015 retired: `class-factory.md` "Generated Code" (`ISkillConsultationFactory`) and "Caller Usage" (`IConsultationFactory`); `static-factory.md` "Usage" and `setup.md` "Static Factory Commands" (`SkillEmployeeCommands`, called as statics though delegates are DI-resolved types; `setup.md`'s `SkillEmployeeEvents` line is also the `[Event]` API removed in v1.5.0); `interface-factory.md`'s Server Registration and "Generated surface" names (EXSET-015)
- Todo 2 · `skills/RemoteFactory/references/polymorphic-hierarchy.md` Key Rule 4 says `[Factory]` and `[Remote]` are inherited and `[Create]`/`[Fetch]` are not; the generator does the reverse for a derived `[Factory]` class (EXSET-014, `attributes-reference.md#attributes-and-inheritance`)

---

## Docs & Retro

---

## Results / Conclusions
