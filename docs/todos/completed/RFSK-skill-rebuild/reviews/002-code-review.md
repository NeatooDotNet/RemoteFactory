## Code Review — RFSK-002 — 2026-10-16

**Verdict: APPROVED**
**Logs:** build PASSED — `reviews/002-build.log` (0 errors; 2 warnings, both the WASM `NativeFileReference` notice in `OrderEntry.BlazorClient`, not this change), `reviews/002-design-build.log` (0 warnings, 0 errors); tests 0 failed — `reviews/002-test.log` (unit 871/871, integration 664 passed + 5 skipped, both TFMs), `reviews/002-design-test.log` (Design 177 passed + 1 skipped, both TFMs)

### Direction & shape
The page has the Neatoo skill's shape: an opening built on the one-assembly premise (not persistence routing), the five model statements, the eleven-row habit table, the pointer paragraph, the decisions, a mechanism quick reference, the known pains, and the reference index (`skills/RemoteFactory/SKILL.md:8-403`). The model statements are VisionAlignment section 2's sentences with these changes only: the parentheticals (D15), (D3, D14), (D3), (D11), (D18) and (C-09) are removed, statement 4's Q1 sentence is replaced by R-C's ruling, and statement 6 is renumbered 5. The Neatoo skill numbers its own model 1-4 and never cites RemoteFactory's statements by number, so the renumbering breaks nothing. The eleven questions in the table match `Neatoo/skills/neatoo/SKILL.md:42` word for word. All five in-page anchors resolve: `#create-or-fetch`, `#one-class-a-child-and-a-root`, `#which-factory-shape`, `#execute-obeys-remote` and `#read-model`. All 8 C# fences are embeds, and each region exists in `src/Design`. The reference index lists exactly the 10 files under `references/`. None of the four audited teachings remains: persistence routing as the headline, N+1 as the reason (lines 28 and 45 deny it), an interface factory as a repository (lines 40 and 59 say "never a repository"), and `[Remote, Create]` as the lead example. Generator spot checks that hold:
- NF0102 "Task or Task<T>" (`DiagnosticDescriptors.cs:26-35`)
- NF0105 with its static exemption (`FactoryModelBuilder.cs:195-207`)
- NF0106 (`DiagnosticDescriptors.cs:62-72`)
- `Inherited = true` (`FactoryAttributes.cs:10`)
- the non-`partial` class losing its ordinal converter (`FactoryModelBuilder.cs:308-309`)
- the `internal` member and guard on the factory interface (`ClassFactoryWithExecute.cs` remarks; the generated `OrderFactory.LocalSave` guard)
- the dispatch phases (`DispatchPhase.cs`)

The only Design edit is the removal of "(R-I)" from a comment (`OrderCheckout.cs:84`).

### Veto-tier
None.

### Callouts
1. `SKILL.md:50`, High. `Affects: AC-6`. `Reachable by:` the brief's word-for-word check of the pointer paragraph against the Neatoo table rows, and RFSK-006's scoring of the two skills as one list. Two of the ten Neatoo questions differ from the Neatoo rows:
   - "How do I get computed values after Fetch?" — Neatoo row 6 (`Neatoo/skills/neatoo/SKILL.md:36`) reads "How do I get computed values after Fetch - `RunRules(All)` at the end?"
   - "The parent saves its children's rows - the `DeletedList` half?" — Neatoo row 7 (`:37`) reads "The parent saves its children's rows?"

   The other eight match.
2. `SKILL.md:43`, Medium-High. `Affects: AC-2`. `Reachable by:` a server-side caller — the command shape statement 5 prescribes (fetch, call a verb, save through the root factory) — or a Logical-mode test. The row says "The instance you passed is not updated and still reports its old state." That is true only when the call crosses. Locally, `LocalSaveCore` routes the same `target` through `LocalInsert`/`LocalUpdate` (`Design.Domain/Generated/.../Design.Domain.Aggregates.OrderFactory.g.cs:525-543`), so the passed instance is the one that was written. VisionAlignment T13 scoped the claim with "In Remote mode". "Reassign it" stays correct everywhere; only the second sentence overreaches.
3. `SKILL.md:50`, Medium. `Affects: AC-6`. `Reachable by:` a reader looking for the Neatoo model the RemoteFactory skill assumes. AC-6 asks the page to point at the Neatoo skill's *statements*, its rows and its decision. The page points at the rows, at the decision, and at "its placement statement", described as "where business logic goes inside an entity". Neatoo's statement 3 (`Neatoo/skills/neatoo/SKILL.md:19`) places logic by its driver: rule, verb, seam or read model. That is wider than "inside an entity". The other statements are not pointed at: 1 (operation sets the baseline) and 4 (the UI binds). The plan's own Acceptance bullet narrows AC-6 to "questions and decision", so this is a gap against the todo AC, not the plan bullet.
4. `SKILL.md:380`, High. `Affects: AC-12`. `Reachable by:` an `[Update]` returning `false` on an authorized class factory, which is reported as an authorization refusal. #104 names two conditions: a `bool` write returning `false`, and new-and-deleted. The line names only the second, and omits the one a developer is more likely to hit. It also credits #104 with the `NotImplementedException` for a missing routed operation, which #104 does not describe; that comes from VisionAlignment F5 (audit T-12).
5. `SKILL.md:371-384`, Medium. `Affects: AC-12`. `Reachable by:` a library author who wants the generated factory to be non-public. VisionAlignment F6 ("No way to declare a generated factory `internal`") is not among the known pains, and no issue was found for it (an open-issue search for "internal factory" returns none). Caveat: the `ClassFactoryWithExecute.cs` remarks say the interface becomes `internal` when every member is, so F6 may already be partly resolved, and its current state should be confirmed before it is named.
6. `SKILL.md:376`, Medium. `Affects: AC-12`. `Reachable by:` a property with an `internal set`. The line says "a property with a non-public setter is silently left out of serialization". The property collector keeps `internal` and `protected internal` setters (`FactoryGenerator.Types.cs:450-455`, `IsSetterAccessibleForObjectInitializer` at `:516-527`); only `private` and `protected` setters are dropped from the ordinal shape. The advice ("public setters", line 358) is right, and the defect description overclaims. One option is the wording VisionAlignment F3 uses ("private-setter properties").

### Could not verify
- Line 356, "a refusal throws `NotAuthorizedException` on the client" for an interface factory: `InterfaceFactoryAuthorizationTests` asserts `ThrowsAsync<NotAuthorizedException>`. I did not confirm that those factories resolve from the client container.
- "`mdsnippets` runs clean": no mdsnippets log was passed. `git diff --stat RFSK...HEAD` touches 3 files (the plan, `SKILL.md` and `OrderCheckout.cs`), which is consistent with a clean run.
- The reference-page targets the rows link to (`Order`'s Fetch, `OrderCommands`, `IProductCatalog`, `TaxRate`): the types exist in `Design.Domain`; their embedding is 003/004's.

### Theoretical (not triaged)
- Line 12, "Every C# block in this skill and its references is compiled and tested", is not yet true of the references (hand-written blocks remain until 003/004). The arc reaches main whole.
- Line 375, "A `[Factory]` class without `partial` still gets a factory": true of a class factory. A static `[Factory]` class without `partial` gets NF0101 (`FactoryModelBuilder.cs:67-68`). Line 359 covers the rule either way.

### Read report
- Read beyond the brief: `FactoryModelBuilder.cs` (NF0102/NF0105 and the ordinal-converter condition), `FactoryGenerator.Types.cs:440-527`, `DiagnosticDescriptors.cs`, `FactoryAttributes.cs:10`, `DispatchPhase.cs`, `InterfaceFactoryRenderer.cs:470-500`, the generated `OrderFactory.g.cs` LocalSave, `ClassFactoryWithExecute.cs:160-200`, `ClassFactoryExecuteTests.cs:99-114`, `Design.Client.Blazor/Program.cs` (wiring), the bodies of #104 and #135, the titles and states of the seven open issues, and Neatoo `SKILL.md:1-48`.
- Named but unused: the states of #113, #117 and #122 (the page names none of them, so there was nothing to check against them); VisionAlignment rulings D19, R-J, R-M, R-N and R-O (no sentence on the page touches them).
