# EXRM-005 — Plan Review

**Plan:** [`plans/005-release-1-9-0.md`](../plans/005-release-1-9-0.md)
**Reviewer:** `business-requirements-reviewer` (the plan's declared opt-in; user decision 2026-09-08 — this reviewer only)
**Date:** 2026-09-08 · **Budget:** tight
**Verdict:** **CONCERNS** — the release-page framing is correct in shape but over-claims in two checkable ways (the "runs where it is called" headline is wrong for two sub-shapes; "no signature changed" is false for one), and it never names the strongest thing this release departs from: v1.0.0's published API Stability Commitment.

Reviewer's private notes: [`005-release-1-9-0.memory/requirements-reviewer.md`](../plans/005-release-1-9-0.memory/requirements-reviewer.md) (the repo already carries 20 such `.memory` directories). The reviewer edited no plan or todo file.

---

## Veto-tier

**None.** The one documented-rule contradiction — `docs/release-notes/v1.0.0.md:198-206`, "Minor releases (1.x.0) — additive features only. Existing code keeps working," with "generated code shapes, and runtime contracts … all considered part of the public surface" — is a *published* commitment, stronger than CLAUDE.md's CI/CD standard. But breaking-under-minor is established 1.x practice (v1.1.0, v1.4.0, v1.5.0 all breaking under minor), the user was offered 2.0.0 and declined, and the todo header records it. That makes it a recorded decision, not a veto. It is handled *incompletely* — see M1.

## Callouts affecting AC-5 (Must)

**M1 — The page must name and carve out v1.0.0's commitment, not just CLAUDE.md's standard.** The plan's Framework bullet says it "reuses [v1.5.0's] idiom rather than inventing a framing," but v1.5.0's blockquote is scoped: "the 1.x line is still settling **the event-shaped surface**" (`v1.5.0.md:15`) — a carve-out that does not cover an `[Execute]`/`[Remote]` break. Reused verbatim it publishes a justification for the wrong surface. *Reachable by:* any consumer who read v1.0.0's commitment. *Seam:* Constraints bullet 3 / Step 3 — write a blockquote naming this surface and citing `v1.0.0.md:198-206`.

**M2 — The headline is wrong for the class-level `internal static` shape.** `ClassFactoryRenderer.cs:420,844,897,1115,1381` render `isServerOnly: method.IsInternal || method.IsRemote`, so a bare `[Execute] internal static` on a class factory is guarded and server-only — exactly as `ClassFactoryWithExecute.cs:153-188` (`ArchiveOnServer`) states. "A bare `[Execute]` runs where it is called" is false for it, and the one-sentence migration rule tells that consumer to make a change they do not need. *Reachable by:* anyone following the migration rule literally. *Seam:* Intent bullet 2, Constraints bullet 4, Acceptance bullet 1 — state the three-placement table the Design file already carries.

**M3 — "No signature changed" is false for the auth'd class-Execute shape.** EXRM-002's CS8603 fix is `isNullable = method.IsNullable || (authorization != null && authorization.HasAuth)` at `FactoryModelBuilder.cs:486-487`, so a class-level `[Execute]` under `[AuthorizeFactory]`/`[AspAuthorize]` now generates `Task<T?>` where it generated `Task<T>`. A consumer who built that shape without `TreatWarningsAsErrors` (CS8603 was a warning for them, not fatal) sees new nullable diagnostics at their call sites — a build break under their own TWAE. By v1.0.0's own definition ("generated code shapes … part of the public surface") this is an API change. *Seam:* Constraints bullet 3 — scope "no signature changed" to the `[Execute]`/`[Remote]` change, and give the Bug Fix entry a migration line.

**M4 — Second qualification the headline needs: auth forces remote.** `FactoryModelBuilder.cs:540-542` derives `isRemote = method.IsRemote || AuthMethodInfos.Any(IsRemote) || AspAuthorizeCalls.Any()`, and `ClassFactoryWithExecute.cs:131-134` states it as the rule. A bare `[Execute]` carrying `[AspAuthorize]` or a `[Remote]` auth method still crosses. This is AC-4, already established — it belongs on the page beside the headline, not only in the not-established list. *Seam:* Step 2's "the change itself" section.

## Callouts affecting Should/Could

**S1 — Mixed-version deployment hits EXRM-001's wire refusal.** A 1.8.1 client still emits remote registrations for bare `[Execute]`; a 1.9.0 server refuses them as unknown (`StaticFactoryRenderer.cs:148-155`, `LocalOnlyDelegateRegistry`). Not reachable when client and server ship from one build (zTreatment's case), but reachable for anyone deploying them apart. One deployment sentence: upgrade both tiers together.

**S2 — `PackageReleaseNotes` target is unverifiable from this repo.** There is no `_config.yml` anywhere and no Pages workflow in `.github/workflows/`, so no rendered docs-site URL is established in-repo. A NuGet-visible URL that 404s is worse than "None yet". Confirm the site resolves, or point at the GitHub blob URL.

**S3 — The `nav_order` rule is documented in two places; the plan corrects neither.** `CLAUDE.md:242` and `docs/release-notes/index.md:138` (`nav_order: N  # Newest = 1, increment existing pages`) both state the step the plan says cannot be followed. The plan is already correcting two items in that CLAUDE.md block; leaving this one makes the next release inherit the same contradiction.

**S4 — Section list omits `Breaking Changes` and `Commits`.** `CLAUDE.md:235` names both as required; v1.5.0 — the plan's own precedent for a breaking minor — has `## Breaking Changes`; v1.7.0 has `## Commits`. The plan's Framework bullet lists neither, while its header line says "Yes".

**S5 — The not-established list is inconsistent between plan sections.** Notes list three open issues (#91, #94, #97); Constraints names only two caveats. #97 is consumer-visible and pre-existing — include it or say why not.

## Theoretical (not triaged)

- "Shifts those twelve" counts 11 existing 1.x pages plus the new one; only 11 get incremented.
- After the shift, 1.x occupies 1–12 and still ties with v0.x pages at 2–6; the shift moves collisions rather than removing them.
- CLAUDE.md's CI/CD block also says multi-targeting `net8.0;net9.0;net10.0`; actual is `net9.0;net10.0` (`src/Directory.Build.props:16`).
- Header-line idiom differs across pages (`**Released:**` v1.8.1 vs `**Release Date:**` v1.5.0/v1.7.0/template); v1.8.1 also drops the `**NuGet:**` line the template carries.

## Answers to the brief's questions

**Q1.** Accurate for the static-factory shape (bare is `private static`, no internal variant — `[Remote]` alone drives the guard, `StaticFactoryRenderer.cs:261`) and for the class-factory `public static` bare case (`ClassFactoryWithExecute.cs:118-151`, `ScoreLocally`). **Not accurate** for class-level `internal static` (M2) and needs the auth qualification (M4). The Design file already reads as a three-row table at `ClassFactoryWithExecute.cs:159-162`; the page should reproduce it.

**Q2.** Three items beyond the stated rule. (a) M3 — the nullable return-type change, the only genuine signature change in the release. (b) S1 — the wire refusal is reachable in a mixed-version deployment, not by a same-build client. (c) The class-level `[Execute]` under auth: the Discovery Log's "never compiled in-repo before" is about the repo, not about consumers — without `TreatWarningsAsErrors` CS8603 was a warning, so a consumer *could* ship that shape and get null on denial. Describe it as "now compiles, and the result is typed nullable," not "was impossible." `[Remote, Execute]` itself did not shift: EXRM-001's Constraints bullet 1 asserts byte-for-byte v1.8.1 output, and `reviews/001-code-review.md:11` confirms it in the generated file.

**Q3.** Clean — no gap. The current version appears only at `src/Directory.Build.props:18-19`. `docs/trimming.md:67`, `skills/…/setup.md:7-11`, `…/trimming.md:21`, `…/advanced-patterns.md:439-440` all use `Version="x.y.z"`; README's install lines and `docs/getting-started.md:30-40` are unversioned; `src/Directory.Packages.props` carries only third-party versions; no README badge and no version string in the workflows.

**Q4.** Not veto-tier — a recorded, reaffirmed decision with in-repo precedent (three prior breaking minors). But the plan handles it against the *weaker* rule: it cites CLAUDE.md's CI/CD standard and never `v1.0.0.md:198-206`, the published consumer-facing commitment a consumer would quote back. Reviewer's own view, distinct from the rule question: 1.9.0 is defensible given a single known consumer whose 43 call sites already carry `[Remote]`, provided the page says plainly that it breaks at runtime.

**Q5.** Count verified: exactly **33** pages hold `nav_order: 3` — and **`v1.7.0.md:6` is one of them**, so the 1.x block is a clean 1–11 only within itself, not in the rendered order. Existing 1.x pages number 11, not 12. The departure from `CLAUDE.md:242` is justified; since the plan already corrects two items in that block, it should correct this third, in both places (S3).

**Q6.** Right, but not complete. Both named caveats hold (`docs/trimming.md:40,239`; `FactoryModelBuilder.cs:537-539` states #91 in a source comment). A consumer will also ask about interface factories generally — they stay always-remote under NF0106 and cannot carry `[Execute]` at all, a *different* caveat from the trimming one. #97 is the fourth candidate (S5).

## Read report

Pulled beyond the brief: `v1.0.0.md:198-206` (the decisive find), `FactoryModelBuilder.cs:486-487` and `:540-542`, `ClassFactoryRenderer.cs` guard sites, `StaticFactoryRenderer.cs:148-155,261`, `index.md:138`, `CLAUDE.md:218-260`, section-heading greps of v1.7.0/v1.8.1, a repo-wide version-string sweep, and a `_config.yml`/Pages-workflow check. Unused: `reviews/002-…`–`004-test-review.md`, plan 003's gate detail, `CLAUDE-DESIGN.md` beyond the rules table. Not checked: whether a rendered docs site exists outside this repo.

---

## Orchestrator verification (before triage)

Four claims checked against source rather than taken on report:

- **M1** — `v1.0.0.md:198-206` does publish the commitment, in those words, including "generated code shapes … part of the public surface"; `v1.5.0.md:15` does scope its blockquote to "the event-shaped surface". Both confirmed verbatim.
- **M3** — `FactoryModelBuilder.cs:485-487` reads `var isNullable = method.IsNullable || (authorization != null && authorization.HasAuth);`, with a comment naming CS8603 and `TreatWarningsAsErrors`. The signature change is real, and by v1.0.0's own definition it is an API change.
- **M4** — `FactoryModelBuilder.cs:540-542` confirmed as quoted.
- **S2 — settled decisively.** `gh api repos/NeatooDotNet/RemoteFactory/pages` returns **404 Not Found**; there is no `_config.yml` anywhere in the tree, `.github/workflows/` holds only `build.yml`, and no `github.io` URL appears in the repo. There is no rendered docs site, so a site URL in `PackageReleaseNotes` would 404. The reviewer's alternative is the correct target.

## Triage

| # | Finding | Affects | Priority | Size | Disposition |
|---|---|---|---|---|---|
| M1 | Page must carve out v1.0.0's published commitment; v1.5.0's blockquote is scoped to the event surface | AC-5 | Must | punch | **Amend** — the blockquote names this surface and cites `v1.0.0.md#api-stability-commitment`; the Framework bullet stops claiming the v1.5.0 idiom is reusable as-is |
| M2 | Headline false for class-level `internal static`; migration rule would prescribe a needless change | AC-5 | Must | punch | **Amend** — the page carries the three-placement table from `ClassFactoryWithExecute.cs`; the migration rule is scoped to bare `public static` / `private static` that need the server |
| M3 | "No signature changed" is false — auth'd class `[Execute]` now generates `Task<T?>` | AC-5 | Must | punch | **Amend** — header line names it; the Bug Fix entry gets a migration line; the fix is described as "now compiles, result typed nullable", not "was impossible" |
| M4 | Headline needs the auth-forces-remote qualification | AC-5 | Must | punch | **Amend** — stated beside the placement table, citing EXRM-002's pins |
| S1 | Mixed-version deployment hits the wire refusal | AC-5 | Should | punch | **Amend** — one deployment sentence: upgrade both tiers together |
| S2 | `PackageReleaseNotes` target would 404 | AC-5 | Should | punch | **Amend** — GitHub blob URL for the release-notes index (user decision: same choice, resolving form) |
| S3 | The nav_order rule is documented in two places, corrected in neither | AC-5 | Should | punch | **Amend** — corrected in both `CLAUDE.md` and `index.md`'s template |
| S4 | Section list omits `Breaking Changes` and `Commits` | AC-5 | Should | punch | **Amend** — both added to the Framework bullet and Step 2 |
| S5 | Not-established list inconsistent between plan sections | AC-5 | Should | punch | **Amend** — #97 included; interface factories split into their own caveat (Q6) |

Theoretical items folded into the same amendment: the eleven-not-twelve count, and `**Released:**` adopted as the header idiom (v1.8.1, the most recent page). The collision observation needs no action — the plan already declines to repair the v0.x tail. **User decision (2026-09-08):** accept all as proposed; additionally fix CLAUDE.md's stale `net8.0;net9.0;net10.0` TFM claim in the same edit, and use the blob URL. The 2.0.0 alternative was offered a second time, with v1.0.0's commitment in hand, and declined — 1.9.0 stands.
