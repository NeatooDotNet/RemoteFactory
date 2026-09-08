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
- `reviews/003-evidence/*publish.log` are caught by `.gitignore` (`*.log`), so the container the release page points at carries the gate and harness outputs but not the publish logs behind them.

*(The reviewer's report truncated mid-second tech-debt item; the remainder was requested. Nothing outstanding affects the dispositions below — the verdict, all three should-cover items and the first tech-debt item arrived complete.)*

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

Fixes committed in `2f04e00`. No re-run needed: no code changed, and the build/test/pack evidence for bullets 6 and 8 is untouched by prose edits.
