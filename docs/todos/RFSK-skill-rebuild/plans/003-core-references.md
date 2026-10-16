# Core references as compiled mechanism

**Plan #:** 003
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (no public API; three regions added, no behaviour change)
**Code-review opt-in:** Yes (six pages restate documented rules and generator behaviour; the review checks them against VisionAlignment and the generator)
**Branch:** RFSK-003-core-references
**PR:** —

---

## Scope

Rebuild `class-factory.md`, `interface-factory.md`, `static-factory.md`, `service-injection.md`, `setup.md` and `anti-patterns.md` as mechanism pages under `SKILL.md`'s model: every C# block a `src/Design` region, every RIGHT half of an anti-pattern replaced by a sentence or an embed, and the WRONG blocks kept short and named after Design's types. The repository-named interface factories go (#112), the blocks naming the retired `SkillConsultation`, `SkillEmployeeCommands` and `SkillEmployeeEvents` types go (#148), method injection stops being "Server Only", children stop being taught as an N+1 problem, and the child `Create` is shown `public`. Each link `SKILL.md` makes into these pages resolves to an embed there. Does NOT touch `factory-events.md`, `trimming.md`, `polymorphic-hierarchy.md` or `advanced-patterns.md` (004).

---

## Intent

- A reader following a trap row's "Copy this" link lands on compiled code that shows exactly what the row claims.
- None of the six pages teaches a pattern `SKILL.md` rules out: no repository as an interface factory, no `[Remote, Create]` that calls a service for nothing, no N+1 framing, no private-setter recipe.
- An editor finds no category of hand-written example left to copy from.

---

## Framework & Architectural Alignment

- `SKILL.md`'s model and decisions (RFSK-002), which these pages sit under; VisionAlignment sections 0, 2 and 4 behind them.
- R-A and `.claude/rules/design-snippets.md`; a WRONG block shows a forbidden pattern and nothing else.
- The pages are self-contained: they link only within `skills/RemoteFactory/`.

---

## Constraints & Invariants

- No existing region is renamed or changed; three are added, each pinned by a Design test.
- Every C# fence in the six pages is an embed or a block whose first line is a `// WRONG` comment.
- Facts that are mechanism, not doctrine, survive the rewrite when they are true: the factory-interface visibility table, the constructor-shape rule for ordinal serialization, the `[AuthorizeFactory<T>]` scope model on interfaces, the generated `Can{Method}` surface, CS0051, dropped default parameter values.
- Both solutions build and test green; `mdsnippets` changes nothing outside these pages but attribution lines.

---

## Steps

1. Add the three regions the pages need and Design lacks: the page's injected static-command delegate and its call, and the client's own service registration.
2. Rebuild `class-factory.md` around `Order`: the operations, `IFactorySaveMeta` routing, the lifecycle hooks, the class-level `[Execute]` seam, the list cascade, and the visibility rules with `OrderLine` as the mixed-visibility example.
3. Rebuild `interface-factory.md` around `IProductCatalog` and `ICustomerDirectory`: contract, plain implementation, server registration, the client call, and authorization.
4. Rebuild `static-factory.md` around `OrderCommands`: the commands, how a caller gets and calls the generated delegate, the bare `[Execute]`, and the rules as prose.
5. Rebuild `service-injection.md`: constructor injection on both tiers, `[Service]` parameters where the method runs, children reached on the server, and each tier's registrations.
6. Rebuild `setup.md`: packages, server, client, modes, the page's factory and command calls.
7. Rebuild `anti-patterns.md`: each mistake one short WRONG block in Design's names, the fix in a sentence that names where the compiled right shape is.
8. Run `mdsnippets`, check the diff, build and test both solutions.

---

## Acceptance

- [ ] Every C# block in the six pages is an embedded `src/Design` region or a short block labelled WRONG `[explicit-skip: grep of the fences]`
- [ ] The three new regions are pinned by Design tests on the real containers `[integration]`
- [ ] No page names an interface factory as a repository, or a type EXSET retired `[explicit-skip: grep]`
- [ ] No page states N+1 as the reason a child operation is `internal`, or titles method injection "server only" `[explicit-skip: grep; code review]`
- [ ] Each `SKILL.md` link into these pages lands on an embed showing what the row claims `[explicit-skip: code review]`
- [ ] No sentence contradicts a VisionAlignment ruling or the generator's behaviour `[explicit-skip: code review]`
- [ ] `mdsnippets` runs clean; both solutions build and test green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

Walked 2026-10-16 on the arc at `606c1cf`.

- The six pages hold 53 C# fences; 21 are embeds. Hand-written: `class-factory.md` 6 (an `Employee : IFactorySaveMeta`, the `[JsonInclude]` private-setter recipe, `ISkillConsultationFactory`, `IConsultationFactory` usage, an `OrderLine` with `int Id` and an `internal` `Create`, a `Department` with `IDeptRepo`); `interface-factory.md` 4 (`IMyRepository` WRONG/RIGHT, `EmployeeRepository`, two registration lines); `static-factory.md` 4 (`SkillEmployeeCommands` usage, a WRONG/RIGHT `_SendNotification`, the `void`/`Task`/`Task<T>` trio, `partial`); `service-injection.md` 6 (stored service WRONG/RIGHT on `[Remote, Create] public`, `Employee` with `IAssignmentFactory`, the N+1 pair, registrations); `setup.md` 1 (`SkillEmployeeCommands`/`SkillEmployeeEvents`, the second is the `[Event]` API removed in v1.5.0) plus an XML package block; `anti-patterns.md` 12, all WRONG plus a hand-written RIGHT.
- Wrong claims to drop: `anti-patterns.md` #6 says a missing `partial` is CS0260 (a class factory falls back to the named shape; a static class gets NF0101); #1 and `service-injection.md` frame `[Remote]` on a child as N+1; `service-injection.md:47` titles method injection "Server Only"; `class-factory.md:288` shows a child `Create` as `internal`.
- Regions to embed that exist: `save-ifactorysavemeta`, `collection-factory-basic`, `orderline-child`, `orderline-duality`, `save-usage`, `readme-client-usage`, `interface-factory-usage`, `aspnetcore-service-registration` (server registrations ending in `RegisterMatchingName`), `service-injection-basic`, `operations-create-method`.
- Missing: the page's command delegate is an `[Inject]` property at `Home.razor:170-171`, called in `ExecuteCommand` at `:331-343`; the client's own registration, `AddScoped<ITextScorer, TextScorer>()`, is at `Design.Client.Blazor/Program.cs:85`, outside `getting-started-client-program`. Pins: `StaticFactoryTests` for `SendNotification`; `LocalExecuteTests.Execute_*_WithoutRemote_RunsOnClient` for the client's `ITextScorer`.
- The generator never reads a factory method parameter's default value (`HasExplicitDefaultValue` appears only for constructors, `FactoryGenerator.Types.cs:565`), so anti-pattern #11 stands.

---

## Punchlist

- 

---

## Test Evidence

The flip step was not run, as in RFSK-001: temporary production edits are refused by the permission classifier. No behaviour changed; the three regions and three comment edits are markers and doc comments.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Every C# block an embed or a WRONG block | Required | `[explicit-skip]` | per page, fences = embeds + WRONG: class-factory 11 = 11 + 0, interface-factory 6 = 6 + 0, static-factory 7 = 7 + 0, service-injection 7 = 6 + 1, setup 9 = 9 + 0, anti-patterns 15 = 0 + 15 | ✓ |
| The three new regions pinned | Required | `[integration]` | `skill-command-inject`, `skill-command-call`: resolving the generated `ExampleCommands.SendNotification` delegate on the client and calling it is `StaticFactoryTests.Execute_SendNotification_ReturnsResult`. `skill-client-services`: the client resolving `ITextScorer` for a bare `[Execute]` is `LocalExecuteTests.Execute_StaticFactory_WithoutRemote_RunsOnClient` and `…Execute_ClassFactory_WithoutRemote_RunsOnClient`. The three regions sit in `Design.Client.Blazor`, which `Design.sln` compiles but no test runs; the tests run the same calls through `DesignClientServerContainers`, which registers the client by `RegisterMatchingName`. This is the arrangement the existing `getting-started-client-program`, `skill-blazor-inject` and `skill-blazor-usage` regions already have | ✓ (through the mirroring containers) |
| No repository interface factory, no retired type | Required | `[explicit-skip]` | grep of the six pages: no `IEmployeeRepository`, `IMyRepository`, `IDeptRepo`, `IAuthorizedRepository`, `SkillConsultation`, `SkillEmployee*`, `IConsultationFactory`; every "repository" is the corrected framing | ✓ |
| No N+1 reason, no "server only" title | Required | `[explicit-skip]` | grep: no "N+1"; method injection is "the tier that runs the method"; code review | — |
| `SKILL.md` links land on embeds | Required | `[explicit-skip]` | code review | — |
| No contradiction with a ruling or the generator | Required | `[explicit-skip]` | code review | — |
| mdsnippets clean; both green | Required | `[explicit-skip]` | `reviews/003-*.log`: unit 871, integration 664 (5 skipped), Design 177 (1 skipped), both TFMs; outside the skill, `docs/factory-operations.md` changed by one embedded comment line | ✓ |

---

## Gate Record

- 

---

## Plan Amendments

---

## Notes

- Loaded: `SKILL.md` as rebuilt by RFSK-002, VisionAlignment sections 0, 2 and 4, `.claude/rules/design-snippets.md`.
