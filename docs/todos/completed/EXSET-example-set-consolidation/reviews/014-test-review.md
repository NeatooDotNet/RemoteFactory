# EXSET-014 test review — 2026-10-15

## Round 1 — CONCERNS (2 veto-tier, both on bullet 2; 0 callouts)

**Reviewer:** test-reviewer, against `a564b9d`, diffed from `973a90f`. Logs green:
- main build (`014-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`014-design-build.log`): 0 errors, 0 warnings. `Design.Domain` was rebuilt on both TFMs, so the tests ran against the new region markers.
- reference app (`014-refapp-build.log`): 0 errors, 0 warnings.
- tests (`014-test.log`, `014-design-test.log`): unit 852, integration 657 + 7 skips, Design 173 + 1 skip, each on both TFMs. No failures. The one Design skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104). The counts are the same as 009's, which is what a change made only of markers should give.
- mdsnippets (`014-mdsnippets.log`): no error, exception or missing-snippet line.

The logs are timestamped 14:55–14:56, and the commit is 14:57.

Notes per bullet:
- **1. Page and skill (Must): honest.**
  - `attributes-reference.md` has 15 `snippet:`/`endSnippet` pairs and 15 fences, and no fence sits outside a pair.
  - Each of the 15 regions is defined once, in `src/Design`.
  - `attributes-suppressfactory`, `attributes-factoryhintnamelength`, `attributes-inheritance` and the other retired `attributes-*` names are neither defined nor embedded outside `docs/todos`.
  - The skill embeds only `attributes-execute-local`, which is now Design's.
  - Both reference-app sources are deleted, and `AssemblyAttributes.cs` has no markers.
  - factory-operations, interfaces-reference and save-operation change only in `snippet source` lines.
- **2. Inheritance and `[SuppressFactory]` (Must): not honest as it stands.** See findings 1 and 2.
  - The class half holds. `FactoryGenerator.cs:19-23` uses `ForAttributeWithMetadataName` on class and record declarations, and line 23 drops a declaration whose own attribute list names `SuppressFactory`.
  - That matches the probe's five rows and #141, and the page's `[Factory]` statements (`:58`, `:434`).
  - Two of the page's other statements are contradicted by generator code that the cited evidence does not cover.
- **3. Embedded regions pinned (Must): honest.** Each new region's type is exercised through the client container by the test named in the map:
  - `attributes-factory`: `OperationsTests` resolves `ICustomerPreferencesFactory` on the client and asserts Create, Save and Fetch outcomes.
  - `attributes-factory-interface`: `InterfaceFactoryAuthorizationTests` calls `ICustomerDirectoryFactory` from the client, with allow and deny outcomes.
  - `attributes-execute-local`: `LocalExecuteTests.Execute_StaticFactory_WithoutRemote_RunsOnClient` runs behind a stand-in that throws if the call crosses the wire. `Execute_WithRemote_StillTriesTheWire` is the control, so the "no remote request" claim is not vacuous.
  - `attributes-factoryeventhandler` and `attributes-factoryevent`: `FactoryEventHandlerTests` raises `OrderPlacedEvent` and asserts the `Order 42 confirmed!` message that `OrderNotifyHandlers` sends.
  - The reused regions keep their definitions and their tests from 007–009.
- **4. Meta (Must): honest.** The logs and counts are as stated above.

**Sacred-test check: clean.**
- No test file changed between `973a90f` and `a564b9d`.
- The only `src/` changes are five `#region`/`#endregion` pairs in `Design.Domain`, the two deleted reference-app samples (neither one a test) and the removed markers in `AssemblyAttributes.cs`.

**Plan-introduced reachable paths:** none. No production code changed.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Veto | **Bullet 2 (`· Must`): the page says `[SuppressFactory]` turns generation off on an interface, and the generator does not.**<br>The page says so in two places:<br>- Quick Lookup (`attributes-reference.md:10`): Target "Class, Interface", "Turn generation off for a type marked `[Factory]`";<br>- `:62`: "On a type that also carries `[Factory]`, turns generation off for it".<br>The skill's `class-factory.md` `[SuppressFactory]` section repeats it.<br>The only `SuppressFactory` check in `src/Generator` is in the class/record pipeline (`FactoryGenerator.cs:23`). The interface pipeline (`FactoryGenerator.cs:54-56`) checks only for generic parameters. So `[Factory][SuppressFactory] interface I…` still gets a factory, which `SuppressFactoryAttribute`'s `AttributeTargets.Interface` allows (`FactoryAttributes.cs:18`).<br>The cited evidence is all class-shaped: the probe's five rows, lines 19-23 and #141.<br>Suggested shape: restate the page and the skill to the generator (classes and records; no effect on an interface) and cite `:54-56`. Or, if the interface claim stays, pin it with a unit-tier generator test of a `[Factory][SuppressFactory]` interface, which would fail today. | AC-3, AC-4 | **Amend** (fixed): the page's table says Class, Record; the section says it has no effect on an interface; the skill's `class-factory.md` likewise. Recorded on #141 |
| 2 | Veto | **Bullet 2 (`· Must`): the page says `[AuthorizeFactory<T>]` is declared on each class it applies to. The generator takes it from a base class.**<br>The claim is in `attributes-reference.md:437`, under "The generator reads attributes from each declaration". The skill's `advanced-patterns.md` "Attributes and Inheritance" line says the same.<br>`TypeAuthMethods` finds the attribute with `ClassOrBaseClassHasAttribute(typeSymbol, "AuthorizeFactoryAttribute")` (`FactoryGenerator.Transform.cs:310`). That helper (`:19-31`) walks `BaseType` recursively. So a `[Factory]` class derived from a class that carries `[AuthorizeFactory<T>]` gets T's checks without declaring the attribute, even though `AuthorizeFactoryAttribute<T>` is `Inherited = false` (`FactoryAttributes.cs:145`).<br>The explicit-skip evidence (the probe, lines 19-23) does not touch authorization.<br>Suggested shape: restate the line to the generator (a derived class takes its base's `[AuthorizeFactory<T>]`) and cite `Transform.cs:310`. Or pin the stated rule with a unit-tier generator test of a derived `[Factory]` class under an authorized base, which would fail today. | AC-3, AC-4 | **Amend** (fixed): Attributes and Inheritance states the nearest base's `[AuthorizeFactory<T>]` and the base operation methods, constructors excepted; `advanced-patterns.md` likewise. Recorded on #141 |
| 3 | Tech-debt | `[SuppressFactory]` is matched by its syntax name `== "SuppressFactory"` (`FactoryGenerator.cs:23`), so `[SuppressFactoryAttribute]` or `[Neatoo.RemoteFactory.SuppressFactory]` suppresses nothing. | — | **Punch** (fixed): the page says to write it as `[SuppressFactory]`; recorded on #141 for the library |
| 4 | Tech-debt | No retained test pins `[Factory]`'s declaring-type-only rule or `[SuppressFactory]`. Bullet 2 rests on a probe whose source was deleted, and #141 says no test covers either attribute. | — | **Dismiss**: the rule is stated from source with lines cited, and a generator test for it is #141's when it is decided; Bullet 2 is `explicit-skip` by design |
| 5 | Tech-debt | The plan's Scope says `attributes-factoryeventhandler` is "on a phased handler". It wraps `OrderNotifyHandlers`, an `Immediate` handler. | — | **Amend** (recorded): Plan Amendment 2026-10-15 · Implementation |

Could not verify:
- The probe compile itself, because its source was deleted. Its five class rows agree with `FactoryGenerator.cs:19-23` and with #141.

Theoretical (not triaged):
- `GetMethodsRecursive` (`FactoryGenerator.Transform.cs:58-67`, used at `FactoryGenerator.Types.cs:179`) collects base-class methods except constructors. So a derived `[Factory]` class takes its base's attributed operations. The page's "each method and parameter declares its own" (`:436`) is literally true, but it does not tell the reader this.

## Round 2 — 2026-10-15 — APPROVED (0 veto-tier, 0 callouts)

**Reviewer:** test-reviewer, against `ad07e40`, diffed from `a564b9d`. Only the round-1 findings were checked.

**Logs: green.** They are timestamped 15:14, and the commit is 15:15.
- main build (`014-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`014-design-build.log`): 0 errors, 0 warnings.
- tests (`014-test.log`, `014-design-test.log`): unit 852, integration 657 + 7 skips, Design 173 + 1 skip, each on both TFMs. No failures. The Design skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104).
- mdsnippets (`014-mdsnippets.log`): no error, exception or missing-snippet line.

**Sacred-test check: clean.** Nothing under `src/` changed between `a564b9d` and `ad07e40`. The commit touches only the page, two skill references, the plan, the todo, the ledger and the reviews.

Round-1 findings:
- **1 (Veto): closed.**
  - The Quick Lookup row (`attributes-reference.md:10`) now says "Class, Record".
  - The `[SuppressFactory]` section (`:62`) says it has no effect on an interface, which keeps its factory.
  - The skill's `class-factory.md:407` says the same.
  - This matches the generator. The class/record predicate (`FactoryGenerator.cs:19-23`) is the only `SuppressFactory` check in `src/Generator`, and the interface pipeline (`:54-56`) does not check it.
  - Nothing on the page or in the skill still names an interface as a `[SuppressFactory]` target.
- **2 (Veto): closed.**
  - "Attributes and Inheritance" (`attributes-reference.md:432-440`) and `advanced-patterns.md:545-547` now state two things. A derived `[Factory]` class takes its bases' operation methods, constructors excepted. It also takes the nearest base's `[AuthorizeFactory<T>]` when it has none of its own.
  - This matches the generator:
    - `GetMethodsRecursive` (`FactoryGenerator.Transform.cs:58-67`) adds base methods with `includeConst = false`. It is called on the factory type at `FactoryGenerator.Types.cs:179`.
    - `ClassOrBaseClassHasAttribute` (`Transform.cs:19-31`) returns the type's own attribute first, then walks `BaseType`. It is called from `TypeAuthMethods` (`:310`), which `Types.cs:181` calls.
  - The old "declared on each class or method they apply to" line is gone. This also closes round 1's Theoretical item on `GetMethodsRecursive`.
- **3 (Punch): done.** `:62` says to write it as `[SuppressFactory]`, and that `[SuppressFactoryAttribute]` and namespace-qualified forms are not recognised. That agrees with the `== "SuppressFactory"` match at `FactoryGenerator.cs:23`.
- **4 (Dismiss): not re-raised.**
- **5 (Amend): recorded.** It is the second 2026-10-15 · Implementation entry in the plan's Plan Amendments.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|

None.

Could not verify:
- None.

Theoretical (not triaged):
- The inheritance list's `[SuppressFactory]` line (`attributes-reference.md:435`) does not repeat the interface exception that `:10` and `:62` now state. It does not contradict them.
