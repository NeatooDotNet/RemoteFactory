# Requirements Reviewer — EXRM-005 Release 1.9.0

Last updated: 2026-09-08
Current step: Step 2 plan review delivered (verdict CONCERNS). Report returned to team-lead; orchestrator writes it to `reviews/`.

## Key Context

- Orchestrator brief said **review only, edit no file**; findings go back in the report. Only this memory file was written.
- **The strongest requirement in this arc is not CLAUDE.md — it is `docs/release-notes/v1.0.0.md:198-206`**, a *published* API Stability Commitment: "Minor releases (1.x.0) — additive features only. Existing code keeps working," and "generated code shapes, and runtime contracts ... are all considered part of the public surface." The plan never cites it. CLAUDE.md's CI/CD standard is the weaker, internal restatement.
- v1.5.0 (`v1.5.0.md:15`) is the in-repo mechanism for departing from that commitment: a `> **Pre-stable API note.**` blockquote **scoped to the event-shaped surface**. Reusing it verbatim for an `[Execute]` break would publish a carve-out that does not cover this change.
- Breaking-under-minor is established practice in the 1.x line (v1.1.0, v1.4.0, v1.5.0), so the departure is a recorded decision, not veto-tier.
- **The headline "a bare `[Execute]` runs where it is called" is wrong for two cases**: class-level `internal static` bare `[Execute]` is server-only/guarded (`ClassFactoryRenderer.cs:420,844,897,1115,1381` — `isServerOnly: method.IsInternal || method.IsRemote`; Design: `ClassFactoryWithExecute.cs:153-188`); and `[AspAuthorize]` or a `[Remote]` auth method forces remote (`FactoryModelBuilder.cs:540-542`). Static factories have no internal variant — `[Remote]` alone drives the guard there (`StaticFactoryRenderer.cs:261`).
- **"No signature changed" is false for one shape.** EXRM-002's CS8603 fix set `isNullable = method.IsNullable || authorization.HasAuth` at `FactoryModelBuilder.cs:486-487`, so a class-level `[Execute]` under `[AuthorizeFactory]`/`[AspAuthorize]` now returns `Task<T?>`. By v1.0.0's own definition that is a public-surface change.
- **Mixed-version deployment hits EXRM-001's wire refusal.** A 1.8.1 client still emits remote registrations for bare `[Execute]`; a 1.9.0 server refuses them via `LocalOnlyDelegateRegistry` (`StaticFactoryRenderer.cs:148-155`). Only reachable if client and server are deployed apart.
- **Version sweep is clean.** Grep found no hardcoded current version outside `src/Directory.Build.props:18-19`. `docs/` and `skills/` use `x.y.z` placeholders; README uses unversioned `dotnet add package`. Plan's Scope is adequate.
- **No docs site is configured in this repo** — no `_config.yml` anywhere, no Pages workflow. So the `PackageReleaseNotes` "release-notes index URL" target is unverifiable from the repo and may 404.
- **33 pages do share `nav_order: 3`** — verified by counting. But **v1.7.0 is one of the 33**; existing 1.x pages number 11 (v1.8.1=1 … v1.0.0=11), so "shifts those twelve" only holds if the new page is counted.
- `nav_order: N  # ... increment existing pages` also lives in `docs/release-notes/index.md:138`, a second documented place the plan's departure contradicts and does not correct.
- CLAUDE.md:235 lists `Commits` among required sections; v1.7.0 has one, v1.8.1 does not. The plan's section list omits both `Breaking Changes` (despite a "Yes" header line) and `Commits`.

## Mistakes to Avoid

- Do not treat CLAUDE.md as the top requirement source for release scope. The published release-notes pages (v1.0.0's commitment, v1.5.0's carve-out idiom) are consumer-facing and outrank it.

## User Corrections

None received in this run. Recorded user decisions relied on: v1.9.0 minor bump (todo header); breaking-changes line reads "Yes — behaviour, not API"; `PackageReleaseNotes` points at the release-notes index; 2.0.0 offered and declined (plan Notes line 130).
