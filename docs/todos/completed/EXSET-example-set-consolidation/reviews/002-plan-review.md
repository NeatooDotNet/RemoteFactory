# EXSET-002 plan review — 2026-10-12

**Reviewer:** plan-reviewer (tight). **Verdict:** APPROVED. No veto-tier findings in either pass.

Pass A: the plan matches R-C, R-D, D14, R-I, R-E, R-M and §2 statement 6; the 009 deferral traces to the Plan Index.

Pass B reality check: mixed visibility (public `[Create]`, internal other operations) yields a public factory interface with internal members; a list-level `Save(target, rows)` generates; a line marked `IsDeleted` crosses the wire inside the list since #119. **Correction to Current State:** remote calls in the Design test containers share one server scope (`HandleRemoteDelegateRequest` is transient and closes over `serverScope.ServiceProvider`); a singleton store is still right for `Design.Server`, but the tests cannot tell scoped from singleton.

## Callouts (all affect Must criteria)

| # | Finding | Affects | Reachable by | Disposition |
|---|---|---|---|---|
| 1 | The aggregate returned by a save is not clean: an inserted line keeps `IsNew`, a removed line stays in the list | AC-2 · Must | save, keep editing the result, save again | Amend: line clears `IsNew` after its Insert; the list drops removed lines after its write; add a second-save Acceptance bullet |
| 2 | The list's `Save` routes on the list's `IsNew`/`IsDeleted`, which never cross; a missing branch throws `NotImplementedException` | AC-2 · Must | every client save of an order | Amend: the list is never new or deleted (`IsNew`/`IsDeleted` fixed false), its interface carries `IFactorySaveMeta` |
| 3 | An `internal` `Create` passes every container test (guard switch defaults on, `InternalsVisibleTo`) while R-C/D14 are unmet | AC-2 · Must | `Create` left `internal` with only `[Remote]` dropped | Amend: `Order`, `OrderLine`, `OrderLineList` `Create` are `public`; evidence is the Blazor page compiling calls to them |
| 4 | Step 8 misses CLAUDE-DESIGN `:761-783`, `:828`, `:374` and `OrderLine.cs:21-33`, `:160` | AC-4 · Must | editing only the listed passages | Amend: add them to Step 8 |
| 5 | Seeded orders are `Submitted`, but adding a line requires Draft | AC-2 · Must | running the edit scenario against order 123 | Amend: seed a Draft order for the edit scenarios |

## Theoretical (not triaged)

- `Design.Server` left `AddScoped` would lose saves between requests while the tests pass; the plan already says singleton.
- `Order.AddLine`/`Submit`/`Cancel` throw on status; R-E covers factory methods and hooks only.
- A filtered `IEnumerable<IOrderLine>` enumerator would hide deleted lines from code that enumerates through the interface; STJ writes through the base list.
