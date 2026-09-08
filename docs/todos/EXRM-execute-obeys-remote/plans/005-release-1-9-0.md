# Release Notes and Version for v1.9.0

**Plan #:** 005
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5
**Status:** Draft
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
6. Correct `CLAUDE.md` where the arc proved it wrong — the version property it names does not exist; its commit-scan step misses work landed under a non-`feat`/`fix` prefix, as this very release demonstrates; its nav_order step cannot be followed as written, which `index.md`'s template repeats and so is corrected in both places; and its CI/CD block still claims a `net8.0` target the repo dropped.
7. Cross-check the page against the Design source of truth and the docs EXRM-004 rewrote, so the release page and the pages it sends readers to state the same contract.
8. Build and test both solutions to log files, and pack in Release to confirm the version bump produces 1.9.0 packages and nothing else moved.

---

## Acceptance

- [ ] The v1.9.0 page states the behaviour change, names it breaking, carries the three-placement table so the `internal static` and authorization exceptions are visible, and gives a migration rule scoped to the shapes it applies to `[explicit-skip: doc prose]` · Must
- [ ] The generator bug fix — a class-level `[Execute]` under `[AuthorizeFactory]`/`[AspAuthorize]` returned null through a non-nullable signature and failed CS8603 under `TreatWarningsAsErrors` — appears as a Bug Fix that a commit scan would have missed, described as "now compiles, and the result is typed nullable" rather than "was impossible", with the `Task<T>` → `Task<T?>` consequence stated as a migration item `[explicit-skip: doc prose]` · Must
- [ ] Every claim on the page traces to arc evidence, and what the arc did not establish is stated rather than implied `[explicit-skip: doc prose; verified claim-by-claim at the gate]` · Should
- [ ] The minor-bump decision and its reason appear on the page, naming and carving out v1.0.0's published API Stability Commitment rather than only the repo's CI/CD standard `[explicit-skip: doc prose]` · Should
- [ ] The index carries a Highlights row and an All Releases entry, and v1.9.0 takes the first nav slot with the 1.x sequence shifted behind it `[explicit-skip: doc prose]` · Must
- [ ] Both version properties read 1.9.0 and `PackageReleaseNotes` points at the release-notes index, and a Release pack produces `Neatoo.RemoteFactory.1.9.0.nupkg` and `Neatoo.RemoteFactory.AspNetCore.1.9.0.nupkg` `[explicit-skip: build artifact, named in the log]` · Must
- [ ] `CLAUDE.md`'s release process names the real version property, says release content is sourced from the arc's plans rather than a commit scan, states a nav_order rule that can actually be followed (mirrored in `index.md`'s template), and drops the `net8.0` target the repo no longer builds `[explicit-skip: doc prose]` · Should
- [ ] Both solutions build and test green with counts flat `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

_(Step 3)_

---

## Punchlist

Step 2 triage / **Step 6 sweep** (2026-09-08): this is the todo's last plan, so the sweep runs here. Both remaining open rows lie in this plan's path and are pulled down; nothing else is open, so the sweep needs no branch of its own and closes with this plan.

- [ ] `FileVersion` reads 1.7.0 while `PackageVersion` reads 1.8.1 · `src/Directory.Build.props:18-19` · done when both read 1.9.0 · AC-5 · Must (from the todo)
- [ ] `CLAUDE.md` release step says bump `<VersionPrefix>`, which does not exist; the props carry `<PackageVersion>` · `CLAUDE.md:242` · done when the step names the real property · AC-5 · Must (from the todo)
- [ ] `CLAUDE.md`'s commit-scan step misses work landed under a non-`feat`/`fix` prefix · same release-process section · done when the step says to source from the arc's plans, citing this release as the case · AC-5 · Should (found at this plan's Step 2; rides the row above since it edits the same block)
- [ ] The nav_order step cannot be followed as written and is stated twice · `CLAUDE.md:242` and `docs/release-notes/index.md:138` · done when both say to maintain the 1.x sequence and treat the v0.x tail as frozen · AC-5 · Should (plan-review S3)
- [ ] `CLAUDE.md`'s CI/CD block claims multi-targeting `net8.0;net9.0;net10.0`; the repo builds `net9.0;net10.0` · `CLAUDE.md` CI/CD section, `src/Directory.Build.props:16` · done when the block names the real target set · AC-5 · Should (plan-review Theoretical; user directed it into scope 2026-09-08 as a fourth stale item in a file this plan already corrects)

---

## Test Evidence

_(after implementation)_

---

## Gate Record

_(Step 5)_

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
