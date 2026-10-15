# EXSET-004 plan review — 2026-10-13

**Reviewer:** plan-reviewer. **Verdict:** APPROVED. No veto-tier findings.

Pass A: R-F settles C-14 (constructors throw); Step 6 is exactly C-06's own remedy in the audit (a save on an `IFactorySaveMeta` entity, or a `[Remote, Execute]` command). Keith's four rulings are carried out correctly.

Pass B reality check:
- A static `[Fetch]` on a `[Factory]` record compiles and runs (`RecordTestObj.cs:14-26`), and a static Fetch can return `Task<T?>` (`FetchCombinations.g.cs:360-367`). Step 1 has only to probe a null crossing the wire.
- A class-level `[Remote, Execute]` that injects its own factory and calls Fetch, then Create, is the reference app's `StartForPatient`:
  - nesting is supported (`FactoryEntryCall` drains at the outermost call);
  - an `internal` non-`[Remote]` `[Fetch]` runs because the Execute is already on the server;
  - `OpenDraft` must return `Task<IOrder>` (F8, no diagnostic);
  - a distinct method name avoids a generator-renamed `Fetch` overload.
- `ClassExecuteDemo`'s local Create works from every Execute: the client one (`ScoreLocally`, `ScopesWithNoWire`) and the server ones.
- A mixed `[Remote]`/non-`[Remote]` Insert/Update set still yields a remote Save (`FactoryModelBuilder.cs:707`).
- The Blazor client references none of the changed types. Nothing in `src/Tests` touches Design types.

## Callouts (all affect a Must criterion)

| # | Finding | Reachable by | Triage |
|---|---|---|---|
| 1 | The Intent and the Should bullet ("no `[Create]` takes a `[Service]` other than a child factory") are stricter than doctrine. §2.3 lets a Create read defaults, and D14 names a sequence. The grep fails on `ExampleClassFactory.Create` (`AllPatterns.cs:123-137`, `GenerateId`) and `LazyLoadExample` (#114) | running the bullet's grep | **Amend**: the plan's wording was wrong, not the criterion. The bullet and Intent become "no `[Create]` raises an event or writes; none in this plan's files takes a service" |
| 2 | `RegisterMatchingName` runs on the test client too (`DesignClientServerContainers.cs:302-303`), so an `ITaxTable`/`TaxTable` pair is registered there and "not registered on the client" fails. The test server and local lists are separate from `ServerServices` | naming the pair by convention | **Amend**: the implementation takes a name the convention does not match and is registered explicitly in `ServerServices` and the test server and local lists. The Current State line about `IExampleService` is corrected to the test client |
| 3 | ServerOnly relay test: (a) the insert's relayed event lands in the same aggregator, and the relay is fire-and-forget; (b) nothing sets `IsNew = false` after insert, so the second Save inserts again. `Relay_NoConsumerRegistration_NoOpDefaultUsed` passes vacuously once Create is local | reshaping as Create, Save, change, Save | **Amend**: `CheckoutOrder` clears `IsNew` after insert as `Order` does; the ServerOnly test waits for the insert's event before the update and asserts that no second one arrives; the NoOp test saves |
| 4 | "Static command" reads two ways. Left on `Invoice`, it needs a Create to build through, which it loses; moved to a static class, `IInvoiceFactory`/`IInvoiceArchiverFactory` go and four callers move to the delegate form. Current State misses `CLAUDE-DESIGN.md:38, 234-246, 395, 397`. `:234-246` is a hand-written `[Remote, Create] Finalize` that stages, flushes and commits: C-06 in Design's own guide | leaving the method on `Invoice` | **Amend**: static command classes like `QuarterClose`; the callers move to the delegates; the CLAUDE-DESIGN lines are named in Step 7 and Current State |
| 5 | "Wire shape unchanged" cannot hold: a `[Factory]` `Money` writes an ordinal array, and a plain record goes through `RecordBypassConverterFactory` as a named object (ordinal is the test default) | honouring the constraint | **Amend**: "operations and round trip unchanged" |
| 6 | Step 1's fallback "or must it be a static `[Create]`" makes a Create look up a key and return null, against D3 and R-I | the probe failing | **Amend**: a failed probe is a stop-and-ask |

## Theoretical (not triaged)

- Read literally, "No `new` on a `[Factory]` type" forbids `TaxRate`'s static Fetch from constructing itself. That is the `RecordTestObj` precedent, and a static factory method has no other way to do it.
- A plain-record `Money` in a publish-trimmed client depends on STJ metadata surviving trimming.
