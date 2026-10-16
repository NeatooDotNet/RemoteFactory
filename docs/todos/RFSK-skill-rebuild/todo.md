# Rebuild the RemoteFactory Skill Around the Model and the Trap Rows

**ID:** RFSK
**Type:** Enhancement
**Status:** In Progress
**Created:** 2026-10-16
**Last Updated:** 2026-10-16
**Initial split:** 6 plans
**Plan cap:** 9 — counts plan numbers issued, including Abandoned and Retired. Issuing a number above the cap is a stop-and-ask.
**Arc branch:** RFSK — cut from main at Step 1; every plan and punchlist branch PRs into it; it PRs into main at Step 8.
**Related:** Issue #163 is the brief (the Neatoo skills session, 2026-10-16); this todo closes it. Adopts #112, #148, #149, #152 (and #150, Optional), parked for "Todo 2" by EXSET. Sibling of Neatoo's SKR (complete 2026-10-12): SKR owns model statements 5, 7, 8, 9 and the Neatoo rows; this todo owns 1–4, 6 and the RemoteFactory rows. `docs/VisionAlignment.md` R-B, sections 2, 4, 5 and 7 are the ruled basis. No other arc is open.
**Standing orders:** Unattended from the moment RFSK-001 is drafted. Claude merges plan and punchlist PRs into the arc and takes every reviewer recommendation that does not contradict a line in this header or a ruling in `docs/VisionAlignment.md`; it parks, never decides: a priority change, a Goal or Out of Scope edit, a plan number beyond the initial split, a sentence attributed to Keith, and the AC-11 wording until the Neatoo session sends it. RFSK-006 may overwrite `~/.claude/skills/RemoteFactory` with the repository copy and may run the headless fresh-session `claude -p` sessions. A test change the documented intent justifies is made and reported in the PR; an out-of-scope test failure, or a test-versus-code disagreement no documented intent covers, is parked as an issue labelled `question` and stops the loop (Keith, 2026-10-16).

---

## Goal

Someone building an application, human or Claude, opens the RemoteFactory skill beside the Neatoo skill and can decide, for any type or operation, which factory shape it takes, which attribute it gets and which tier it runs on. `SKILL.md` leads with the five model statements RemoteFactory owns, then the eleven habit-trap rows it owns, each at its decision point with a compiled example, then links to the mechanism references. Every code block is a `src/Design` region, pinned by a Design test. The two skills read as one list: each points at the other's statements and rows, and nothing is copied.

## Acceptance Criteria

- [ ] **AC-1** · Required — `SKILL.md` opens with the five model statements RemoteFactory owns, verbatim from VisionAlignment section 2, before any prohibition.
- [ ] **AC-2** · Required — The eleven habit-trap rows follow the model, each with the question, the answer and a compiled example, worded to match the Neatoo skill's pointer block.
- [ ] **AC-3** · Required — Every C# block in the skill is a `src/Design` region or a short block labelled WRONG, and the editing guidance says so; closes #148 and #152.
- [ ] **AC-4** · Required — No skill page names an interface factory as a repository; closes #112.
- [ ] **AC-5** · Required — `polymorphic-hierarchy.md` states attribute inheritance the way the generator does it; closes #149.
- [ ] **AC-6** · Required — `SKILL.md` points at the Neatoo skill's statements, rows and its "Neatoo, or plain RemoteFactory" test, and no ruling ID appears in skill prose.
- [ ] **AC-7** · Required — A fresh session given only the installed skills falls into no RemoteFactory trap row across the five SKR-007 scenarios and new scenarios for the five rows those never exercise.
- [ ] **AC-8** · Required — `~/.claude/skills/RemoteFactory` matches the repository copy at close.
- [ ] **AC-9** · Optional — The skill's trigger words and reference pages carry no LazyLoad guidance.
- [ ] **AC-10** · Optional — The Person example drops the private-setter recipe `trimming.md` no longer prescribes; closes #150.
- [ ] **AC-11** · Optional — The "where does business logic go" row carries the wording the Neatoo session sends.
- [ ] **AC-12** · Optional — The skill names each known defect in VisionAlignment section 5 with its issue or analyzer.

## Out of Scope

- Edits to the Neatoo skill; its pointer block already lists the eleven questions.
- The LazyLoad removal itself (#114); AC-9 trims guidance, nothing more.
- Library and generator behaviour changes, and the analyzers of R-L (Todo 3).
- `docs/` pages, `README.md` and `CLAUDE-DESIGN.md`, except where a region they embed changes under them.

---

## Trap rows and their regions

VisionAlignment section 4 holds the rows and the answers. This table is the Step 1 recon: which `src/Design` region each row embeds, and what RFSK-001 must add. The Neatoo skill's pointer block (`skills/neatoo/SKILL.md:42`) lists the eleven questions by topic, with no T-numbers; the T-numbers live only in the todos.

| Row | Compiled example | State |
|---|---|---|
| T3 load-or-create | `operations-class-execute` (`OpenDraft`), `readme-client-usage` | `FetchDraft` behind it is in no region (001) |
| T4 Create from loaded data | `operations-create-method`, `operations-fetch-instance`, `orderline-child` | exists |
| T5 client calls the repository | `operations-fetch-instance` (`[Service]` repository); `interface-factory-contract` (domain service) | exists; the skill's `IOrderRepository`/`IEmployeeRepository` go (003) |
| T6 application service | `operations-execute`, `operations-class-execute`; context | `Contexts/OrderCheckout.cs` has no region (001) |
| T11 RF half | `operations-insert`/`operations-update`, `collection-factory-basic`, `orderline-child` | exists |
| T13 Save returns a new instance | `save-usage`, `skill-blazor-usage`, `aspnetcore-error-handling` | exists |
| T14 constructor vs `[Service]` | `service-injection-constructor`, `service-injection-server-only`, `attributes-execute-local` | exists; `OrderCheckout`'s constructor is the better constructor example (001) |
| T15 child `[Insert]` is `internal` | `orderline-child` + `orderline-duality` | two regions; a side-by-side block needs a wrapping region (001) |
| T18 `[Remote]` is the crossing | `operations-create-method` vs `operations-fetch-instance`; `OrderCheckout.Fetch` | the context call is in no region (001) |
| T19 `[Execute]` obeys `[Remote]` | `attributes-execute-local` vs `operations-execute`; class-level | `ClassFactoryWithExecute.cs` has no region (001) |
| T20 DTO from a controller | read model; `getting-started-server-program` | `OrderSummary` has only its Fetch in a region (001) |

---

## Plan Index

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-design-regions-for-trap-rows](./plans/001-design-regions-for-trap-rows.md) | Design regions the trap rows embed | AC-2, AC-3 | Done | #164 |
| 002 | [002-skill-model-and-traps](./plans/002-skill-model-and-traps.md) | SKILL.md: model, trap table, pointers, pains | AC-1, AC-2, AC-6, AC-9, AC-12 | Done | #166 |
| 003 | [003-core-references](./plans/003-core-references.md) | Core references as compiled mechanism | AC-3, AC-4 | Done | #168 |
| 004 | [004-events-trimming-hierarchy-references](./plans/004-events-trimming-hierarchy-references.md) | Events, trimming, hierarchy, advanced references | AC-3, AC-5, AC-9 | Done | — |
| 005 | [005-person-example-recipe](./plans/005-person-example-recipe.md) | Person example drops the private-setter recipe | AC-10 | Draft | — |
| 006 | [006-fresh-session-and-sync](./plans/006-fresh-session-and-sync.md) | Fresh-session test, then sync | AC-7, AC-8 | Draft | — |

AC-11 rides whichever plan is open when the Neatoo session sends the wording; if none is, it is a punchlist row.

---

## Punchlist

- 

## Dismissed

- 

## Parked

- #165 · A generated factory cannot be declared internal · RFSK-002 code review (F6)
- #167 · Design.Client.Blazor is compiled but never run by a test · RFSK-003 test review

---

## Discovery Log

### 2026-10-16 — RFSK-001 · serves AC-2
- **Finding:** No Design test showed a class-level `[Remote, Execute]` crossing the wire; the control used a static command.
- **Decision:** Amend
- **Follow-up:** n/a

---

## Skipped Steps

- RFSK-001 Step 5 flip: temporary production edits refused by the permission classifier; regions-only plan, cited tests gated in EXSET.

---

## Sibling Todos

- Neatoo SKR (`Neatoo/docs/todos/completed/SKR-neatoo-skill-rebuild/todo.md`), complete 2026-10-12. Its plan 007's fresh-session prompts and scoring are reused by RFSK-006. Cross-repository, so its branches never touched this arc.

---

## Close-Out Audit

---

## Docs & Retro

---

## Results / Conclusions

