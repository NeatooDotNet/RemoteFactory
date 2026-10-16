# SKILL.md: model, trap table, pointers, pains

**Plan #:** 002
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-6, AC-9, AC-12
**Status:** In Progress
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (no code, no public API; the code review below checks the doctrine)
**Code-review opt-in:** Yes (the page restates documented rules: VisionAlignment sections 0, 2 and 4 are the rules it must not contradict)
**Branch:** RFSK-002-skill-model-and-traps
**PR:** —

---

## Scope

Rewrite `skills/RemoteFactory/SKILL.md` in the Neatoo skill's order: the five model statements RemoteFactory owns; the eleven trap rows as a table on the first screen after the model, each row the question, the answer and the compiled example to copy; a pointer paragraph naming the Neatoo skill's questions and its "Neatoo, or plain RemoteFactory" decision; the decisions (the three factory shapes, the three mechanisms of VisionAlignment section 2, the regions 001 added); the known pains with their issue or analyzer; then the reference index. The two hand-written blocks and the "Partial/illustrative snippets" policy go, the trigger description drops "lazy loading" and "deferred loading", no ruling ID appears in prose, about 400 lines. Does NOT rebuild the reference pages (003, 004); a row whose example lives in a reference links to it.

---

## Intent

- A reader, or a Claude session, holding a habit-shaped question finds the answer on the first screen after the model, with the compiled example to copy beside it.
- The two skills read as one list: the eleven questions the Neatoo skill points at are answered here in the same words, and this page points back at the Neatoo skill's questions without copying them.
- The page stops teaching the four things the audit found: "persistence routing" as the headline, N+1 as the reason child operations are `internal`, an interface factory named as a repository, and `[Remote, Create]` as the lead example.

---

## Framework & Architectural Alignment

- VisionAlignment section 2 (statements 1–4, 6 and the three mechanisms), section 4 (rows T3–T6, T11's RemoteFactory half, T13–T15, T18–T20), section 0 (D3, D11–D15, D18, D22–D24, R-C, R-D, R-G, R-H, R-I, R-K) and section 5 (F1–F11).
- The Neatoo skill's shape (`Neatoo/skills/neatoo/SKILL.md`, 337 lines): model → "The questions the habit asks" (`| The habit asks | … answer | Copy this |`) → pointer paragraph → decisions → known pains → references. Its pointer block lists RemoteFactory's eleven questions by topic, with no T-numbers, at its line 42.
- R-A and `.claude/rules/design-snippets.md`: every C# block is an embedded region; placeholders written as a pair; `mdsnippets` log outside the tree.

---

## Constraints & Invariants

- The model statements keep VisionAlignment's sentences; only the ruling-ID parentheticals go (AC-6), statement 4's "Whether a child's `[Create]` is the exception is Q1" becomes R-C's ruling (public and local), and the lazy-load clause is the on-demand child-list `[Fetch]` (R-K).
- Each trap row's question is the Neatoo pointer block's sentence, word for word.
- No answer contradicts a ruling in VisionAlignment section 0, and no answer states a defect as current that has shipped a fix (#113, #117, #122 are fixed).
- Every embed resolves, and `mdsnippets` changes no other file beyond attribution lines.
- The reference index lists every file under `references/` and no file that is not there.

---

## Steps

1. Write the frontmatter: the description leads with the habit questions as the Neatoo skill's does, keeps the mechanism triggers that are current, and drops the lazy-loading triggers.
2. Write the opening and the model: what RemoteFactory is (the one-assembly premise, not persistence routing), then the five statements.
3. Write the trap table: eleven rows, each the Neatoo pointer block's question, the answer from VisionAlignment section 4 corrected to the rulings, and a "Copy this" cell that names an embed on this page or a reference section that embeds the region.
4. Write the pointer paragraph to the Neatoo skill: its questions by topic, its "Neatoo, or plain RemoteFactory" decision, and that the business-logic placement is its.
5. Write the decisions: which factory shape (class, interface as a domain service, static command, plus the context and the read model), where `[Remote]` goes, constructor versus `[Service]`, visibility and trimming, and one class in two roles, embedding the regions 001 added where the row needs code on the page.
6. Keep a short mechanism quick reference for what is not a trap (events, authorization, trimming, serialization), each row correct under the rulings and pointing at its reference.
7. Write the known pains from VisionAlignment section 5, each with its issue or the analyzer it waits for, current as of today.
8. Write the reference index, then run `mdsnippets` and check the diff.

---

## Acceptance

- [ ] `SKILL.md` opens with the five model statements before any prohibition `[explicit-skip: prose; checked by the code review]`
- [ ] The eleven trap rows follow the model, each question identical to the Neatoo pointer block's `[explicit-skip: prose; checked by the code review]`
- [ ] Every C# block on the page is an embedded `src/Design` region, each pinned by a Design test on the real containers `[integration]`
- [ ] No sentence on the page contradicts a VisionAlignment ruling, names a fixed defect as open, or carries a ruling ID `[explicit-skip: prose; checked by the code review]`
- [ ] The page points at the Neatoo skill's questions and its "Neatoo, or plain RemoteFactory" decision without copying them `[explicit-skip: prose; checked by the code review]`
- [ ] The known pains name each open defect of VisionAlignment section 5 with its issue or analyzer `[explicit-skip: prose]` · Optional
- [ ] The trigger description and the page carry no LazyLoad guidance `[explicit-skip: grep]` · Optional
- [ ] `mdsnippets` runs clean; both solutions build and test green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

Walked 2026-10-16 on the arc at `95befa1`.

- `skills/RemoteFactory/SKILL.md`: 137 lines, version 1.0.0, no embeds. Two hand-written blocks (lines 18–41: `DashboardViewModel` with `[Fetch][Remote]` and `IOrderRepo`; an `Order : EntityBase<Order>`). The patterns table (54–58) names `IOrderRepository` as the interface-factory example. The Quick Decisions table (62–90) has 28 rows; four contradict the rulings: "[Remote]? Only aggregate root entry points" (64), "private setters? No - breaks serialization" (66, true but a mechanism), "child entities [Remote]? No - causes N+1" (70, D18), "child entity methods internal? Yes" (71, R-C). The Code Sample Sources section (114–137) sanctions hand-written partial examples (#152).
- The Neatoo skill's pointer sentence (its line 42) is the source of the eleven questions.
- VisionAlignment's T19 answer says plain-`Task` `[Execute]` "does not compile until #113 lands"; #113 is closed (fixed in v1.10.2), as are #117 (v1.10.5) and #122 (v1.10.4). Open: #97, #104, #114, #123, #131, #135, #141. Analyzers pending (R-L, Todo 3): non-public setter on a `[Factory]` class, `[Factory]` class without `partial`, class-level `[Execute]` not returning `Task<T>` of the containing type.
- Regions the references already embed that rows can link to: `operations-class-execute` (class-factory.md), `orderline-child` and `service-injection-constructor`/`-server-only` (service-injection.md), `orderline-duality` and `collection-factory-basic` (advanced-patterns.md), `operations-execute`/`attributes-execute-local` (static-factory.md), `interface-factory-contract` (interface-factory.md), `skill-blazor-usage` (setup.md). The context, the read model, the three class-level placements, `skill-orderline-root-and-child` and `skill-order-fetchdraft` are embedded nowhere yet.

---

## Punchlist

- 

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| | | | | |

---

## Gate Record

- 

---

## Plan Amendments

---

## Notes

- Loaded: the Neatoo skill's `SKILL.md`, VisionAlignment sections 0–5, `.claude/rules/design-snippets.md`. The project declares no design skill for Step 2.
