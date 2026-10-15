# EXSET-013 code review — 2026-10-15

**Reviewer:** code-reviewer (per-plan, Step 5; `4bf6733` against `dea042c`). **Verdict:** APPROVED. No veto-tier findings.

**Build.** 0 errors in all three builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`013-build.log`).
- Design solution: 0 warnings (`013-design-build.log`).
- Reference app: 0 warnings (`013-refapp-build.log`).

**Tests.** 0 failed on both TFMs: unit 839, integration 646 + 7 skipped, Design 155.
- The 7 skips are the 5 known ones (three `ShowcasePerformanceTests`, two `RelayTimingTests`) and the two `FactoryReferenceIdentityTests.Ordinal_*` cases (#133), each named in `013-test.log` on both TFMs.
- Integration rose 644 + 5 → 646 + 7, and Design rose 151 → 155, so both runs exercised this change.
- `013-mdsnippets.log` shows no missing snippet.

The deliverable landed as scoped:

- **Design.**
  - `LogicalModeTests.Logical_RunsRemoteOperations_InTheSameContainer` registers `NeatooFactory.Logical`, then runs Create, Save and Fetch (`[Remote]`) against the container's singleton store. Logical mode registers no `IMakeRemoteDelegateRequest` (`AddRemoteFactoryServices.cs:153`), so the generated factory takes its local constructor. That matches the region's comment and the page.
  - `Order.cs:93-99` wraps the five declared properties in `serialization-ordinal-versioning`, which keeps its migrated name. The comment above it agrees with the generator, apart from callout 2.
  - The two `SerializationTests` pins match the code:
    - `CollectOrdinalProperties`' `StringComparer.Ordinal` sort.
    - The setter filter (`FactoryGenerator.Types.cs:436-444`, `IsSetterAccessibleForObjectInitializer`), which drops `Total`.
    - `NeatooInterfaceJsonTypeConverter.Write` writes `$type` as `FullName`, then `$value` (`:83-94`).
- **Library claims on the pages, checked against source.** All of these hold:
  - **The Named fallback** (`FactoryModelBuilder.cs:308-313`), together with `RequiresServiceInstantiationCheck` (`FactoryGenerator.Types.cs:526-560`). Its record exemption is at `:294-298`.
  - **Ordinal construction** with `new T { ... }`, or the primary constructor for a record (`OrdinalRenderer.cs:349-362`, `:390-402`).
  - **The named path's DI resolution.** `NeatooJsonTypeInfoResolver.GetTypeInfo` resolves the instance only when `IsService(type)` is true. `requiresEntityRegistration` includes `RequiresServiceInstantiation` (`FactoryModelBuilder.cs:300-302`), so a constructor-injected type is registered.
  - **Interface applicability.** The converter applies to an interface or abstract type that is not generic and is in a registered assembly (`NeatooInterfaceJsonConverterFactory.CanConvert`; `ServiceAssemblies.HasType`).
  - **Records.** `RecordBypassConverterFactory` is added in both formats.
  - **The ordinal converter.** It writes nested values through `JsonSerializer.Serialize(writer, …, options.GetTypeInfo(…))` and writes no metadata of its own.
  - **The client ignores `X-Neatoo-Format`.** Only `WebApplicationExtensions` writes it.
  - **`RemoteFactory.Trace`** is created by name in `NeatooJsonSerializer` (`:67`) and `HandleRemoteDelegateRequest` (`:60`). Its messages are at `LogLevel.Trace`.
  - **Both registrations take `NeatooSerializationOptions`**: `AddNeatooRemoteFactory(mode, options, …)` and `AddNeatooAspNetCore(options, …)`.
  - **factory-modes.md's three log lines** match `Log.cs:81` (3001, Information), `Log.cs:281` (7001, Information) and `ClassFactoryRenderer.cs:637`, which is emitted inside the local method body only.
  - **Their categories:** `ILogger<MakeRemoteDelegateRequest>` in `Neatoo.RemoteFactory.Internal`; `NeatooLoggerCategories.Server` (`AddRemoteFactoryServices.cs:149`, `ServiceCollectionExtensions.cs:36`); and `ILogger<{Type}Factory>` (`ClassFactoryRenderer.cs:431`).
  - **The "crossed / ran there" reading at `:98`** follows from that.
- **Reference identity.**
  - `013-probe-shared.log` and `013-probe-backref.log` show the probe failing in Ordinal on both TFMs: "shared reference not preserved", and `JsonException: A possible object cycle was detected` thrown from the ordinal converters' `Write`. Its Named case passes.
  - The committed regression test is written naturally. Its Ordinal bodies match the Named ones line for line, and they are skipped with the issue number, as Keith ruled.
- **Regions.**
  - `modes-logical-config` and `serialization-ordinal-versioning` are each defined once. Every embed's line range on the touched pages matches its source at `4bf6733`: `Order.cs#L93-L99`, `#L224-L298` and `#L37-L51`; `LogicalModeTests.cs#L28-L35`; `ServerServices.cs#L72-L74` and `#L96-L101`; `Program.cs#L64-L72`.
  - No retired name, and none of the 13 unembedded regions the deleted files defined, is defined or embedded outside `docs/todos` and `docs/plans/completed`. None of the 13 was embedded at `dea042c`.
  - `serialization-caveat-*` is left to EXSET-010.
  - Every `cs` fence on both pages and in setup's factory-modes block is an embed.
  - No source file names a deleted file, except one stale comment (Theoretical).
- **Findings text.** None of the plan's listed finding text remains on the two pages or in setup's block: "alphabetical", "Remote mode only", "resolves from DI", LazyLoad, "all operations", the invented log lines, `NeatooFactory.Local` and `new TeamMember`.
- **Ledger.**
  - C-07, C-19, C-20, I-01 and T-22 match the diff.
  - C-04's outcome ("Remote sends [Remote] operations and runs the rest locally") is accurate to D18, but incomplete against the generator; see callout 1.

**Sacred tests.** No existing test lost an assertion. `SerializationTests.cs` changes comments and appends two tests. The one deleted test, `LogicalModeTestingSample.TestWithLogicalMode`, is the reference-app sample this plan retires, and `LogicalModeTests` re-pins it more strongly. The test review's punch (disposing client and local scopes) is not re-raised.

| # | Callout | Affects | Triage |
|---|---|---|---|
| 1 | **Remote mode's crossing rule leaves out authorization.**<br>• The page and setup.md say Remote mode sends each `[Remote]` operation and runs every other operation on the client: the table at `factory-modes.md:12`, `:47` ("An operation without it, such as a local `Create` or a `Can*` check, runs on the client") and `setup.md:80`.<br>• The generator also makes an operation remote when it has `[AspAuthorize]`, or when one of its auth methods is `[Remote]`. Reads: `FactoryModelBuilder.cs:350-352`. `Can*`: `:577` and `:798-799`.<br>• Design's own `SecureOrder` is a case: its generated factory assigns `CanFetchProperty = RemoteCanFetch` and `CanSaveProperty = RemoteCanSave` in the remote constructor (`SecureOrderFactory.g.cs:71`, `:75`).<br>• `authorization.md:188` and `:323-331` already state this.<br>• Confidence High. Reachable by: a reader with `[AspAuthorize]` on a non-`[Remote]` operation, or a `[Remote]` auth method, who expects the call, or its `Can*`, to run on the client. It goes to the server.<br>• One option: "a `[Remote]` operation, or one whose authorization runs on the server (`[AspAuthorize]`, a `[Remote]` auth method)", with a link to authorization.md's Can* table. | AC-4 · Must (C-04) | **Punch** (fixed): factory-modes and setup say server-side authorization (`[AspAuthorize]`, a `[Remote]` auth method) also sends an operation to the server |
| 2 | **"Base-class properties first" is the reverse of what the generator does.**<br>• `serialization.md:26`, `SerializationTests.cs:72-75` and the `IOrdinalSerializable` doc comments all say base-class properties come first.<br>• `CollectPropertiesRecursive(symbol, properties, 0)` gives the type's own properties depth 0 and its base's depth 1 (`FactoryGenerator.Types.cs:413`, `:432`). `CollectOrdinalProperties` then sorts `OrderBy(p => p.InheritanceDepth)` ascending (`:417`). So the type's own properties come first and the base's after, the reverse of the comment at `:408`.<br>• Nothing pins it. Order has no base class (the test review's Theoretical item), and none of the 185 checked-in `*.Ordinal.g.cs` files has a group that sorts out of order.<br>• Confidence High on the code reading; not executed. Reachable by: a reader of the page with a `[Factory]` type that derives from a class with public settable properties, reading a payload or a network trace position by position.<br>• Options: state the order as the generator has it, or treat it as a library question, since the library's own XML docs promise base-first. | AC-4 · Must | **Punch** (fixed): the page and the Design guide say the type's own properties come first, then the base class's. The generator's comments are corrected; behaviour is unchanged, since changing it would change the wire format |
| 3 | **The two plain-class statements disagree with each other and with the library.**<br>• `serialization.md:9` lists "what crosses at all" without plain classes. `:79` then says plain classes keep references, and the library supports plain DTOs (`DtoConstructorRegistry`; `SharedReferenceTargets.cs` round-trips plain classes).<br>• `:79` says plain classes keep shared and circular references "in either format". A class with no public parameterless constructor and a public parameterized one is claimed by `RecordBypassConverterFactory.CanConvert` and written with `ReferenceHandler = null`, the same as a record (its doc: "records, classes without a default constructor"). The page before this change stated that ("Records, immutable classes"); the new one drops it. Confidence High.<br>• The qualifier "serialized on their own, not as part of a `[Factory]` object" is narrower than the code suggests. The ordinal converter serializes nested values with the same options, and `NeatooPreserveReferenceHandler.CreateResolver` returns the one `NeatooReferenceResolver.Current`. Confidence Medium; not executed.<br>• Reachable by: a reader who shares an immutable DTO class (a constructor-only class) in two places, or in a cycle. They get two copies, or the cycle exception. | AC-4 · Must | **Punch** (fixed): plain classes and enums are on the crosses list; the reference sentence says a public parameterless constructor is needed, and that a class without one is handled like a record |
| 4 | **Where the back-reference failure surfaces depends on the direction of the call.**<br>• The table gives the Ordinal back-reference outcome as "**Fails:** `JsonException`" (`serialization.md:76`).<br>• On a `[Remote]` Fetch, the server throws while serializing the response (`HandleRemoteDelegateRequest.cs:231`). The catch-all logs it and rethrows (`:253-258`), and over HTTP that is a 500. The client then gets `HttpRequestException`, as 006 established, not `JsonException`. The `JsonException` reaches the caller directly only when the client serializes (a Save), or in the in-process harness the probe used.<br>• Confidence Medium: reasoned from the handler and 006's failure table, not executed over HTTP. Reachable by: a reader whose aggregate has a back reference, who fetches it from a Blazor client and catches `JsonException`.<br>• One option: "fails to serialize (`JsonException` on the side that writes it; a 500 when that is the server)". | AC-4 · Must | **Punch** (fixed): the cell says the serializer throws, and that a fetch fails on the server as a 500 over HTTP |
| 5 | **The skill's Logical block names Design types that the skill never defines.**<br>• `setup.md:82-93` embeds `modes-logical-config`, which registers `IOrderDbContext` and `InMemoryOrderDbContext`.<br>• Those names appear nowhere else under `skills/RemoteFactory/`, and the skill's own examples use Employee. CLAUDE.md requires the skill to be self-contained. This is the same class of finding as 006 callout 1 (`AddDesignServer`).<br>• The region's comment does say what the line stands for ("the services a server would supply"), which softens it.<br>• Confidence Medium. Reachable by: an agent copying the skill's Logical registration into an app that has no `IOrderDbContext`. | AC-3 · Must | **Punch** (fixed): setup.md says `IOrderDbContext` is the example's in-memory store |
| 6 | **Two pages link to the removed LazyLoad section.**<br>• `interfaces-reference.md:602` and `lazyload.md:195` link `serialization.md#lazyloadt-properties`, which this rewrite removed (AC-5).<br>• Both pages belong to later plans: 008 removes interfaces-reference's LazyLoad mentions, and 011 deletes lazyload.md. Until then the links are dead.<br>• Confidence High. Reachable by: a reader following either link on the branch. | AC-5 · Should | **Punch** (fixed): interfaces-reference links lazyload.md#serialization, and lazyload.md drops its self-reference |

## Could not verify

- The working tree has an uncommitted edit to `SerializationTests.cs`, made at 11:24:45: the test review's punch, which disposes the client and local scopes. It postdates every build and test log (11:19–11:20). The file has no region, so no embed moves. It still needs a Design build and test before it is committed.
- The committed `FactoryReferenceIdentityTests.Ordinal_*` cases have never run, because they are skipped. The probe logs come from `ProbeFactoryReferenceTests` with `ProbeTeam`/`ProbeMember`, which are not in the commit. The committed targets have the same shape.
- Callout 2's ordering and callout 4's HTTP outcome are reasoned from source, not executed.

## Theoretical (not triaged)

- `serialization.md:61` ("System.Text.Json cannot deserialize a property typed as an interface") and the guide's "NO: generic interface-typed collections" (`SerializationTests.cs:43-46`) overstate. STJ deserializes `IList<T>`, `IEnumerable<T>` and `IReadOnlyList<T>` into `List<T>` natively, and nothing in RemoteFactory claims them: the interface factory skips generics, and `RecordBypass` finds no constructors. The advice to use a concrete collection is harmless.
- `SerializationTests.cs:25`, "Enums (serialize as string by default)", is pre-existing and outside the plan's named edits. No `JsonStringEnumConverter` is registered, so enums are written as numbers.
- `TestingSamples.cs:380` (reference app) still says "Mode examples moved to FactoryModesSamples.cs", a file this change deletes. EXSET-012 deletes the reference app.
- `FactoryReferenceIdentityTests.FetchTeam` never disposes its scopes. The integration project's other tests do the same.

## Read report

- **Read beyond the brief:**
  - `FactoryGenerator.Types.cs:380-560` and `:700-712`
  - `FactoryModelBuilder.cs:290-335`, `:345-372`, `:560-605`, `:785-800`
  - `OrdinalRenderer.cs` (the write and construction paths)
  - `ClassFactoryRenderer.cs:255-280` and `:420-436`
  - `NeatooJsonSerializer.cs:40-130`
  - `RecordBypassConverterFactory.cs:1-80`
  - `NeatooPreserveReferenceHandler.cs`
  - `NeatooJsonTypeInfoResolver.cs`
  - `NeatooInterfaceJsonConverterFactory.cs`
  - `ServiceAssemblies.HasType`
  - `HandleRemoteDelegateRequest.cs:40-60` and `:150-262`
  - `NeatooLoggerCategories.cs`, plus `Log.cs` by grep
  - `IOrdinalSerializable.cs`
  - Design's generated `SecureOrderFactory.g.cs` by grep
  - `authorization.md:188` and `:323-331`
  - `SharedReferenceTargets.cs` and `SharedReferenceTests.cs` by grep
  - the 185 `*.Ordinal.g.cs` `PropertyNames` arrays, scripted
  - `013-test-review.md`, plus `013-probe-*.log`, which appeared during the review
  - the working-tree diff
- **Named but unused:**
  - VisionAlignment 0.1/0.2, read by grep only (D18)
  - plans 006 (its review was used instead)
  - `.claude/rules/design-snippets.md`, as given in the brief
