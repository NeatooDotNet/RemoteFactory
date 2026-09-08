# Release Notes and Version for v1.9.0

**Plan #:** 005
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5
**Status:** Done
**Last Updated:** 2026-09-08
**Plan-review opt-in:** Yes — the release page is the arc's public face and restates a contract four plans established, so a page that contradicts the docs it links is a reachable defect. `business-requirements-reviewer` only — user decision 2026-09-08 (the stub had expected No; the orchestrator changed the recommendation at Step 2)
**Code-review opt-in:** No — user decision 2026-09-08: no code changes; docs, two version properties, and the release-process block in `CLAUDE.md`
**Branch:** exrm-005-release-1-9-0 — cut from the arc at Step 2
**PR:** —

---

## Scope

Cut v1.9.0: a release-notes page whose headline is the behaviour change — `[Execute]` obeys `[Remote]`, which breaks at runtime for a bare `[Execute]` that needed the server — with the one-line migration rule, the bug fix that rode along, and a statement of what the arc did not establish; the index's Highlights row, All Releases entry and nav ordering; both version properties in `Directory.Build.props`, reconciling their drift; and the two corrections the arc proved `CLAUDE.md`'s release process needs. Records the deliberate departure from the major-bump standard where a reader will see it. Does not change library, generator, Design, skill or test code; does not tag, publish, or create a GitHub release — those follow the arc's merge to `main` at Step 8 and are the user's; and does not notify the waiting zTreatment session, which happens once the tag exists.

---

## Intent

- AC-5's remaining half. Four plans changed how `[Execute]` behaves and rewrote every place the old contract was stated; a consumer upgrading from 1.8.1 currently has no way to learn any of it.
- The change is genuinely breaking even though the version is a minor bump, and in two ways rather than one. At runtime: a bare `[Execute]` that resolved a server-only `[Service]` used to run on the server and now runs where it is resolved — with two exceptions the page must state, because `internal static` on a class factory stays server-only and authorization still forces the server (plan-review M2, M4). At compile time: a class-level `[Execute]` under authorization now generates `Task<T?>` where it generated `Task<T>` (M3). The notes say both plainly, carry the migration rule, and put the minor-bump decision on the page with its reason rather than leaving a reader to infer it.
- One shipped item is invisible to the documented procedure. `CLAUDE.md` says to build the notes from `git log | grep ^feat:`/`^fix:`; the arc's generator bug fix landed inside a commit prefixed `test(execute):`, so that scan returns one `feat` and misses it entirely. The notes are sourced from the arc's plans and gate records instead, and the procedure is corrected so the next release does not inherit the trap.
- The version properties have drifted apart — `FileVersion` still reads 1.7.0 while `PackageVersion` reads 1.8.1 — and a release is the one moment that reliably reconciles them.

---

## Framework & Architectural Alignment

- Release-notes conventions as the existing pages practise them: Jekyll front matter (`layout`, `title`, `description`, `parent: Release Notes`, `nav_order`), an `# vX.Y.Z` heading, `**Released:**` and `**Breaking changes:**` header lines, an Overview, then the sections the release warrants — What's New, Bug Fixes, Behaviour Changes, Migration Guide, Testing, Documentation.
- **v1.5.0 is the precedent for this release's shape** — an intentional minor bump carrying a breaking change, with a `> **Pre-stable API note.**` blockquote — but its blockquote is **scoped to the event-shaped surface** and cannot be reused verbatim here (plan-review M1). This plan follows the idiom and writes a blockquote naming *this* surface, and it carves out the commitment that actually binds: the **API Stability Commitment published in v1.0.0** ("minor releases — additive features only"; "generated code shapes … part of the public surface"), which is stronger and more consumer-facing than the repo `CLAUDE.md` CI/CD standard.
- Required sections per `CLAUDE.md`'s release-notes template and the precedents: a `## Breaking Changes` section (v1.5.0 has one, and this release's header line says "Yes") and a `## Commits` section (v1.7.0 has one) — plan-review S4.
- **v1.7.0 is the precedent for scope-limiting**: its "What this release does not claim" section. This arc has the same shape — a measured trimming pair on two factory shapes, and an interface-factory leg explicitly not established — so the page says what is measured and what is not.
- `index.md`'s two lists: the Highlights table (newest first, one dense `**Feat**`/`**Fix**`/`**Breaking**`-prefixed paragraph) and the All Releases list.
- CI/CD standards from the repo `CLAUDE.md`: version lives in `Directory.Build.props`, bumped by hand; the `v*` tag drives publish and the GitHub release. Multi-targeting `net9.0;net10.0` is unchanged.
- The arc's own record is the source of truth for what shipped: the four plan files, their Gate Records, and the todo's Discovery Log — not a commit scan.

---

## Constraints & Invariants

- No change under `src/Generator`, `src/RemoteFactory`, `src/Design`, `skills/`, or any test project. This plan edits `docs/release-notes/`, `src/Directory.Build.props`, and `CLAUDE.md` only.
- Every claim on the page traces to something the arc measured or shipped, cited from a plan or a gate record. Where the arc did not establish something the page says so rather than letting coverage be inferred, and the list is four items, consistent between this plan's sections (plan-review S5, Q6): interface-factory bodies under trimming (not established); interface factories cannot carry `[Execute]` at all and stay always-remote under NF0106 — a different caveat from the trimming one; `[AspAuthorize]` enforcement on a **static-factory** `[Execute]` is collected but not enforced (#91); and generated code relies on the consumer's `ImplicitUsings` (#97).
- The break is named in the header line whatever the version number says, and the minor-bump decision appears with its reason. The record must show a decision, not a slip. **The header line reads "Yes — behaviour, and one generated signature"** (user decision 2026-09-08, amended from "behaviour, not API" once plan-review M3 showed that clause to be false): a bare `[Execute]` is no longer always remote, and the auth'd class-level shape now generates `Task<T?>`. The blockquote beneath it names this surface and cites v1.0.0's commitment.
- The migration rule is actionable without opening another page, and **scoped to the shapes it applies to** (plan-review M2): audit bare `[Execute]` methods that are `private static` on a static factory or `public static` on a class factory for server-only `[Service]` parameters, and add `[Remote]`. It does not tell the reader to change an `internal static` class-level `[Execute]`, which stays server-only, nor one whose authorization already forces the server. The page carries the three-placement table `ClassFactoryWithExecute.cs` already states, so the exceptions are visible rather than buried in prose.
- Both `FileVersion` and `PackageVersion` read 1.9.0 when this lands, and `PackageReleaseNotes` points at the release-notes **index** rather than a per-version page (user decision 2026-09-08) — one URL that cannot go stale, so no recurring step is added to a process this plan is already correcting. The URL is the **GitHub blob URL**, not a docs-site one: plan-review S2 flagged the target as unverifiable and the check settled it — `gh api …/pages` returns 404, there is no `_config.yml` in the tree, and `.github/workflows/` holds only `build.yml`, so no rendered site exists and a site URL would 404 on the NuGet page. No other property in `Directory.Build.props` changes.
- No tag, no `gh release create`, no NuGet publish, and no message to the waiting zTreatment session on this branch — the tag follows the arc's merge to `main`, and the notification follows the tag.
- `nav_order` is adjusted across the maintained 1.x sequence only. The v0.x pages' numbering is already degenerate (33 pages share `nav_order: 3`) and predates this arc; it is not this plan's to repair.
- Both solutions build and test green with counts flat — nothing here should move a test — and a Release pack produces 1.9.0 packages.

---

## Steps

1. Assemble what actually shipped from the arc's four plans, their Gate Records and the Discovery Log — not from a commit scan — separating the behaviour change from the bug fix, and marking which claims the arc measured and which it deliberately left unestablished.
2. Write the v1.9.0 page: header lines naming both breaks, an Overview a consumer can stop reading after, a Breaking Changes section carrying the three-placement table and the qualification that authorization still forces the server, the migration rule, the bug fix with its nullable-result consequence, a deployment note that both tiers upgrade together (a 1.8.1 client's bare-`[Execute]` remote registration meets a 1.9.0 server's refusal), a scope-limit section for what the release does not claim, and a Commits section.
3. Put the minor-bump decision on the page in the v1.5.0 idiom but scoped to this surface, naming and carving out the API Stability Commitment published in v1.0.0 — the rule a consumer would actually quote back — rather than only the repo's CI/CD standard.
4. Add the Highlights row and the All Releases entry to the index, and give the new page the first nav slot, incrementing the eleven existing 1.x pages behind it.
5. Bump both version properties to 1.9.0, closing the drift the punchlist recorded, and replace the stale `PackageReleaseNotes` placeholder with the release-notes index URL.
6. Correct the repo's `CLAUDE.md` where the arc proved it wrong — the version property it names does not exist; its commit-scan step misses work landed under a non-`feat`/`fix` prefix, as this very release demonstrates; and its nav_order step cannot be followed as written, which `index.md`'s template repeats and so is corrected in both places.
7. Cross-check the page against the Design source of truth and the docs EXRM-004 rewrote, so the release page and the pages it sends readers to state the same contract.
8. Build and test both solutions to log files, and pack in Release to confirm the version bump produces 1.9.0 packages and nothing else moved.

---

## Acceptance

- [x] The v1.9.0 page states the behaviour change, names it breaking, carries the three-placement table so the `internal static` and authorization exceptions are visible, and gives a migration rule scoped to the shapes it applies to `[explicit-skip: doc prose]` · Must
- [x] The generator bug fix — a class-level `[Execute]` under `[AuthorizeFactory]`/`[AspAuthorize]` returned null through a non-nullable signature and failed CS8603 under `TreatWarningsAsErrors` — appears as a Bug Fix that a commit scan would have missed, described as "now compiles, and the result is typed nullable" rather than "was impossible", with the `Task<T>` → `Task<T?>` consequence stated as a migration item `[explicit-skip: doc prose]` · Must
- [x] Every claim on the page traces to arc evidence, and what the arc did not establish is stated rather than implied `[explicit-skip: doc prose; verified claim-by-claim at the gate]` · Should
- [x] The minor-bump decision and its reason appear on the page, naming and carving out v1.0.0's published API Stability Commitment rather than only the repo's CI/CD standard `[explicit-skip: doc prose]` · Should
- [x] The index carries a Highlights row and an All Releases entry, and v1.9.0 takes the first nav slot with the 1.x sequence shifted behind it `[explicit-skip: doc prose]` · Must
- [x] Both version properties read 1.9.0 and `PackageReleaseNotes` points at the release-notes index, and a Release pack produces `Neatoo.RemoteFactory.1.9.0.nupkg` and `Neatoo.RemoteFactory.AspNetCore.1.9.0.nupkg` `[explicit-skip: build artifact, named in the log]` · Must
- [x] The repo's `CLAUDE.md` release process names the real version property, says release content is sourced from the arc's plans rather than a commit scan, and states a nav_order rule that can actually be followed — mirrored in `index.md`'s template `[explicit-skip: doc prose]` · Should
- [x] Both solutions build and test green with counts flat `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Walked 2026-09-08 on `exrm-005-release-1-9-0` @ `052128e`. One surprise, handled as an amendment before the first edit: the `net8.0` TFM item is not in this repo.

- **The TFM correction is out of scope, and the Step 2 question that authorized it was wrong.** `grep -n "net8\|Multi-targeting" CLAUDE.md` returns nothing; the claim is at **`~/.claude/CLAUDE.md:199`**, the user's global cross-project config. It was folded into scope on the orchestrator's description of it as "a fourth stale item in a file this plan already corrects three times", which the pre-flight disproved. Dropped from Steps, Acceptance and the Punchlist; a private config file outside the repository is not edited from inside a repo PR, and the authorization rested on a false premise. Reported to the user as a separate one-line change they can take or leave.
- **Repo `CLAUDE.md` edit sites confirmed:** `:220-225` (the commit-scan step, with `findstr "^feat:"` / `"^fix:"`), `:242` (nav_order), `:246` (`<VersionPrefix>X.Y.Z</VersionPrefix>`). All three are in the "Creating a New Release" block.
- **`index.md` template repeats the nav_order rule** at the `nav_order: N  # Newest = 1, increment existing pages` line inside its "Create the release file" fenced block; the template also uses `**Release Date:**` and carries a `**NuGet:**` line, while v1.8.1 uses `**Released:**` and drops the NuGet line. The page follows v1.8.1 (most recent), and the template is left as the template except for the nav_order comment.
- **The eleven 1.x pages to increment, confirmed by file:** v1.8.1=1, v1.8.0=2, v1.7.0=3, v1.6.1=4, v1.6.0=5, v1.5.0=6, v1.4.0=7, v1.3.0=8, v1.2.0=9, v1.1.0=10, v1.0.0=11 → each +1, v1.9.0 takes 1.
- **`## Commits` format** (v1.7.0): a bullet per commit as `` `sha` — message ``, then a line pointing at the todo container, then a line pointing at the GitHub release for the auto-generated full list. The arc's shippable commits are `a315fec` (feat, EXRM-001) and `8f02dfc` (the EXRM-002 generator fix, landed under a `test(` prefix — the reason the scan misses it); `docs:` commits are omitted per the repo's convention.
- **The wire refusal, for the deployment note** (`StaticFactoryRenderer.cs:141-158`): bare `[Execute]` delegates get one unguarded registration in every mode **and** `LocalOnlyDelegateRegistry.Register(typeof(...))`, whose comment states the purpose — "the registry declaration lets the server's delegate handler refuse a crafted request that names it". `LocalOnlyDelegateRegistry.Register` is idempotent (`TryAdd`). So the refusal is a server-side guard against a crafted request, and a 1.8.1 client's remote registration for a bare `[Execute]` meets it as an unknown delegate — the mixed-version case S1 raised.
- **The three-placement table is already written** in `ClassFactoryWithExecute.cs:158-161` (`RunCommand` / `ScoreLocally` / `ArchiveOnServer`); the page reproduces it rather than composing a new one.
- **Version state unchanged since Step 2:** `src/Directory.Build.props:18-19` `FileVersion` 1.7.0, `PackageVersion` 1.8.1, plus `<PackageReleaseNotes>None yet</PackageReleaseNotes>` at `:29`.
- **Baselines** (per TFM): UnitTests 779, IntegrationTests 631 (626 + 5 skipped), Design.Tests 103, reference app 42. Nothing here touches code, so all four stay flat.

---

## Punchlist

Step 2 triage / **Step 6 sweep** (2026-09-08): this is the todo's last plan, so the sweep runs here. Both remaining open rows lie in this plan's path and are pulled down; nothing else is open, so the sweep needs no branch of its own and closes with this plan.

- [ ] `FileVersion` reads 1.7.0 while `PackageVersion` reads 1.8.1 · `src/Directory.Build.props:18-19` · done when both read 1.9.0 · AC-5 · Must (from the todo)
- [ ] `CLAUDE.md` release step says bump `<VersionPrefix>`, which does not exist; the props carry `<PackageVersion>` · `CLAUDE.md:242` · done when the step names the real property · AC-5 · Must (from the todo)
- [ ] `CLAUDE.md`'s commit-scan step misses work landed under a non-`feat`/`fix` prefix · same release-process section · done when the step says to source from the arc's plans, citing this release as the case · AC-5 · Should (found at this plan's Step 2; rides the row above since it edits the same block)
- [ ] The nav_order step cannot be followed as written and is stated twice · `CLAUDE.md:242` and `docs/release-notes/index.md:138` · done when both say to maintain the 1.x sequence and treat the v0.x tail as frozen · AC-5 · Should (plan-review S3)
- [x → outside this PR] `net8.0;net9.0;net10.0` TFM claim · **not in the repo's `CLAUDE.md`** — it is the user's global `~/.claude/CLAUDE.md` · dropped from this plan at Step 3 pre-flight (the Step 2 question had described it as living in the block this plan already corrects, which the pre-flight disproved, so the authorization rested on a false premise), reported, and then **done as a standalone edit at the user's re-direction with the correct file named** — `net9.0;net10.0`. Not in this branch or PR; recorded here so the close-out audit does not read it as dropped work · AC-5 · Should

---

## Test Evidence

Docs, version properties and one config file — no code, so the tiers are prose and build artifacts. Logs (local, gitignored): `reviews/005-build-main.log`, `005-test-main.log`, `005-build-design.log`, `005-test-design.log`, `005-pack.log`.

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Tier confirmed |
|---|---|---|---|---|
| 1 — behaviour change named, placement table carries the exceptions, migration rule scoped | Must | `[explicit-skip: doc prose]` | `v1.9.0.md`: header line "Yes — behaviour, and one generated signature"; `## Breaking Changes` opens with the five-row placement table (static `[Remote]`/bare, class `[Remote]`/`public static`/`internal static`) and the two qualifications beneath it — `internal static` needs no migration, authorization still forces the server. `## Migration Guide` scopes the audit to `private static` (static factory) and `public static` (class factory), lists three "leave alone" cases, and — after the gate's should-cover 1 — carries a fourth audit item for the `[AuthorizeFactory]` method's **own** `[Service]` parameters, since a non-`[Remote]` auth method does not force the server (`FactoryModelBuilder.cs:471-473`) and now runs client-side alongside the call it guards. Table matches `ClassFactoryWithExecute.cs:158-161` and `docs/trimming.md`'s guard rows | ✓ |
| 2 — bug fix present, described as "now compiles, result typed nullable", with the `Task<T?>` migration | Must | `[explicit-skip: doc prose]` | `v1.9.0.md` `## Bug Fixes`: both build outcomes stated (CS8603 fatal under TWAE; without it, a warning and `null` through a non-null signature), the fix as "the same rule the Read path has always applied", and the explicit "it was not previously impossible, only unsound". Migration consequence appears twice — in `## Breaking Changes` ("One generated signature changed") and in `## Migration Guide`. Traced to `FactoryModelBuilder.cs:485-487` | ✓ |
| 3 — claims trace to arc evidence; what is not established is said | Should | `[explicit-skip: doc prose]` | `## What this release does not claim` carries all four items the plan required: interface-factory trimming (unchanged from v1.7.0), interface factories cannot carry `[Execute]` (NF0106), static-factory `[AspAuthorize]` not enforced ([#91]), generated code relies on `ImplicitUsings` ([#97]). The trimming claim is stated as measured — three matched pairs, absent and present, invoked on the trimmed client — matching EXRM-003's Gate Record. Step 7 cross-check corrected one overstatement before commit: "the same holds on a class factory" implied identical registration mechanics, and now names the Local-method-only shape | ✓ |
| 4 — minor-bump decision on the page, carving out v1.0.0's commitment | Should | `[explicit-skip: doc prose]` | `v1.9.0.md` pre-stable blockquote names the [API Stability Commitment](../../../release-notes/v1.0.0.md) by link and by what it promises, says this release departs from it deliberately, scopes the carve-out to "where factory operations run" (not v1.5.0's event surface), and cites v1.1.0/v1.4.0/v1.5.0 as prior breaking minors. Anchor `v1.0.0.md#api-stability-commitment` verified against `v1.0.0.md:198` | ✓ |
| 5 — index Highlights row, All Releases entry, first nav slot | Must | `[explicit-skip: doc prose]` | `index.md`: Highlights row added above v1.8.1 in the dense one-paragraph idiom; All Releases entry added at the top. `v1.9.0.md` `nav_order: 1`; the eleven 1.x pages incremented 1→2 … 11→12, verified by listing every `v1.*.md`. `git diff --name-only docs/release-notes/` shows only the eleven 1.x pages plus the index — the v0.x tail untouched, as the plan constrained | ✓ |
| 6 — both version properties 1.9.0, `PackageReleaseNotes` set, Release pack produces 1.9.0 packages | Must | `[explicit-skip: build artifact]` | `src/Directory.Build.props:18-19` both read `1.9.0` (closing the 1.7.0/1.8.1 drift); `:28-31` `PackageReleaseNotes` is the GitHub blob URL with a comment saying why it is not a site URL. `005-pack.log`: both packs exit 0 and produce **`Neatoo.RemoteFactory.1.9.0.nupkg`** and **`Neatoo.RemoteFactory.AspNetCore.1.9.0.nupkg`**; the throwaway output directory was deleted after the check | ✓ |
| 7 — repo `CLAUDE.md` release process corrected in three places, mirrored in the index template | Should | `[explicit-skip: doc prose]` | `CLAUDE.md` step 1 now says to assemble from the todo container and names this release as the case a prefix scan under-reports (one of three shippable items found); step 5 states the followable nav rule; step 6 names `<FileVersion>` **and** `<PackageVersion>`. `grep -n "VersionPrefix\|findstr \"^feat"` returns nothing. `index.md`'s template comment mirrors the nav rule. The `net8.0` item proved to live in the user's **global** config, not this file — dropped from the plan at pre-flight and done separately at the user's re-direction (Punchlist) | ✓ |
| 8 — both solutions build and test green, counts flat | Must | `[explicit-skip: meta-bullet]` | `005-build-main.log`, `005-build-design.log` exit 0. `005-test-main.log`: UnitTests **779**, IntegrationTests **631** (626 + 5 skipped); `005-test-design.log`: Design.Tests **103** — all per TFM (net9.0 + net10.0), 0 failed, identical to the EXRM-004 baselines as predicted, since no code changed | ✓ |

Line-ending note: a blanket CRLF normalisation was applied to `src/Directory.Build.props`, which git stores LF, and reverted. **The first revert was incomplete and the evidence statement here was false** — the gate caught it (should-cover 3): the file ends without a newline, so `s/\r\n$/\n/` did not touch its final line, which stayed `</Project>\r`. Verified by `od -c` against `git show e356aa6~1:`, fixed with `perl -0777 -pi -e 's/\r\z//'`, and re-checked: the diff against `e356aa6~1` is now exactly the three intended property changes and nothing else. The eleven release-note pages and `CLAUDE.md`/`index.md` normalised cleanly throughout and show only their content edits.

---

## Gate Record

- Round 1 (2026-09-08): test-review **CONCERNS** — no veto-tier and **no must-cover**; every Acceptance bullet's evidence exists and says what the map claims, the five-row placement table is correct row-for-row against the generator, and the eleven older release-note pages differ by exactly one `nav_order` line each. Three should-cover items, all **worked inline rather than deferred**, because none was a real candidate for deferral: the Migration Guide missed the `[AuthorizeFactory]` method's own `[Service]` parameters (a non-`[Remote]` auth method does not force the server and now runs client-side with the call it guards — a gap a consumer following the guide falls into); the Overview contradicted the Bug Fixes section on whether `[Remote]` changed; and this plan's own line-ending evidence statement was false, the first CRLF revert having missed a final line with no newline after it. One tech-debt item fixed (the release counted its own shipment two ways), one dismissed with reason. — [`reviews/005-test-review.md`](../reviews/005-test-review.md)
- Code review: not opted in (user decision at Step 2) — no code in this plan.
- Gate closed in one round, no leftovers and no demotions. Fixes in `2f04e00`; no re-run needed, since the build, test and pack evidence is untouched by prose edits. Done.

---

## Plan Amendments

_(append-only)_

### 2026-09-08 — Plan-review callouts amended before implementation

- **Section affected:** Intent bullet 2; Framework (v1.5.0 precedent, required sections, the binding rule, the signature change); Constraints (header line, migration rule, not-established list, `PackageReleaseNotes`); Steps 2, 3, 4, 6; Acceptance bullets 1, 2, 4, 7; Punchlist; Notes.
- **Original said:** the headline claimed a bare `[Execute]` "runs where it is called" and the header line read "Yes — behaviour, **not API**"; the migration rule was one unscoped sentence; the framing was v1.5.0's blockquote "reused rather than inventing a framing"; the not-established list held two items; `PackageReleaseNotes` pointed at an unspecified index URL; `CLAUDE.md` was to gain two corrections; the Notes said Step 4 shifts "twelve" pages.
- **What changed:** the headline gains the three-placement table and the auth-forces-remote qualification, because `internal static` on a class factory stays server-only (M2, M4); "not API" is dropped and the `Task<T>` → `Task<T?>` change named, since it is a real signature change by v1.0.0's own definition (M3); the blockquote names *this* surface and carves out v1.0.0's published API Stability Commitment rather than reusing v1.5.0's event-surface scope (M1); a deployment sentence covers the mixed-version wire refusal (S1); the blob URL replaces a docs-site URL that would 404, Pages being disabled (S2); the nav_order step is corrected in both `CLAUDE.md` and `index.md` (S3); `Breaking Changes` and `Commits` sections are required (S4); the not-established list is four items and consistent across sections (S5); eleven pages increment, not twelve.
- **Why:** `reviews/005-plan-review.md`; user decision 2026-09-08 (accept all as proposed), plus the `net8.0` TFM correction directed into scope.
- **Discovery Log:** 2026-09-08 / EXRM-005

---

## Notes

The zTreatment session (ztreatmentneatoo-9c) is waiting on a message that 1.9.0 is tagged before UCG-001 starts.

Release-notes conventions on record (docs recon, 2026-09-08): front matter `layout: default`, `title: "v1.9.0"`, `description`, `parent: Release Notes`, `nav_order: 1` with older pages incremented; header lines `**Released:** YYYY-MM-DD` then `**Breaking changes:**` (v1.8.1 idiom: "No — but see [Behaviour Notes]"; v1.5.0 idiom: "Yes — … See [Migration Guide]"). Behavior-change section is a bold one-line claim followed by bullets naming who is unaffected and what moved; spelled "Behaviour Changes" (v1.7.0) or "Behaviour Notes" (v1.8.x); v1.7.0 adds "What this release does not claim". `index.md`: Highlights table (newest first, one dense **Feat**/**Fix**/**Breaking**-prefixed paragraph) and the All Releases list, plus a `## Documentation` section listing changed doc, Design, and skill files. The version lives in `src/Directory.Build.props` as `PackageVersion` (`FileVersion` has drifted; punchlist row on the todo).

Pointer from EXRM-002 (2026-09-08): release notes carry a bug fix beside the behavior change — a class-level [Execute] under [AuthorizeFactory] or [AspAuthorize] declared a non-nullable factory method while returning `Authorized<T>.Result`, so the generated file failed CS8603 under TreatWarningsAsErrors and a denial returned null through a non-nullable signature; `BuildClassExecuteMethod` now mirrors the Read path (`isNullable` includes `HasAuth`). Affected both bare and [Remote] class Execute; never compiled in-repo before 002.

**Step 2 recon (2026-09-08, arc @ `56c23b1`, after EXRM-001/002/003/004 merged).**

- **The commit scan under-reports the release.** `git log v1.8.1..HEAD --format=%s | grep -E '^(feat|fix|perf)'` returns exactly one line — `feat(generator): [Execute] obeys [Remote] for static factories` (`a315fec`). The generator bug fix shipped inside `8f02dfc test(execute): local Execute proven on both shapes, plus auth`, and EXRM-004's whole documentation rewrite is under `docs:`. Building the page from the documented procedure would ship a release that names neither. This is the plan's own evidence for the `CLAUDE.md` correction.
- **Version state:** `src/Directory.Build.props:18-19` — `<FileVersion>1.7.0</FileVersion>`, `<PackageVersion>1.8.1</PackageVersion>`. Also in that PropertyGroup: `<PackageReleaseNotes>None yet</PackageReleaseNotes>`, which is NuGet-visible and stale — raised with the user at Step 2 rather than assumed into scope.
- **nav_order is degenerate below the 1.x line.** Only `v1.8.1` holds a unique number (1); `v1.8.0` shares 2 with two v0.x pages, and **33 pages share `nav_order: 3`** — `v1.7.0` among them (plan-review Q5), so the 1.x block is a clean sequence only within itself, not in the rendered order. Step 4 increments the **eleven** existing 1.x pages (v1.8.1=1 … v1.0.0=11 become 2…12) and leaves the v0.x tail as it found it; the collisions move rather than resolve, which is accepted. `CLAUDE.md`'s "increment existing release page nav_orders" has not been followed in practice for some time and cannot be, as written, without renumbering fifty pages for no reader benefit — so the step itself is corrected, in `CLAUDE.md` and in `index.md`'s template, which repeats it.
- **Precedents to follow:** `v1.5.0.md:9-17` (minor bump carrying a breaking change, with the pre-stable API blockquote and a "Yes — …" breaking-changes line — but its blockquote is scoped to the event surface and is not reusable verbatim, plan-review M1) and `v1.7.0.md:116` ("What this release does not claim"). `v1.8.1.md` is the most recent page and the closest formatting model; its `**Released:**` header idiom is adopted over the template's `**Release Date:**`.
- **The rule that actually binds** is `docs/release-notes/v1.0.0.md:198-206` — the published API Stability Commitment ("Minor releases (1.x.0) — additive features only. Existing code keeps working"; "Diagnostics IDs, generated code shapes, and runtime contracts … all considered part of the public surface"). The Step 2 draft cited only `CLAUDE.md`'s CI/CD standard and missed this; the page must name and carve it out. Precedent that it is a departure rather than a first: v1.1.0, v1.4.0 and v1.5.0 were all breaking under a minor bump.
- **The auth'd class-Execute signature change is real.** `FactoryModelBuilder.cs:485-487` — `isNullable = method.IsNullable || (authorization != null && authorization.HasAuth)` — so that shape now generates `Task<T?>` where it generated `Task<T>`. Verified at Step 2. A consumer building without `TreatWarningsAsErrors` could ship the old shape (CS8603 was a warning for them), so the fix is described as "now compiles, result typed nullable", not "was impossible".
- **What the arc shipped, for Step 1's assembly:** EXRM-001 static-factory delegates obey `[Remote]`, the weld removed, plus the server's refusal of a crafted request naming a bare local-only delegate (PR #92); EXRM-002 both shapes proven locally with the authorization interplay, and the CS8603 generator fix (PR #93); EXRM-003 the trimming gate measuring three matched pairs, absent and present (PR #95); EXRM-004 the contract restated across docs, skill, Design, XML docs and two diagnostics, the class-level visibility rule pinned, and NF0105's static exemption kept with a stated reason (PR #96). Punchlist PR #98 carried a `CLAUDE.md` mdsnippets note and filed issue #97.
**User decisions at Step 2 (2026-09-08):** priorities confirmed as drafted; plan review by `business-requirements-reviewer` only (the orchestrator raised its own earlier "expect No"); the breaking-changes header line is a "Yes — …" in the v1.5.0 idiom rather than v1.8.1's "No — but see …"; `PackageReleaseNotes` points at the release-notes index. The 2.0.0 alternative was offered and declined.

**After the plan review (2026-09-08):** all nine callouts accepted as proposed, plus the `net8.0` TFM correction folded into the same `CLAUDE.md` edit and the blob URL for `PackageReleaseNotes`. The header line was amended from "Yes — behaviour, not API" to **"Yes — behaviour, and one generated signature"**: M3 showed the "not API" clause to be false, since the auth'd class-level `[Execute]` now generates `Task<T?>`. The 2.0.0 alternative was offered a **second** time with v1.0.0's published API Stability Commitment in hand, and declined again — the minor-bump decision from the todo header stands, and zTreatment's UCG-001 pins 1.9.0.

- **Open issues to reference as not-established rather than fixed:** #91 (`[AspAuthorize]` on a static-factory `[Execute]` is collected but not enforced), #94 (MSB3552 build flake), #97 (generated code relies on the consumer's `ImplicitUsings`).
