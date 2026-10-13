# EXSET audit ledger

One row per finding in `Neatoo/docs/todos/consistency-audit/audit-remotefactory.md` (86 rows, RemoteFactory `2a7b438`). This is AC-4's evidence.

- **Owners**: the EXSET plans whose pages or sources carry the finding; `T2` = the skill rebuild (Todo 2), for skill prose and hand-written skill blocks. A finding with several owners is closed by the last of them.
- **Outcome**: filled by the owning plan. `fixed (EXSET-NNN)`, `retired (EXSET-NNN)` when its source is deleted rather than corrected, `T2` when only skill prose remains, or `fixed elsewhere (<ref>)`.
- Section 5 of the audit (installed skill copy) is C-17.

| ID | Gist | Owners | Outcome |
|---|---|---|---|
| C-01 | "`[Remote]` executes on server" comments | 005, 007, 010, 012 | |
| C-02 | Child `[Create]`/`[Fetch]` public; "server-side only" on a public method | 002, 007, 010, T2 | 002: Design fixed (R-C) |
| C-03 | Decision tree: "called from client → `[Remote]`" | 005 | |
| C-04 | "Remote mode: all operations POST to server" | 006 | |
| C-05 | Static `[Create]` as create-or-fetch | 007 | |
| C-06 | Events examples insert/commit inside `[Create]` | 004, 011, T2 | |
| C-07 | `[Factory]` child built with `new` | 006, 012 | |
| C-08 | Hand-coded GetOrCreate service; consumer-app references | T2 | |
| C-09 | Commands write another aggregate's row | 007, 012 | |
| C-10 | Parent deletes children's rows | 007 | |
| C-11 | Hooks documented for validation; throwing in hooks | 002, 007, 008, 010, 012 | 002: Design's Order hook and CLAUDE-DESIGN Lifecycle Hooks fixed |
| C-12 | "Manual checks in your write methods" | 008 | |
| C-13 | Throwing validation sample regions (unpublished) | 012 | |
| C-14 | Value-object constructors validate by throwing | 004, 010, 012 | |
| C-15 | `bool` `[Create]`, business filter in `[Fetch]`, HR rule in `[Update]` | 007, 010 | |
| C-16 | `[SuppressFactory]` public static `Task` command as "the shape to copy" | 009 | |
| C-17 | Installed skill copy predates v1.9.0 | T2 | |
| C-18 | Read models with `[Create]`; no read-model branch | 003, 010, 012 | |
| C-19 | Public concretes everywhere; not interface-first | 005, 006, 007, 008, 009, 010, 011, T2 | |
| C-20 | Properties and callers typed on concretes; casts | 006, 007, 008, 010 | |
| C-21 | Method-injected services stored in fields (hooks samples) | 008 | |
| C-22 | LazyLoad loader captures a method-injected service | 011 | |
| C-23 | Client-constructed classes take server services in ctor (unpublished) | 012 | |
| C-24 | `Terminate` as an `[Update]`; HR rule in `[Update]` | 009, 012 | |
| C-25 | "Business logic in the entity, not the factory" | T2 | |
| C-26 | Trimming: "business rules ship to the browser" | 011, T2 | |
| C-27 | Interface factories named repositories | 003, 009, T2 | |
| C-28 | "Persistence routing engine" framing; "Data Mapper Factory" | 005, 007, 008, 010, 012 | |
| C-29 | Class factory = "aggregate roots with lifecycle" only | 005, T2 | |
| C-30 | `[Execute]` = "decides which persistence path" | 007 | |
| I-01 | Private setters three ways; ordinal drops them | 005, 006, 008, 009, 011, T2 | |
| I-02 | `[Service]` "server-only" vs "that tier's container" | 007, 009, 010, 012, T2 | 002: CLAUDE-DESIGN Service Injection fixed |
| I-03 | `[Execute]` return type: skill vs NF0102 | 009, T2 | |
| I-04 | `[Remote] public` in hand-written blocks | 012, T2 | |
| I-05 | Parameter defaults: "optional last" vs dropped | 007, 010, T2 | |
| I-06 | Parameter ordering rule vs `params` sample | 007 | |
| I-07 | `Save` result not reassigned | 002, 005, 008 | 002: Design tests and Blazor page reassign |
| I-08 | "Events must be raised inside a factory method" vs client raise | 011, T2 | |
| I-09 | `Can*` method set, `Authorized<T>`, flag coverage | 009, T2 | |
| I-10 | "Feature switch is all that's needed" vs `TrimMode` | 011, T2 | |
| I-11 | "No separate repository layer" over a repository sample | 005 | |
| I-12 | Hand-written "generated interface" illustrations | 005, 006, 007, T2 | |
| I-13 | "Child entity: internal interface" comment | 005 | |
| I-14 | Duplicate `attributes-factoryhintnamelength` region | 009 | |
| I-15 | "Or before:" block identical to "after" | T2 | |
| T-01 | LazyLoad loader "re-created on the client" | 011 | |
| T-02 | Missing `partial` "CS0260" | 010, 011, T2 | |
| T-03 | "Call Insert/Update/Delete directly through the factory" | 005, 007, 008 | |
| T-04 | `TryFetch` | 007 | |
| T-05 | `NeatooFactory.Local` | T2 | |
| T-06 | `RegisterMatchingName<T>()` | 010, T2 | |
| T-07 | `[Save]` attribute | 011, T2 | |
| T-08 | Static delegates called without injection | T2 | |
| T-09 | `[Event]` remnants; fire-and-forget event hosting | 005, 006, 009, 011, T2 | |
| T-10 | Wrong request/response DTO shapes and log categories | 006 | |
| T-11 | 409 on concurrency; 401/403 for all remote calls | 008, 009 | |
| T-12 | Save table omits authorized new+deleted and missing-method outcomes | 008, T2 | |
| T-13 | `IFactoryCore<T>` | 008 | |
| T-14 | `src/docs/samples` duplicates every README snippet; .NET 8 | 005 | |
| T-15 | `CODING_STANDARDS.md` stale throughout | 012 | |
| T-16 | "not-registered DI exception"; "26 tests"; `reference-docs` path | 005, 012 | |
| T-17 | "Seven operations" | 005, 007 | |
| T-18 | "Right" event handlers are instance methods | 011 | |
| T-19 | `AddRemoteFactoryServices`/`FactoryMode` names; relay wording | 011, T2 | |
| T-20 | "Methods fall back to remote calls" | 005 | |
| T-21 | Partial properties on a plain class | T2 | |
| T-22 | "Guaranteed to compile" over `[SuppressFactory]` regions | 005, 006, 007, 008, 009, 010, 012, T2 | |
| T-23 | `IFactoryOnCancelled` "on operation failure" | T2 | |
| T-24 | "Causes duplicate generation" instead of NF0106 | 010 | |
| T-25 | Two-container testing sample uses one Logical container | 006 | |
| T-26 | CSLA `DataPortal_Fetch` in a diagram | 011 | |
| T-27 | Neatoo-side converter names in the appendix | 011 | |
| T-28 | `Can*` "returns `Authorized<T>`" | T2 | |
| T-29 | 39 unpublished regions, some on removed concepts | 012 | |
| U-01 | How is a child created on the client? (R-C) | 002, 007, 010, T2 | 002: Design compiled example and tests |
| U-02 | When does `[Create]` need the server? (D14) | 002, 005, 007, 010, 012, T2 | 002: Design's Order local; AuthorizedOrder, ParamAuthOrder, SecureOrder left for 009 |
| U-03 | The cascade on plain classes (R-D) | 002, 007, 008 | 002: Design compiled example and tests |
| U-04 | Missing row in `[Update]` (R-I) | 005, 008, T2 | |
| U-05 | Target not found in a command (R-I) | 007 | |
| U-06 | Read model vs interface-factory query (R-H) | 003, 005, 010, T2 | |
| U-07 | Side effects in a `[Create]` constructor (R-J) | 010 | |
| U-08 | Server executes a client-supplied query string | 010 | |
| U-09 | Entity duality (R-G) | 005, 012, T2 | |
| U-10 | Who maps a child's row on load (R-D) | 002, 007, T2 | 002: Design compiled example |
| U-11 | Bare `List<Child>` vs `[Factory]` lists (R-D) | 007 | |
| U-12 | `IsNew`/`IsDeleted` shape (R-M) | 008, 011 | |
