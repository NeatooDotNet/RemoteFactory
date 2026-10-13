# Vision Alignment: RemoteFactory

**Status:** Draft 2, 2026-10-09. Written by Claude (Fable 5.1); Keith ruled on every question the same day. Section 0 is doctrine. Sections 2 to 5 have been corrected to the rulings but have not been re-read by Keith line by line; where a line there conflicts with section 0, section 0 wins. This is a working document, not user-facing documentation.

**How to use it:** Section 0 holds the rulings: 0.1 carried over from Neatoo, 0.2 made here. Sections 2 to 5 state RemoteFactory's half of the model. Section 6 records the questions as asked, each marked with the ruling that answered it. Section 7 is the order of work.

**Relation to the Neatoo document.** `Neatoo/docs/VisionAlignment.md` holds the whole model and Keith's rulings D1 to D19. This document carries over the parts that RemoteFactory enforces, so that each statement lives in the repository of the framework that enforces it, and adds the questions the RemoteFactory consistency audit (`Neatoo/docs/todos/consistency-audit/audit-remotefactory.md`, 86 findings at `2a7b438`) left for Keith.

**What I read:** both VisionAlignment sections 0, 2, 3, 6 and 7; all 86 audit findings and the audit README; the SKR todo; this repository's `CLAUDE.md`, `skills/RemoteFactory/SKILL.md`, `src/Design/CLAUDE-DESIGN.md` headings and the anti-pattern and setter sections, the `[Factory]` type inventory of `Design.Domain`, `OrderLine.cs` and `Order.cs` operation signatures, `Money.cs`, `DesignClientServerContainers.cs`, `mdsnippets.json`, `.gitattributes`, `build.yml`, issue #112, and the NuGet registration index. I have not re-verified the audit's 86 findings against HEAD; the 35 commits since `2a7b438` are the DICT todo and the v1.10.1 release.

---

## 0. Decided

### 0.1 Carried over from Neatoo's rulings (2026-10-05 and 2026-10-09)

These are RemoteFactory doctrine. They were ruled in the Neatoo repository and are recorded here so this repository's guidance derives from them.

| # | Ruling | Where it bites here |
|---|---|---|
| D3 | `[Create]` is the equivalent of `new`; `[Fetch]` gets existing data. Create takes what a constructor would take; Fetch takes a key. | Audit C-05 (a static `[Create]` that chooses new-or-existing), C-07 (a child built with `new`), U-10 (child `[Fetch]` receives scalars; see Q7). |
| D8 | Neatoo uses the latest RemoteFactory. **Correction for the record:** on 2026-10-09 NuGet's registration index lists 1.9.0, 1.10.0 and 1.10.1, all `listed: true`; the newest is **1.10.1**, which is also this repository's source version. The Neatoo pin at 1.9.0 is behind. | Nothing in this repository; the Neatoo session should re-pin. |
| D11 | An operation that may return a new object or an existing one is an `[Execute]` that calls Create or Fetch inside it. Every open, start and ensure seam. | C-05, C-08. The compiled example already exists: `SkillConsultation.StartForPatient` in `references/class-factory.md` (a class-level `[Remote, Execute]`). |
| D12 | A static command is for a command: usually returns nothing or a simple result. A class-level static `[Execute]` is an aggregate factory method or verb with logic before or after the standard operations. An interface factory is a DDD domain service the client calls. **A repository is never an interface factory**; the examples that expose one are issue #112. | C-27 (20 lines in 4 skill files), `Design.Domain/FactoryPatterns/AllPatterns.cs` `IExampleRepository`, `Aggregates/AuthorizedRepository.cs` `IAuthorizedRepository`, `CLAUDE-DESIGN.md` Pattern 2 and 2a. |
| D13 | A context (a plain `[Factory]` instance) when the screen binds to the result and later verbs act on it; a static command when the call returns and is done. The context's verb is a plain instance method that calls the held aggregate's own verb and persists through that root's `Save()`; never an instance `[Execute]`, which does not exist (NF0103/NF0201). | No context example exists anywhere in this repository (skill, docs, Design, reference app). |
| D14 | A root `[Create]` is local. It is `[Remote, Create]` only when creation needs something only the server has, such as tenant defaults, a sequence, or an `[AuthorizeFactory]` check that needs server services (added 2026-10-12: the check runs inside the generated Create on whichever tier runs it; the Save is enforced on the server either way). | U-02: about 25 sites show `[Remote, Create]` with a repository that is never used, including the skill's lead class-factory example. |
| D15 | RemoteFactory's point: when the library holding the object definitions is loaded on both tiers, the client/server seam needs no DTOs and no controllers. Persistence routing is one use of that, not the headline. | C-28 ("persistence routing engine" in 8 files, `CLAUDE.md` "Data Mapper Factory"), C-29 (the decision tree gives read models and orchestration classes no home), C-30. |
| D18 (the `[Remote]` part) | `[Remote]` on a child operation causes no extra round trips: once on the server, `[Remote]` does nothing different. It is still wrong on a child, because the child's persistence is reached from the server, but the reason is placement and trimming, not N+1. | `SKILL.md:70` ("causes N+1 remote calls"), `anti-patterns.md:5` and `:329`, `advanced-patterns.md:587`, `service-injection.md:150-152`, `CLAUDE-DESIGN.md` Anti-Pattern 1. |
| D19 | Interface-first (a public interface per entity, an `internal` concrete, references through the interface) is strongly recommended, not required. No analyzer enforces it. | C-19: the reference app has 160 public concretes and 0 `internal`; `Design.Domain` is interface-first except `SecureOrder`, `ExampleClassFactory`, `ClassExecuteDemo`, `Invoice`. |

Two further Neatoo rulings shape this repository's examples without being RemoteFactory doctrine: D2 (validation is a rule; an exception is an application failure; a factory method does not check data and drop out) and D17 (a value object is a `record`, not `ValidateBase`). Both raise questions here (Q9, Q10).

### 0.2 Decided in this repository

| # | Ruling | Supersedes |
|---|---|---|
| R-A | `src/Design` is the one compiled example set for RemoteFactory. Every C# block in the skill, `docs/` and `README.md` is a MarkdownSnippets region from it; the only hand-written C# is a short block labelled WRONG. `src/docs/reference-app` regions migrate into Design region by region or are retired; `src/docs/samples` is deleted; the Person example stays as an application and stops being a snippet source. | The three-set arrangement; the skill's "Partial/illustrative snippets" category. Ruled 2026-10-09. |
| R-B | The work runs as two iterative todos: one for the example-set consolidation plus the audit's findings against this document; a second for the skill rebuild, which depends on the first. | — Ruled 2026-10-09. |

Rulings of 2026-10-09 on section 6:

| # | Ruling | Answers |
|---|---|---|
| R-C | A child's `[Create]` is the one exception to "child operations are `internal`": it is `public` and local, so client code can call `itemFactory.Create(...)` then `Add` (Neatoo D9). Every other child operation is `internal`. | Q1 |
| R-D | Neatoo's D10 cascade carries over to plain RemoteFactory classes: the root makes its own row and hands the row's child collection to the list factory's `Save`; the list loops, makes a row for each new child, finds the row of each existing child, removes the row of each deleted child, and passes each live child its own row; the child maps itself into the row it is handed; the root flushes once. Load is symmetric: the list's `[Fetch]` receives the rows and hands each child its row. | Q6, Q7 |
| R-E | On a plain RemoteFactory class, a check the user can recover from is a verdict the UI reads before acting (a `CanX` or a result), never a throw in a factory method or hook; authorization is `[AuthorizeFactory<T>]`; a throw is for broken state. The hook documentation drops "validation gates"; the `bool`-returning `[Create]` and the "only HR" `[Update]` go. The pinned `EmployeeWithSave` test changes with Keith's approval, still to be asked for at the edit. | Q9 |
| R-F | A value object is a `record`. It needs no `[Factory]` to cross the wire; it takes `[Factory]` when its construction needs a service (a database, an API call) or authorization. Keith's words: "ValueObjects can still need factories if they need services like a database or api call or there's authorization." The Design example demonstrates the rule: a value object whose construction needs a service carries `[Factory]`; `Money`, which needs none, becomes a plain record. A value object's constructor throws on an invalid value: "If you need rules it's not a value object anymore." | Q10, Q10a, Q10b |
| R-G | An entity can be both a root and a child. "Absolutely an entity can be both a root and a child. The signature of the factory method is many times all the difference you need." Entity duality stays endorsed guidance in `CLAUDE.md`, `CLAUDE-DESIGN.md` and the skill; the Neatoo Design's contrary statement is wrong and the Neatoo session has been told to fix it. | Q2 |
| R-H | A `[Fetch]`-only `[Factory]` class when the client holds and binds to an instance; an interface-factory method returning a record when the call returns and is done. "No DTOs" means no hand-written transfer layer between tiers, not "never return a record". | Q3 |
| R-I | A missing row or target: `[Fetch]` is the one operation that handles it cleanly, with a `bool` signature and `false` (the factory returns null). A command may report it through a complex return type; in a `void` or `Task` command it is an exception. In `[Update]` it is an exception in most cases, with rare edge cases allowed. RemoteFactory supports the common case cleanly without making the rare case impossible, so this is guidance, not an analyzer. The silent skip in the samples goes. | Q4, Q5 |
| R-J | `[Create]` constructors: guidance is a warning, not a rule. "Be aware that `[Create]` constructors are called on deserialization. If there's side-effect behavior a `[Create]` factory method is the better solution." Constructor DI makes the temptation real, which is why the warning is stated. | Q8 |
| R-K | LazyLoad is removed from RemoteFactory. "LazyLoading was a bad idea. It needs to be removed." Issue #114 (techdebt) tracks the removal; `CLAUDE.md` records it; the docs and the skill exclude it from now on. Deferred loading is a `[Remote]` `[Fetch]` on the child list factory, called when needed. | Q11 |
| R-L | Analyzers: a plain-`Task` `[Execute]` generating `Task<Task>` with no diagnostic is **critical** and is fixed soon (issue #113). A non-public setter on a `[Factory]` class and a `[Factory]` class without `partial` get diagnostics as their own small todo. Added 2026-10-10: a class-level `[Execute]` that does not return `Task<T>` of the containing type gets one in the same todo (F8). | Q12, Q13 |
| R-M | `IsNew` and `IsDeleted` are public `get; set;`. The docs' "either design is viable" goes. | Q14 |
| R-N | The trimming reference says "persistence code", never "business rules", and states the one required `TrimMode`. | Q15 |
| R-O | Reference-app regions that migrate into Design take Design's Order domain. | Q16 |
| R-P | T18, T19 and T20 join the shared habit-trap list with the wording in section 4; the Neatoo session mirrors them. | section 4 |

---

## 1. What I found

### 1.1 Four example sets, not three

| Set | Hand-written `.cs` | `#region`s | Tests | Feeds snippets |
|---|---|---|---|---|
| `src/Design` | 35 files, about 8,500 lines | **0** | 115 (`[Fact]`/`[Theory]`), on real Remote / Server / Logical containers (`DesignClientServerContainers.cs:241-243`) | No. `CLAUDE.md` names it the source of truth. `Design.sln` is not built or tested in CI; `build.yml` builds only `src/Neatoo.RemoteFactory.sln`. |
| `src/docs/reference-app` | 110 files, about 9,900 lines | 178 | Yes, but on one Logical container; nothing serializes (audit T-25) | **All 162 embeds** in the skill, `docs/` and `README.md`. 160 public concretes, 0 `internal`; 14 `[SuppressFactory]`, so some published "compiled" regions are never seen by the generator (T-22). |
| `src/docs/samples` | 1 file, 275 lines | 5 | None; in no solution | Yes: duplicates every README snippet, the copy with eight `[Remote] public` methods (T-14). |
| `src/Examples/Person` | 22 files, about 1,100 lines | 0 | — | No. The Blazor WASM example application. |

The skill has 117 C# blocks; 34 are embedded. `anti-patterns.md`, `factory-events.md` (24 blocks), `trimming.md` and `polymorphic-hierarchy.md` have no embeds at all. The eight events examples that insert, flush and commit inside a `[Create]` (C-06) are all hand-written.

Housekeeping the Neatoo side has done and this repository has not: no `.claude/rules` file naming the snippet source; `.gitattributes` has `*.sh text eol=lf` only (mdsnippets writes LF into `.md`); `Design.sln` is not in CI. The installed skill at `~/.claude/skills/RemoteFactory` differs from `skills/RemoteFactory` in 9 of 12 files and predates v1.9.0.

### 1.2 Where the audit's 86 findings fall

| Class | Count | What most of them are |
|---|---|---|
| CONTRADICTS a settled point | 30 | "`[Remote]` executes on the server" comments (C-01 to C-04); writes and validation inside `[Create]`, hooks and value-object constructors (C-06, C-11 to C-15); commands that write another aggregate's row (C-09); repository-named interface factories (C-27); the persistence-routing framing (C-28 to C-30); public concretes everywhere (C-19, C-20). |
| INTERNAL contradiction | 15 | Private setters three ways (I-01); `[Service]` "server-only" vs "that tier's container" (I-02); `[Execute]` return type (I-03); `Save` not reassigned (I-07). |
| STALE | 29 | LazyLoad loader "re-created on the client" (T-01); "CS0260 without `partial`" (T-02); write methods callable on the factory (T-03); `[Event]` remnants (T-09); wrong DTO shapes (T-10); 409 claim (T-11); `IFactoryCore` (T-13); `CODING_STANDARDS.md` whole (T-15). |
| UNRULED | 12 | Section 6. |

### 1.3 What Design already answers that the published sets contradict

Design is the declared truth, and on several of the audit's open points it has already taken a position that the reference app, the skill and the docs do not follow:

- **Child operations are `internal`, `[Create]` included** (`OrderLine.cs:140-141, 166-167`; `OrderLineList` the same). The reference app keeps child `[Create]` public so the client can add a line (U-01, C-02).
- **Setters are public** (`CLAUDE-DESIGN.md` Anti-Pattern 4, Critical Rule 5). The skill says the same; the docs say private setters work with `[JsonInclude]`; the reference app uses `{ get; private set; }` 212 times with neither (I-01).
- **Real client/server containers** in `Design.Tests`. The reference app's "two-container" tests resolve everything from one Logical container.
- **Interface-first** throughout the aggregates.

And two places where Design is itself off the rulings: `Money` is a `[Factory] partial record` with a `Create` (D17 says a value object needs no factory; Q10), and `IExampleRepository` / `IAuthorizedRepository` are interface factories (#112).

---

## 2. The model: RemoteFactory's statements

Numbered as in the Neatoo document so the two skills share one list. Statements 5, 7, 8 and 9 are Neatoo's and the application architecture's; they are not restated here.

1. **One domain assembly, loaded on both tiers.** The browser and the server run the same compiled classes. A domain object can exist on either side, and its code runs wherever the object is. Each tier has its own container. The tiers differ in which services are registered, not in which classes exist. This is RemoteFactory's premise (D15): when the library holding the object definitions is on both tiers, the seam needs no DTOs and no controllers.

2. **The factory operation is the seam.** An object is created, loaded, sent across the wire and written only through a factory operation. That operation is where services are injected, authorization is checked, the transaction lives and events are raised. Code outside an operation holds no server service. Nothing with `[Factory]` is constructed with `new`.

3. **The operation attribute is a claim about persistence.**
   - `[Create]`: this object is not in the store yet. Its first save inserts. It takes what a constructor would take and is local unless creation needs the server (D3, D14).
   - `[Fetch]`: this object reflects what the store holds, and is null when the store holds nothing. It takes a key (D3).
   - `[Insert]`, `[Update]`, `[Delete]`: write this object's own row. They are never called by name and are not on the generated interface; `Save` routes by `IFactorySaveMeta` state.
   - `[Execute]`: anything else. Decide, orchestrate, compute. An operation that might return a new object or an existing one is an `[Execute]` that calls Create or Fetch inside it (D11). `[Execute]` obeys `[Remote]`.

   The claim is what matters, not what the body does. A `[Create]` may read the database for defaults and is still a Create. Building a loaded child with `Create` is wrong even when every value is right, because the child then claims to be new and the next save inserts it again.

4. **`[Remote]` marks where a call crosses, not where code lives.** It goes on the operations the client calls: a root's operations, a command that needs the server, a child list's `[Fetch]` when the client loads it on demand. A `[Service]` parameter is resolved on the tier where its method runs, so a server-only service belongs only on a method that runs on the server: one that is `[Remote]` or `internal`. A child's operations are `internal` and never `[Remote]`, because the call is already on the server when they run; `[Remote]` there is not an N+1 problem, it is a placement and trimming error (D18). Whether a child's `[Create]` is the exception is Q1.

6. **Every object writes itself, and only itself.** The root's operation opens the transaction and commits. Each child's own `[Insert]`, `[Update]` or `[Delete]` maps and writes its own row, reached through the child factory's `Save`. A parent never maps a child's row. An operation touches only its own aggregate's rows; another aggregate's fact comes through that aggregate's domain service. A command that mutates an aggregate fetches it through its factory, calls a domain method, and saves through the root factory; it does not write the row (C-09).

Three mechanisms that follow from 1 and 4 and that the skill must state plainly, because they are what a mainstream .NET developer gets wrong by habit:

- **Constructor injection is both tiers; `[Service]` parameters are the tier that runs the method.** A class the client deserializes cannot take a server-only service in its constructor (C-23). A method-injected service is never stored in a field or captured in a delegate that outlives the operation (C-21, C-22).
- **Visibility decides the guard and the trimming.** `[Remote] internal` is promoted to public on the factory interface and crosses. Plain `internal` without `[Remote]` is guarded: it throws `"Server-only method called in non-server runtime."` on a client whose `IsServerRuntime` switch is `false`, and its body is trimmed (T-16). `[Remote] public` is NF0105. A `public` non-`[Remote]` method is unguarded and runs on whichever tier calls it.
- **A static `[Execute]` without `[Remote]` is a local-only delegate**, registered on the client unguarded and untrimmed. It is for logic that must run on the calling tier with that tier's services (client-side engines, pure computation on the client). A command that needs the server is `[Remote, Execute] private static _Name`.

---

## 3. Each DDD concept, with RemoteFactory

RemoteFactory's column of the Neatoo document's table, corrected to the rulings. Neatoo's column is in the Neatoo document and is not copied here.

| DDD concept | With RemoteFactory | Compiled example today |
|---|---|---|
| Aggregate root | A `[Factory]` class behind a public interface (recommended, D19). `[Remote]` on its operations the client calls. Its Insert and Update own the transaction and start the cascade. `IFactorySaveMeta` gives `Save` routing. `Save` returns a new instance; reassign it. The same class may also carry `internal` child-context operations, distinguished by signature (R-G). | `Design.Domain/Aggregates/Order.cs` |
| Child entity | A `[Factory]` `internal` class. Its `[Create]` is `public` and local so the client can build one before adding it (R-C); every other operation is `internal` and not `[Remote]`. It maps its own row, which the list hands it; the parent reaches it through the child factory's `Save` (R-D). | `Design.Domain/Entities/OrderLine.cs` (writes: R-D) |
| Child collection | A `[Factory]` list the parent holds; list-level `[Fetch]`, `[Remote]` when the client loads it on demand; removed items tracked for delete; the list runs the R-D cascade. | `OrderLineList` (no write path yet; R-D) |
| Value object | An immutable `record`. It crosses the wire as a parameter or a property with no factory; it takes `[Factory]` when its construction needs a service or authorization (R-F). | `Design.Domain/ValueObjects/Money.cs` (has `[Factory]`, takes no service; Q10a) |
| Factory | The generated `IXxxFactory`. Nothing is constructed with `new`. | every Design type |
| Repository | Not supplied. An application interface, injected by `[Service]` into persistence operations, server-only. **Never an interface factory** (D12, #112). | `IOrderRepository` in `Order.cs:342` (correct shape) |
| Domain service | An interface with an `internal` implementation in DI. Server-only: reached by `[Service]`. Callable by the client: an **interface factory**, which on the client resolves to the generated proxy, so a call through it is a wrapped server call. | `AllPatterns.cs` `IExampleRepository` (wrong name; #112) |
| Command or use case | A static `[Factory]` class with `[Remote, Execute] private static`, returning nothing or a simple result. A class-level static `[Execute]` when the result is that aggregate (D12). | `AllPatterns.cs` `ExampleCommands`; `ClassFactoryWithExecute.cs` |
| Context | A plain `[Factory]` instance when the screen binds to the result and later verbs act on it (D13). Its verbs are plain instance methods that call the held aggregate's verbs; it persists through that root's `Save()`. | **none** |
| Read model | A plain `[Factory]` class with `[Fetch]` only. No `[Create]`. | **none** in Design (the reference app's have `[Create]`, C-18) |
| Create-or-fetch seam | A class-level `[Remote, Execute]` that calls Fetch, then Create (D11). | `SkillConsultation.StartForPatient` (reference app; migrates) |
| Domain event | `IFactoryEvents.Raise` inside an operation, handled by a static `[FactoryEventHandler<T>]` method. Dispatch phase chooses Immediate, AfterFlush or AfterCommit. Events relay to the client. | `FactoryPatterns/FactoryEvent*.cs` |
| Authorization | `[AuthorizeFactory<T>]` or `[AspAuthorize]` on the factory. | `Aggregates/AuthorizedOrder.cs`, `ParamAuthOrder.cs` |
| Consistency boundary | One root `Save` is one transaction. Across roots, an `[Execute]` seam or an event. | `Order.cs` |

---

## 4. The skill's habit-trap rows that RemoteFactory owns

The rubric is shared with the Neatoo skill (`Neatoo/docs/todos/SKR-neatoo-skill-rebuild/todo.md`); the T-numbers are kept so a fresh-session test is one list. Each RemoteFactory `SKILL.md` carries a pointer block naming the other skill's rows; no content is copied.

| # | The question the habit asks | RemoteFactory's answer | Compiled example | State |
|---|---|---|---|---|
| T3 | How do I load-or-create? | A class-level `[Remote, Execute]` that calls Fetch, then Create (D11). Never a `[Create]` that inspects, never a `[Fetch]` that creates. | `StartForPatient` | exists in the reference app; migrates to Design |
| T4 | Can I `[Create]` from loaded data? | No. Create = new, Fetch = existing (D3). A loaded child is built with its own `[Fetch]`, handed its row by the list (R-D). | `OrderLine.Fetch` | exists; row-shaped per R-D |
| T5 | How does the client call the repository? | It doesn't. A repository is `[Service]` on a server operation. A service the client calls is an interface factory, a domain service (D12). | `IOrderRepository` as `[Service]`; a domain-service interface factory | second half needs writing (#112) |
| T6 | Where does my application service go? | Static command when the call returns and is done; class-level `[Execute]` when the result is the aggregate; context when a screen binds to it (D12, D13). | `ExampleCommands`; `ClassExecuteDemo`; a context | context needs writing |
| T11 (RF half) | The parent saves its children's rows? | Each object maps its own row. The root hands the row's child collection to the list factory's `Save`; the list loops and hands each child its row; the child's own `[Insert]`/`[Update]`/`[Delete]` maps it (R-D). | `Order` + `OrderLineList` + `OrderLine` | child writes need writing (R-D) |
| T13 | `Save()` updates my object? | It returns a new instance; reassign it. In Remote mode the original still reports `IsNew`. | `Order` save test | exists; the docs' `await factory.Save(x)` twice-in-a-row comments go (I-07) |
| T14 | Which services can a constructor take? | Both tiers. `[Service]` parameters resolve on the tier that runs the method, which for `[Remote]` and `internal` is the server. | `CtorInjectionExample.cs` | exists |
| T15 | Should the child's `[Insert]` be `[Remote]`? | No; `internal`. The call is already on the server. Not because of N+1 (D18). | `OrderLine` | exists; prose fix in 6 places |

Three rows that are RemoteFactory's but have no T-number in the SKR table yet, proposed for the shared list:

| # | The question the habit asks | RemoteFactory's answer | Compiled example |
|---|---|---|---|
| T18 | Does `[Remote]` mean "runs on the server"? | No. It marks where the client's call crosses. Once on the server, it does nothing. Placement is `internal` plus `[Service]`; `[Remote]` is the entry point. | `Order` root vs `OrderLine` child |
| T19 | My command needs the server, so plain `[Execute]`? | `[Remote, Execute] private static`. A bare static `[Execute]` is a local delegate on the calling tier. `[Execute]` returns `Task<T>`; plain `Task` generates code that does not compile until #113 lands. | `ExampleCommands`, `LocalExecuteTests` |
| T20 | I'll return a DTO from a controller for this screen? | A `[Fetch]`-only `[Factory]` class the client holds, or a domain-service interface factory returning a record. No controller, no DTO class. | read model (to write); interface factory |

---

## 5. Known defects and framework questions

Recorded here so the skill names each pain with its issue or analyzer, as SKR's AC-7 does for Neatoo.

| # | What | Evidence | Action proposed |
|---|---|---|---|
| F1 | `[Execute]` returning plain `Task` generates `Task<Task>`; CS0266 in generated code, no diagnostic. NF0102 catches only non-`Task` returns. | Neatoo audit README section 4, confirmed on 1.9.0; audit I-03 reads the same from `StaticFactoryRenderer.cs:99` | **Critical, issue #113** (R-L). Generator handles `Task` or NF0102 fires on it; a generator test either way. |
| F2 | A lazy-loaded property reaches the client with no loader; `LoadAsync()` throws. The docs say the loader is re-created. | Audit T-01: `OrdinalRenderer` emits `new LazyLoad<T>()`; `LazyLoad.cs:225-228` throws when `_loader == null`. Source reading only. | **LazyLoad is removed, issue #114** (R-K). Excluded from docs and skill now. |
| F3 | Ordinal serialization drops private-setter properties; the generator never reads `[JsonInclude]`. | Audit I-01: `FactoryGenerator.Types.cs:430-436, 496-508`; on-disk `Employee.Ordinal.g.cs` omits `Id` and `IsNew`. Source reading only. | Design already rules public setters. Delete the `[JsonInclude]` doc passages. Analyzer for a non-public setter on a `[Factory]` class (R-L). |
| F4 | A non-`partial` `[Factory]` class silently falls back to named JSON; the skill says CS0260. | Audit T-02: `FactoryModelBuilder.cs:304-308` | Diagnostic (R-L). |
| F5 | `Save` on an authorized factory for a new-and-deleted object throws `NotAuthorizedException`; a missing routed operation throws `NotImplementedException`; neither documented. | Audit T-12; issue #104 | Document in the save table. |
| F6 | No way to declare a generated factory `internal`. | Neatoo audit A.2 | Already known; link the issue if one exists. |
| F7 | `[Remote]` on a child is documented as N+1. | D18 | Prose fix, six sites. No analyzer: `[Remote]` on a child is a placement smell, not a failure. |
| F8 | A class-level `[Execute]` that does not return `Task<T>` of the containing type fails in generated code with no diagnostic: plain `Task` gives CS0029 (`void` to the service interface). The generator checks only NF0103 (static) and NF0102 (a `Task`); `BuildClassExecuteMethod` assumes the containing type ("Enforced" in its comment, enforced nowhere). The rule is documented as skill anti-pattern #9. | Probe on the #113 branch, 2026-10-10; `FactoryModelBuilder.cs` `BuildClassExecuteMethod`; `FactoryGenerator.Transform.cs` Execute branch | Diagnostic in Todo 3 (R-L). |
| F9 | Generated files name `Task`, `CancellationToken`, `IServiceProvider` and `InvalidOperationException` unqualified and rely on the consumer's implicit usings; with them off, any factory fails CS0246 in generated code. | Issue #97 (open, `bug`), found in the EXRM todo | Already tracked; no new work here. |

---

## 6. Questions

Each has the answer I would guess. Reply by number. Audit IDs in parentheses.

**Q1. How is a child created on the client?** (U-01, C-02) Ruled: R-C. Design's `OrderLine.Create` and `OrderLineList.Create` become `public`.

**Q2. Does entity duality stay?** (U-09) Ruled: R-G. Endorsed; the Neatoo Design is the one to fix.

**Q3. Read model versus interface-factory query.** (U-06) Ruled: R-H.

**Q4. Missing row in `[Update]` or `[Delete]`.** (U-04) Ruled: R-I. Exception in most cases; rare edge cases allowed.

**Q5. "Target not found" in a command.** (U-05) Ruled: R-I. Complex return type or, in a `void`/`Task` command, an exception.

**Q6. The cascade on plain RemoteFactory classes.** (U-03, U-10) Ruled: R-D. Design's `Order`/`OrderLine` gets the worked write path.

**Q7. Who maps a child's row on load?** (U-10) Ruled: R-D, symmetric with the write.

**Q8. May a `[Create]` constructor have side effects?** (U-07) Ruled: R-J. A warning in the guidance, not a rule.

**Q9. Validation by throwing in hooks and write methods.** (C-11, C-12, C-13, C-15) Ruled: R-E.

**Q10, Q10a, Q10b. Value objects.** Ruled: R-F. `Money` becomes a plain record; a value object that needs a service carries `[Factory]`; constructors throw.

**Q11. LazyLoad on the client.** (T-01, F2) Ruled: R-K. Removed; issue #114.

**Q12, Q13. Which silent failures get an analyzer?** (F1, F3, F4) Ruled: R-L. Plain-`Task` `[Execute]` is critical, issue #113; the other two get diagnostics as their own small todo.

**Q14. `IsNew` / `IsDeleted` shape.** (U-12, I-01) Ruled: R-M. Public.

**Q15. Trimming guidance scope.** (C-26, I-10) Ruled: R-N.

**Q16. The reference app's domain.** Ruled: R-O. Orders.

**T18 to T20.** Ruled: R-P. Kept; the Neatoo session has the wording.

No questions remain open. Section 6 is retained as the record of what was asked.

---

## 7. Proposed order of work

1. Done 2026-10-09: sections 2 to 5 corrected by the rulings; section 6 answered; section 0.2 holds R-A to R-P.
2. **Issue #113 (critical)**: the plain-`Task` `[Execute]` fix, soon and ahead of the documentation work.
3. **Todo 1, example set and audit** (R-A): `.claude/rules/design-snippets.md`, `*.md text eol=lf`, `Design.sln` in CI; the missing Design examples (context, read model, domain-service interface factory replacing `IExampleRepository` (#112), child writes and the R-D cascade with `OrderLine.Create` made `public` (R-C), create-or-fetch, a static command and a local static `[Execute]`, `Money` as a plain record beside a value object that needs a service (R-F)); LazyLoad excluded from docs and skill (R-K, #114); then the audit's findings fixed against this document region by region, with `src/docs/reference-app` regions moved into Design's Order domain (R-O) or retired, `src/docs/samples` deleted, and 162 embeds re-pointed. `Design.sln` and `Neatoo.RemoteFactory.sln` both built and tested, output to files.
4. **Todo 2, skill rebuild**: `SKILL.md` leads with the model (section 2), then the trap rows (section 4, T3 to T6, T11, T13 to T15, T18 to T20) at their decision points, then the type / operation / tier decisions and trimming, then mechanism references; every C# block a Design region except blocks labelled WRONG; the pointer block to the Neatoo skill's rows; section 5's defects named with their issue or analyzer; a fresh-session test against the trap table; sync to `~/.claude/skills/RemoteFactory`; message the Neatoo session (SKR plan 007 waits on it).
5. **Todo 3, analyzers** (R-L): non-public setter on a `[Factory]` class; `[Factory]` class without `partial`; class-level `[Execute]` not returning `Task<T>` of the containing type (F8).
6. **LazyLoad removal** (#114): its own todo, a breaking change and a major bump, after the documentation work has excluded it.
