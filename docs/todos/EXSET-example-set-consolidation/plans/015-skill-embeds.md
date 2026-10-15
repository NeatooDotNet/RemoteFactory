# Skill-only embeds

**Plan #:** 015
**Date:** 2026-10-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-15
**Plan-review opt-in:** No. Most embeds re-point to regions 002–010 built and reviewed, and the new regions sit on Design code those plans' tests pin.
**Code-review opt-in:** Yes. The re-pointed embeds change what the skill shows, and the notes attached to them must match.
**Branch:** EXSET-015-skill-embeds
**PR:** —

---

## Scope

Split from EXSET-010 at its Step 2 (2026-10-15). Re-point the skill's remaining reference-app embeds to `src/Design`.

**Reused regions:**

| Skill embed | Page | Design region(s) |
|---|---|---|
| `skill-class-factory-complete` | `class-factory.md` | `readme-domain-model` (Order's operations) |
| `skill-lifecycle-hooks` | `class-factory.md` | `operations-lifecycle-onstart`, `operations-lifecycle-oncomplete` |
| `skill-class-execute` | `class-factory.md` | `operations-class-execute` |
| `skill-interface-factory-complete` | `interface-factory.md` | `interface-factory-contract`, `interface-factory-implementation` |
| `skill-entity-duality` | `advanced-patterns.md` | `orderline-child`, `orderline-duality` |
| `skill-complex-aggregate` | `advanced-patterns.md` | `order-interface`, `operations-create-method`, `readme-client-usage` |
| `skill-static-execute-commands` | `static-factory.md` | `operations-execute`, `operations-execute-command` |

**New Design regions, keeping the migrating names:**
- `skill-interface-factory-auth-authclass`, on `ICustomerDirectoryAuth`;
- `skill-interface-factory-auth-factory`, on the whole `ICustomerDirectory`;
- `skill-value-object-factory`, on the whole `TaxRate` record;
- `skill-blazor-usage`, on `Design.Client.Blazor`'s Home page code.

**Becomes prose:** `skill-interface-factory-auth-impl`. The implementation is a plain class, and Design's auth implementation uses test flags, so it is not a shape to copy.

**Notes attached to an embed** that would contradict the Design code it now shows are corrected in one line each:
- the complex aggregate's "constructor injection on the collection";
- the value object's "public setters".

The rest of the skill's prose is Todo 2's.

**Findings to fix:** C-11, C-14, C-15, C-19, C-20, C-02, I-05 and U-02's skill shares.

**Delete** the reference-app `Samples/Skill` sources whose every region retires, and strip the markers from any that other code still uses.

Does NOT touch:
- `references/lazyload.md` (EXSET-011);
- the skill's other prose (Todo 2).

---

## Intent

Every C# embed in the skill comes from Design, and the line that introduces each embed describes the code it shows.

---

## Framework & Architectural Alignment

- **Rulings:** D14, R-C, R-E, R-F, R-G, R-J and R-M.
- **`.claude/rules/design-snippets.md`.**
- **`CLAUDE.md`'s skill rule:** the skill is self-contained, so its prose may not link repository files.

---

## Constraints & Invariants

- **Embeds.** Every skill embed is a Design region (`lazyload.md` excepted, EXSET-011).
- **Regions.** Each region name is defined once in the repository.
- **The whole still works:** both solutions green, mdsnippets swallows nothing, the reference app builds.

---

## Steps

1. The new Design regions.
2. Re-point the skill markers, and correct the attached notes.
3. Delete or strip the reference-app sources, then build `EmployeeManagement.sln`.
4. Verify and record: mdsnippets, both solutions, the ledger.

---

## Acceptance

- [ ] Every skill embed outside `lazyload.md` is a Design region. No region is defined twice, and none retired is embedded. The reference app builds. `[explicit-skip: greps; EmployeeManagement.sln build]` · Must
- [ ] Each corrected note matches the code it introduces `[explicit-skip: read against the region]` · Must
- [ ] The embedded regions are pinned by Design tests `[explicit-skip: each region's test named in Test Evidence]` · Must
- [ ] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **14 skill embeds** come from the reference app's `Samples/Skill` and `Client.Blazor/Samples/Skill`, plus `lazyload.md`'s 2.
- **Design has every shape:** Order, OrderLine, OrderCommands, CustomerPreferences, IProductCatalog, CustomerDirectory, TaxRate and the Blazor Home page.
- **The old samples carry findings:**
  - `[Remote, Create]` with an unused repository (U-02);
  - a business `Status` set in a `[Create]`;
  - a `[Create]` with a default parameter (I-05);
  - a `FactoryStartAsync` "validation" comment (C-11);
  - public concretes (C-19).

---

## Punchlist

---

## Test Evidence

| Bullet | Pinned by | Tier |
|---|---|---|
| 1. Skill embeds | explicit-skip: every `snippet source` link under `skills/` is under `src/Design` except `lazyload.md`'s two (EXSET-011). The four new keys are each defined once. No retired `skill-*` key is defined or embedded. Ten `Samples/Skill/*.cs` files and `BlazorUsageSamples.cs`/`.razor` deleted; `LazyLoadSamples.cs` stays for EXSET-011; `reviews/015-refapp-build.log` (0 errors) | explicit-skip |
| 2. Corrected notes | explicit-skip: each note read against its region. `class-factory.md`'s "Generates" names `IOrderFactory`; the value-object lead and note match TaxRate (a positional `[Factory]` record, no setters); the complex aggregate's note matches `readme-client-usage` (lineFactory.Create, then AddLine); the duality prose names the two OrderLine Fetch signatures; `static-factory.md`'s "Generates" names OrderCommands' delegate types (`OrderCommandsFactory.g.cs:16-17`); `interface-factory.md` names `CustomerDirectory` and `ICustomerDirectoryAuth` | explicit-skip |
| 3. Embedded regions pinned | `skill-interface-factory-auth-authclass` and `-factory` (`InterfaceFactoryAuthorizationTests`; `ListContacts` by the new `InterfaceFactory_ListContacts_ReturnsContacts_AndDeniesTheDeniedCustomer`, added at the gate), `skill-value-object-factory` (`ValueObjectTests.TaxRate_LoadsFromTheServer_AndAppliesToMoney`, `TaxRate_IsNull_ForAnUnknownRegion`). `skill-blazor-usage` is page code: `Design.Client.Blazor` compiles it, and its calls are pinned on the client container: local Create by `AggregateTests.Order_CreateAndEditLines_RunOnClientWithoutServerCall` (0 remote calls); Fetch, lineFactory.Create then AddLine, and Save by `AggregateTests.Order_EditLinesFromClient_SavesExactlyThoseChanges`; Create, Save and Fetch by `ClientServerTestingTests.Order_SavesAndFetches_FromTheClientScope`. The reused regions keep the tests 002–010 cite | explicit-skip |
| 4. Meta | explicit-skip: `reviews/015-mdsnippets.log` (it extracts `skill-blazor-usage` from `Home.razor`); `attributes-reference.md` and both `service-injection.md` change only in attribution. Logs: `015-build.log`, `015-test.log`, `015-design-build.log`, `015-design-test.log` (unit 852; integration 657 + 7 skips; Design 174 + 1 known-bug skip, one test added at the gate; both TFMs) | explicit-skip |

---

## Gate Record

---

## Plan Amendments

- **2026-10-15 · Implementation:**
  - **Attached lines corrected beyond the two notes Scope names:**
    - `class-factory.md`'s "Generates" line;
    - `static-factory.md`'s "Generates" list;
    - the duality prose's two method names;
    - `interface-factory.md`'s auth-class registration names.

    Each described the old embed by name.
  - **Hand-written C# naming retired types stays, as Todo 2's,** and is recorded in the todo's Follow-on: `class-factory.md` "Generated Code" and "Caller Usage", `static-factory.md` "Usage", and `setup.md` "Static Factory Commands".
  - **`skill-blazor-usage` sits in `Home.razor`'s `@code` block;** mdsnippets reads it from there. No test renders the page. Its calls are pinned by `AggregateTests` and `ClientServerTestingTests` (Test Evidence 3).
  - **`ChildEntitySamples.cs`,** stripped of its markers in EXSET-010, is deleted here with `ComplexAggregateSamples.cs`, its only user.
- **2026-10-15 · Gate:** three regions added at the code review, markers only: `order-class-declaration` (Order's `[Factory] internal partial class` line), `ordercommands-declaration`, and `skill-blazor-inject`, a `begin-snippet` razor comment around Home.razor's `@inject` lines, which mdsnippets extracts. The duality sentence now excepts the child's public `[Create]`. Shared pages change only in attribution.

---

## Notes

- **Run unattended** under Keith's standing instruction.
