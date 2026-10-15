# Factory modes and serialization pages

**Plan #:** 013
**Date:** 2026-10-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4, AC-5
**Status:** Done
**Last Updated:** 2026-10-15
**Plan-review opt-in:** No. The scope came out of EXSET-006's reviewed plan (its callouts 7–9 are carried below), and it adds no new Design shape beyond a Logical registration and three pins.
**Code-review opt-in:** Yes (two pages rewritten against library behaviour; a known-bug regression test)
**Branch:** EXSET-013-modes-serialization
**PR:** —

Split from EXSET-006 (Keith, 2026-10-15). It follows 006: its logging and registration embeds reuse 006's regions. Design keeps the default format, because 006 simplified to the happy path.

---

## Scope

Re-point every C# block on `docs/factory-modes.md` and `docs/serialization.md` to `src/Design`, and replace the skill `setup.md`'s hand-written factory-modes block. That block used `NeatooFactory.Local`, which does not exist.

- **Modes.**
  - `modes-server-config` retires into `readme-server-setup`, `modes-remote-config` into `getting-started-client-program`, and `modes-logging` into `aspnetcore-logging`.
  - `modes-logical-config` is a new Logical registration, pinned by a test that runs an Order's create, save and fetch in one container.
- **Serialization.**
  - `serialization-ordinal-versioning` is Order's properties, pinned by a test of `Order.PropertyNames` and the array.
  - `serialization-interface` retires into it: `Order.Lines` is the interface property, pinned by a `$type`/`$value` test in both formats.
  - `serialization-config` and `-debug-named` retire to prose, as 006's format sections did.
  - `serialization-references` retires to prose and a table; there is no Design example (below).
  - `serialization-logging` retires into `aspnetcore-logging`.
- **Reference app.** Delete its `Modes/`, `FactoryModes/` and `Serialization/` samples, together with the unembedded regions in those families.
- **Findings to fix:**
  - C-04, "all operations" in Remote mode (D18);
  - C-07, a `[Factory]` child built with `new`;
  - C-19/C-20, I-01 and T-22;
  - the invented log messages;
  - ordinal order called "alphabetical";
  - the interface wire shape;
  - "resolves from DI" (Named only);
  - "Remote mode only";
  - the LazyLoad section (AC-5).
- **Design's stale statements.** `SerializationTests.cs`'s header guide: private setters, interface collections, circular references, the ordinal shape, and the generated-code illustration.

Does NOT touch: `service-injection.md`'s `serialization-caveat-*` regions (EXSET-010), `skill-blazor-usage` (EXSET-010), the skill's prose (Todo 2).

---

## Intent

- A reader choosing a mode sees D18 stated: Remote sends `[Remote]` operations and runs everything else locally.
- A reader of the serialization page learns what the generator and serializer actually do, pinned by tests:
  - ordinal order and membership;
  - when a type falls back to the Named shape;
  - the interface wire shape;
  - what reference identity survives, in each format.

---

## Constraints & Invariants

- **Embeds.** Every C# block on the two pages, and in setup's factory-modes block, is an embed from `src/Design`. A retired region's definition is deleted, and no page embeds it.
- **No bug hidden in a test.** Library behaviour the page describes as a bug is pinned by a natural test, marked as a known failure. A test is never rewritten to pass.
- **Unchanged regions.** Design's README regions do not change shape.
- **Green.** Both solutions stay green; the reference app builds; mdsnippets swallows nothing.

---

## Steps

1. **Probe reference identity for `[Factory]` types.** STOP point. This found a library bug (Plan Amendments).
2. **Design pins:**
   - Logical mode in `LogicalModeTests`;
   - `Order_OrdinalShape_FollowsPropertyNames` and `Order_LinesInterface_CrossesAsTypeAndValue` in `SerializationTests`;
   - the `serialization-ordinal-versioning` region on Order's properties.
3. **Regression test.** `FactoryReferenceIdentityTests` in `RemoteFactory.IntegrationTests`: the Named cases pass, and the Ordinal cases are skipped as known bug #133.
4. **Rewrite the pages and setup's block; delete the reference-app samples.**
5. **Correct Design's `SerializationTests` guide.**
6. **Verify and record:** mdsnippets, both solutions, the reference app, and ledger outcomes for C-04, C-07, C-19, C-20, I-01 and T-22.

---

## Acceptance

- [x] Logical mode runs an Order's create, save and fetch in one container, `[Remote]` operations included, against that container's store `[integration]` · Must
- [x] Order's ordinal array follows `PropertyNames` in ordinal name order and leaves out the get-only `Total`; `Order.Lines` crosses as `$type`/`$value` in both formats `[integration]` · Must
- [x] Reference identity between `[Factory]` objects is pinned in both formats: Named preserves shared and back references; the Ordinal cases are written naturally and skipped as known bug #133 `[integration]` · Must
- [x] On the two pages and setup's factory-modes block:
  - every C# block is an embed from `src/Design`;
  - no retired region is defined or embedded;
  - none of the findings' text this plan lists remains;
  - the replaced reference-app sources are gone, and the reference app builds.

  `[explicit-skip: grep of fences, markers, #region names and finding text; EmployeeManagement.sln build]` · Must
- [x] mdsnippets' log has no missing snippet and no section was swallowed; both solutions green `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Carried from EXSET-006's recon and plan review.

- **Ordinal order and membership.**
  - Base class first, then `StringComparer.Ordinal` (`FactoryGenerator.Types.cs:410-419`).
  - Only properties with a public, internal or init setter are written (`:436-444`).
  - Order has no non-public setter, so membership is pinned by the get-only `Total` (review callout 7).
- **Named fallback.** A type is written by name unless it is partial, not nested, not a collection, has at least one property to write, and has a usable parameterless constructor, or for a record a primary constructor (`FactoryModelBuilder.cs:308-313`; callout 8).
- **Construction.** Ordinal builds with `new T{...}` (`OrdinalRenderer.cs:355-362`). Named uses DI only for a registered type.
- **Interface wire shape.** `{"$type": FullName, "$value": ...}` (`NeatooInterfaceJsonTypeConverter.cs:83-94`).
- **References.** The generated ordinal converter writes no `$id`/`$ref`, and nothing reads `NeatooReferenceResolver.Current`. Preservation was tested only on plain types.
- **Remote mode.** Only `IsRemote` methods are reassigned to the remote call (`ClassFactoryRenderer.cs:263-275`).
- **Logging.** The messages at `factory-modes.md:75` existed nowhere.

---

## Punchlist

---

## Test Evidence

| Bullet | Pinned by | Tier |
|---|---|---|
| 1. Logical mode | `LogicalModeTests.Logical_RunsRemoteOperations_InTheSameContainer` | integration |
| 2. Ordinal shape; interface shape | `SerializationTests.Order_OrdinalShape_FollowsPropertyNames`; `Order_LinesInterface_CrossesAsTypeAndValue` (Ordinal and Named) | integration |
| 3. Reference identity | `FactoryReferenceIdentityTests`: `Named_SharedReference_…` and `Named_BackReference_…` pass; `Ordinal_SharedReference_…` and `Ordinal_BackReference_…` are skipped as #133. The probe saw them fail as the page describes: two copies, and the cycle exception | integration |
| 4. Pages | explicit-skip: every embed on the pages comes from `src/Design` (setup's `skill-blazor-usage` is EXSET-010's); no `modes-*` or `serialization-*` region is left in the reference app; `reviews/013-refapp-build.log` | explicit-skip |
| 5. Meta | explicit-skip: `reviews/013-mdsnippets.log`; README, getting started and the-problem changed only in attribution lines; `013-build.log`, `013-test.log`, `013-design-build.log`, `013-design-test.log` (unit 839; integration 646 + 7 skips, which are the 5 known and the 2 #133 cases; Design 155; both TFMs) | explicit-skip |

---

## Gate Record

- **Test review** ([013-test-review.md](../reviews/013-test-review.md)): APPROVED, no callouts. Its tech-debt note is punched (the new tests dispose every scope). The probe logs it could not find are `reviews/013-probe-backref.log` and `013-probe-shared.log`.
- **Code review** ([013-code-review.md](../reviews/013-code-review.md)): APPROVED. All six callouts are punched: the authorization crossing rule, own-properties-first ordering (generator comments corrected, behaviour unchanged), the plain-class and enum statements, the back-reference outcome over HTTP, the skill's store note, and two LazyLoad links. Of its theoretical items, the enum line and a stale reference-app comment are fixed.
- **After the punches:** both solutions are green (unit 839; integration 646 + 7 skips; Design 155; both TFMs). mdsnippets is clean.
- **Open:** #133 (`critical`), which needs a generator and wire-format change.

---

## Plan Amendments

- **2026-10-15 · Step 1 stopped on a library bug** (Keith).
  - **The probe.** A `[Factory]` team fetched through the client containers, whose lead is also its first member and whose member points back at the team.
  - **Named:** both references survive.
  - **Ordinal (the default):** the shared reference arrives as two copies, and the back reference throws `JsonException` ("a possible object cycle was detected").
  - **Keith's ruling.** Document it and file a bug issue, which is #133, labelled `critical` at Keith's request. The probe becomes a known-bug regression test: the Ordinal cases are written naturally and skipped with the issue number.
  - **The page.** It gives a per-format table and tells readers to keep `[Factory]` aggregates free of back references and shared instances, or to use Named, until #133 is fixed.

---

## Notes

- The test targets `ReferenceTeam` and `ReferenceMember` live in the integration project, not in Design. A Design example of a graph that fails in the default format would contradict R-A's "shapes to copy". The page's reference section is therefore prose and a table, with no embed.
