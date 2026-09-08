# EXRM-005 — Test Review (Step 5 gate)

**Plan:** [`plans/005-release-1-9-0.md`](../plans/005-release-1-9-0.md)
**Reviewer:** `test-reviewer` · **Round:** 1 · **Date:** 2026-09-08 · **Budget:** tight
**Object:** the plan's Acceptance (8 bullets), its Test Evidence map, and `docs/release-notes/v1.9.0.md` itself, at implementation commit `e356aa6` (plan commit `052128e`)
**Verdict:** **CONCERNS** — no must-cover and no veto; three should-cover items, one of which a consumer will hit.

This plan has no code and no tests, so the gate's question was reframed in the brief: is every `[explicit-skip]` bullet backed by evidence that exists and says what the map claims, and is every factual claim on the release page true? A release page that misstates the product is this plan's equivalent of a vacuous test.

---

## Logs

Build main PASSED, 0 `error CS`, 6 warnings; build design PASSED, 0 `error CS`. Tests — UnitTests 779/0 failed; IntegrationTests 626 + 5 skipped = 631/0 failed; Design.Tests 103/0 failed — all per TFM. Counts identical to the EXRM-004 baselines, as predicted. `005-pack.log` names `Neatoo.RemoteFactory.1.9.0.nupkg` and `Neatoo.RemoteFactory.AspNetCore.1.9.0.nupkg`, both created. No failures anywhere.

## Veto-tier

None. No sacred content weakened: the eleven older release-note pages differ by exactly one line each, and every one of those twenty-two changed lines is a `nav_order` — verified by diffing all eleven and uniq-ing the changed lines.

## Must-cover

None. Every Acceptance bullet's declared evidence exists and says what the map claims; the five-row placement table is correct row-for-row against the generator; the Bug Fixes and Testing sections do not overstate the arc.

## Should-cover

**1. The Migration Guide's audit misses the `[AuthorizeFactory]` method's own `[Service]` parameters.** The page told the consumer to find bare `[Execute]` methods and check *its* `[Service]` parameters. But a bare `[Execute]` whose `[AuthorizeFactory]` auth method is **not** `[Remote]` now runs that auth method client-side too (`FactoryModelBuilder.cs:471-473` — `isRemote` is false, so nothing crosses), and the auth method's own server-only service throws there. The page stated the fact in Breaking Changes but never converted it into an audit item, so a consumer whose `[Execute]` signature is clean stops looking. *Reachable by:* any consumer with `[AuthorizeFactory]` on a bare `[Execute]` resolving a permissions repository — the exact shape pinned by `LocalClassExecuteTests.BareExecute_ClientSideAuth_Allowed_RunsLocally_NoRemoteRequest`. *Affects: AC-5 (Must criterion; the bullet's own word is Must for bullet 1).*

**2. The Overview flatly contradicted the Bug Fixes section.** The Overview ended "With `[Remote]` nothing changes from v1.8.1"; Bug Fixes says "Both bare and `[Remote]` class-level `[Execute]` were affected" by the `Task<T>` → `Task<T?>` change, and Breaking Changes states the same claim correctly with an "otherwise" hedge. The plan designed the Overview as a place a consumer "can stop reading after", so the unhedged version is the one some readers take away. *Reachable by:* a reader of the Overview with a `[Remote, Execute]` class-level method under authorization.

**3. The disclosed CRLF revert on `src/Directory.Build.props` was not complete.** The Test Evidence said the diff "is now exactly the three intended property changes (7 insertions / 4 deletions)". The count was right but the content was four changes: the file's last line went from `</Project>` to `</Project>\r` (both blobs end without a newline). Harmless to MSBuild, but the evidence statement was false, and it is the one thing the brief asked to verify. One byte.

## Tech-debt

- `CLAUDE.md` said the prefix scan "found one of its three shippable items"; `v1.9.0.md` said it "finds just the first of the two entries above" — the same release counting its own shipment two ways.
- `reviews/003-evidence/*publish.log` are caught by `.gitignore:94` (`*.log`) — `003-publish.log`, `baseline-publish.log`, `knownbad-publish.log` — so the container the release page links carries the gate and harness outputs only.
- The Migration Guide was headed "**One audit, one attribute.**" and then carried a second, unrelated migration item (`Task<T?>`) — and, after should-cover 1, a third.
- The page names four unqualified BCL types under #97 while the todo's Dismissed row named two; the two extra are real (`ClassFactoryRenderer.cs:396-397`) and match the issue text, so the Dismissed row was the stale one.

## Answers

**Q1.** All five placement rows correct: `ClassFactoryRenderer.cs:844` passes `isServerOnly: method.IsInternal || method.IsRemote` and `:395-397` emits the guard only when true; `StaticFactoryRenderer.cs:150-156` registers a bare delegate unguarded in every mode with no remote registration, `:182` registers `[Remote]` guarded. Class rows also match `docs/trimming.md:28-30`.

**Q2.** Both qualifications hold — `ClassFactoryRenderer.cs:844` for the `internal static` case (interface-modifier half pinned by `InternalVisibilityTests.cs:301`), and `FactoryModelBuilder.cs:540-542` (static) / `:471-473` (class) for auth-forces-remote. The three "leave alone" cases list nothing that actually needs changing.

**Q3.** Accurate: `FactoryModelBuilder.cs:485-487` is the cited fix and its comment names CS8603 as fatal under TWAE, matching the Bug Fixes text; the `Task<T>` → `Task<T?>` consequence is actionable in three places (header line, Breaking Changes, Migration Guide).

**Q4.** Not overstated: `reviews/003-evidence/README.md`'s pair table gives exactly three async-matched pairs, and `knownbad-gate.txt` shows exit 1 with 3 `is MISSING` against 60 incumbents still ok. "CI gate" is literal — `.github/workflows/build.yml:101-126`. Integration maps to `plans/002-local-execute-coverage.md` bullets 1–5; Unit to `InternalVisibilityTests.cs:301` and `NF0105Tests.cs:161,204`.

**Q5.** Bullets 5, 6, 7 all hold: 1.x `nav_order` now 1…12 with no gap and no v0.x file touched; `Directory.Build.props` both properties 1.9.0 with the blob URL; `005-pack.log` names both packages; `grep -n "VersionPrefix\|findstr \"^feat" CLAUDE.md` exits 1.

**Q6.** The eleven older pages changed by twenty-two lines pooled, all `nav_order` — no historical content altered.

**Q7.** **No false statement found.** `attributes-reference.md:203-207`, `trimming.md:28-30,38`, `DiagnosticDescriptors.cs:34`, `v1.0.0.md:198` and both commit SHAs all check out, and all nine plan-review callouts are implemented.

## Read report

Beyond the brief: `plans/002-local-execute-coverage.md`, `InternalVisibilityTests.cs`, `build.yml`, `NF0105Tests.cs`, `DiagnosticDescriptors.cs` — each for a specific claim on the page. Distillations verified only where a finding turned on them: the props revert (`od -c`) and the eleven-page diff (pooled, not sampled). Named but unused: `005-plan-review.md`'s triage section, the todo Punchlist; Dismissed section checked.

---

## Orchestrator verification (before acting)

All three should-cover findings checked against source rather than taken on report:

- **1** — `FactoryModelBuilder.cs:471-473` reads `isRemote = method.IsRemote || method.AuthMethodInfos.Any(m => m.IsRemote) || method.AspAuthorizeCalls.Any()`. A non-`[Remote]` `[AuthorizeFactory]` method contributes nothing, so the call stays local and the auth method runs with it. The cited test exists at `LocalClassExecuteTests.cs:125`. **Real, and consumer-facing.**
- **2** — confirmed at `v1.9.0.md:20` against `:89` and `:102`. A direct self-contradiction.
- **3** — confirmed by `od -c`: `git show e356aa6~1:src/Directory.Build.props` ends `\n < / P r o j e c t >`; the committed file ended `j e c t > \r`. My revert used `s/\r\n$/\n/`, which cannot match a final line that has no newline after it. **My error, and my evidence statement was false.**

## Disposition — all three worked inline, none deferred

Round 1 carried no must-cover, so the gate closes here. The three should-cover items would normally be the user's judgment call; all three were fixed rather than deferred, because deferring them is not a real option: one is a gap a consumer following the migration guide would fall into, one is a contradiction inside the same page, and one is a false statement in this plan's own evidence.

| # | Fix |
|---|---|
| 1 | `## Migration Guide` gains a fourth audit item — check the `[AuthorizeFactory]` method's own `[Service]` parameters, since a non-`[Remote]` auth method does not force the server and now runs client-side alongside the call it guards; either make it `[Remote]` or make its services client-resolvable |
| 2 | The Overview now reads "With `[Remote]`, **placement** is unchanged from v1.8.1; the one exception is a class-level `[Execute]` under authorization, whose generated return type is now nullable", linking Bug Fixes |
| 3 | Stray `\r` removed with `perl -0777 -pi -e 's/\r\z//'`; the diff against `e356aa6~1` re-verified as exactly the three property changes. The Test Evidence note now records that the first revert was incomplete and the statement false, rather than quietly correcting it |
| td-1 | Both sentences now say the scan finds one of the two commits the notes list, and none of the documentation work |
| td-2 | Not actioned — `*.log` being gitignored is the repo's standing convention (EXRM-002 onward); the tracked `003-evidence/*.txt` files are what AC-3's evidence rests on, and the release page points at the container, not at the logs. No change warranted |
| td-3 | The Migration Guide heading now reads "One audit for placement, plus two things to check afterwards" — accurate for the three items it actually carries, one of which should-cover 1 added |
| td-4 | The todo's Dismissed row now names all four unqualified types, matching issue #97 and the release page. The row was the stale text, not the page |

Should-cover fixes committed in `cf0a0fd`; the two tech-debt fixes from the report's tail followed. No re-run needed: no code changed, and the build/test/pack evidence for bullets 6 and 8 is untouched by prose edits.

**Gate closed CLEAN in one round on the substance** — no must-cover, no veto, every factual claim on the release page verified true, and all nine plan-review callouts confirmed implemented. The reviewer's Q7 pass, which read the page end to end as a consumer would, found nothing wrong.
