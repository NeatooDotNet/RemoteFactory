# Code Review — RFSK-003 — 2026-10-16

Written by the orchestrator from the code-reviewer's report; the reviewer's role does not write review files.

**Verdict: APPROVED**

**Logs:** `003-build.log` 0 errors, 2 warnings (the WASM `NativeFileReference` workload warning on `OrderEntry.BlazorClient`, environmental); `003-design-build.log` 0 errors, 0 warnings. Unit 871 and integration 664 (5 skipped) per TFM in `003-test.log`; Design 177 (1 skipped) per TFM in `003-design-test.log`. No failures.

## Direction and shape

The six pages sit under `SKILL.md`'s model, each built around a Design type (`Order`/`OrderLineList`/`OrderLine`, `IProductCatalog`/`ICustomerDirectory`, `OrderCommands`). Of 55 fences, 40 are embeds and every hand-written one opens with `// WRONG` (service-injection 1, anti-patterns 15), short and in Design's names. Gone: a repository as an interface factory, N+1, method injection titled "Server Only", the `[JsonInclude]`/private-setter recipe, an `internal` child `Create`; the `[Remote, Create]` that calls a service for nothing appears only as WRONG #2. No ruling IDs in the pages.

**Generator claims checked, all true:** factory-interface visibility (`ClassFactoryRenderer.cs:113`); declared accessibility drives the method rules (`Types.cs:722`); NF0101 only for static classes, a non-partial class factory just loses the ordinal serializer (`FactoryModelBuilder.cs:68`, `:309`); NF0102, NF0103 (`Transform.cs:173`), NF0105 with the static exemption (`FactoryModelBuilder.cs:209`), NF0106 regardless of auth (`Transform.cs:138`); abstract and generic classes ignored, `[SuppressFactory]` on class/record only (`FactoryGenerator.cs:19-23`, `:55`); parameter defaults never read (`Types.cs:885`); the constructor-shape table (`Types.cs:539-574`) and the named path through `GetRequiredService` (`NeatooJsonTypeInfoResolver.cs:30`); `RegisterMatchingName`'s convention (`AddRemoteFactoryServices.cs:263-273`); interface-factory registration by mode and the auth class's `TryAddTransient`; `Can{Method}` takes only matched parameters; `IFactoryOnCancelled` on `OperationCanceledException`; the static-command delegate signatures; Remote-mode routing for `[AspAuthorize]` and `[Remote]` auth; underscore stripping.

**Links:** every link `SKILL.md` makes into these pages, and every anchor the pages make, resolves and lands on the embed it names.

## Veto-tier

None.

## Callouts (both Affect AC-3)

1. `anti-patterns.md:133` heading and `:190` summary row say "non-public setter"; the ordinal collector keeps `internal` and `protected internal` setters (`FactoryGenerator.Types.cs:516-528`). Only `private` and `protected` are dropped. Reachable by a developer with an `internal set`. **Fixed:** heading "A private setter", row "A `private` or `protected` setter".
2. `class-factory.md:98` "Generates" omits the `internal` `FetchDraft` member that `OpenDraft` calls through `IOrderFactory`. Reachable by a reader of the `OpenDraft` embed. **Fixed:** the line names it.

## Could not verify

- `static-factory.md:129`, a `[Service]` before a value breaks generated code (#135): not traced. Resolved: the issue's body is the evidence; the page cites it.
- `interface-factory.md:141`, CRUD scopes silently never run on an interface factory: carried over, not traced. Resolved: unchanged from the published `docs/authorization.md` and `docs/interface-factory.md`.
- CS0101/CS0111 for `[Factory]` on the implementation: naming logic supports it, no test pins it. Resolved: matches `docs/interface-factory.md:148`.
- A class-level `[Execute]` returning another type fails in generated code: from VisionAlignment F8, not probed. Resolved: F8 records the probe (2026-10-10).

## Theoretical (all taken)

- "No existing region is renamed or changed" is not literally kept: two regions lost a ruling ID from a comment. **Taken:** Plan Amendment added.
- The named JSON path's `IncludeFields` fact (a public field crosses there) was dropped. **Taken:** restored in `service-injection.md`.
- "`AddScoped` is the explicit form": `RegisterMatchingName` uses `TryAddTransient`. **Taken:** the explicit form is `AddTransient`.
