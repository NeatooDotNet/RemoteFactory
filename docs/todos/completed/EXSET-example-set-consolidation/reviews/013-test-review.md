# EXSET-013 test review — 2026-10-15

## Round 1 — APPROVED (no veto-tier, no callouts)

**Reviewer:** test-reviewer, against `4bf6733`, diffed from `dea042c`. Logs green:
- main build (`013-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`013-design-build.log`): 0 errors, 0 warnings.
- reference app (`013-refapp-build.log`): 0 errors, 0 warnings.
- tests (`013-test.log`, `013-design-test.log`): unit 839, integration 646 + 7 skips, Design 155, each on both TFMs. No failures.

The counts went up by what was added:
- Integration went 644 + 5 → 646 + 7. The two Named reference tests pass. The two new skips are the Ordinal #133 cases, listed by name in the log on both TFMs. The other five skips are the known three `ShowcasePerformanceTests` and two `RelayTimingTests`.
- Design went 151 → 155: `Logical_RunsRemoteOperations_InTheSameContainer` (1), `Order_OrdinalShape_FollowsPropertyNames` (1), and `Order_LinesInterface_CrossesAsTypeAndValue` (2 theory cases).
- Unit is unchanged.

Every cited test exists, asserts what its bullet says and runs at the declared tier:
- **1. Logical mode.**
  - The test registers `NeatooFactory.Logical` with the store as a singleton. It runs `Create` (local), then `Save` and `Fetch`, which are both `[Remote]` on Order (`Order.cs:242`, `:259`).
  - It asserts the fetched customer and the single line's quantity.
  - It asserts the row is in the container's own `IOrderDbContext` singleton. New ids start at 1000 and the seeds are below that (`OrderPersistence.cs:97`), so a seeded row cannot satisfy the lookup.
- **2. Ordinal and interface shape.**
  - `Order_OrdinalShape_…` asserts that `Order.PropertyNames` equals the six names in ordinal order. That order is not the declaration order, and `Total` is absent. The test also asserts that the serialized root is an array of that length, and that positions 0 (customer) and 3 (`IsNew`) carry the expected values.
  - `Order_LinesInterface_…` runs once per format. It finds `Lines` by its `PropertyNames` index in Ordinal and by name in Named, then asserts `$type` equals `OrderLineList`'s full name and that `$value` is present.
  - Both tests use the real DI-registered `INeatooJsonSerializer` from `DesignClientServerContainers`. That is the same harness tier 006's container tests were accepted at.
- **3. Reference identity.**
  - `FetchTeam` calls the `[Remote]` `ReferenceTeam.Fetch` from the client scope of `ClientServerContainers.Scopes(format)`. That overload returns `(server, client, local)`, and the test reads the element by name, so this is the serialized round trip.
  - The Named cases genuinely assert identity. The shared case asserts `Assert.Same(single member, Lead)`. The back-reference case asserts that as well, plus `Assert.Same(team, member.Team)`. A copy would fail these assertions.
  - The Ordinal cases are natural: their bodies match the Named ones line for line, with only the format changed. They are skipped with `Skip = "Known bug #133: …"`, which follows the repo's existing `[Fact(Skip = "<reason>")]` convention (`RelayTimingTests.cs:56`, `:105`). Each reason names the observed failure.

**Sacred-test check: clean.**
- `SerializationTests.cs` changes its header comments, adds two `using`s, and appends two tests. No existing test body changed.
- `EmployeeManagement.Tests/Samples/FactoryModes/LogicalModeTestingSample.cs` is deleted. The plan's Scope deletes the reference app's `FactoryModes/` samples with their unembedded regions, and `modes-logical-testing` was embedded nowhere at `dea042c`. Its behaviour (a Logical create, save and fetch) is re-pinned, more strongly, by `LogicalModeTests`, which also checks the store row.
- `TestingSamples.cs` changes one comment.
- None of the other 11 deleted reference-app files held a test.

Explicit-skip spot checks (bullets 4 and 5):
- Every `cs` fence on `factory-modes.md` and `serialization.md` is inside an embed. The others are a solution-tree diagram and one `json` block.
- The embed sources are all in `src/Design`: `readme-server-setup`, `getting-started-client-program`, `modes-logical-config`, `aspnetcore-logging` and `serialization-ordinal-versioning`.
- None of the 9 retired names is defined or embedded outside `docs/todos`. The only other hit is `modes-remote-config`, in the historical `docs/plans/completed/hosted-blazor-wasm-conversion.md`.
- The reference app has no `modes-*` or `serialization-*` region left except `serialization-caveat-*`, which belongs to EXSET-010.
- setup.md's factory-modes block is now the `modes-logical-config` embed, and `NeatooFactory.Local` is gone.
- `013-mdsnippets.log` has no missing-snippet, error or exception line.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Tech-debt | `Order_OrdinalShape_…` and `Order_LinesInterface_…` discard the client and local scopes from `Scopes(...)` and never dispose them. They dispose only the server scope, where the file's other tests dispose server and client. | — | **Punch** (fixed): both tests dispose server, client and local. The could-not-verify item is answered by `reviews/013-probe-backref.log` and `013-probe-shared.log`, the probe runs where Ordinal failed |

Could not verify:
- That the two Ordinal cases fail today. Skipped tests do not run, and `reviews/` has no log of the probe run. I relied on the Plan Amendment and Discovery Log record of the probe, together with #133.

Theoretical (not triaged):
- Order's six property names sort the same under `StringComparer.Ordinal` as under a case-insensitive alphabetical sort. The pin fixes Order's actual order, but it cannot tell "ordinal" from "alphabetical" in general. The bullet is scoped to Order's array, so this is not a gap against it.
- Order has no base class, so the "base class first" part of the ordering, which the page states, is not exercised. The bullet does not name it.
