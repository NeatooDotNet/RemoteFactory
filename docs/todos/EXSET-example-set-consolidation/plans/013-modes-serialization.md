# Factory modes and serialization pages

**Plan #:** 013
**Date:** 2026-10-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4, AC-5
**Status:** Draft
**Last Updated:** 2026-10-15
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

Split from EXSET-006 (Keith, 2026-10-15). It follows 006: its logging and registration embeds reuse 006's regions. Design keeps the default format (006 simplified to the happy path), so the format sections are prose or embed `readme-server-setup`.

---

## Scope

Re-point every C# block on `docs/factory-modes.md` and `docs/serialization.md` to `src/Design`, and replace the skill `setup.md`'s hand-written factory-modes block (`NeatooFactory.Local`, which does not exist).

**Modes.**
- `modes-server-config` → `readme-server-setup` and `modes-remote-config` → `getting-started-client-program`.
- `modes-logical-config` is a new Logical-mode registration, pinned by a test that runs an Order's create, save and fetch in one container.
- `modes-logging` reuses 006's logging region.

**Serialization.**
- `serialization-config` and `-debug-named`: Design registers no options (006), so these become prose or retire into `readme-server-setup`; the options overload is stated in prose.
- `-ordinal-versioning` comes from Order's properties, with a test that pins the ordinal order.
- `-interface` uses `Order.Lines`, with a test that pins the `$type`/`$value` shape.
- `-references` follows a probe (below).
- `-logging` reuses 006's logging region.

Delete the reference app's `Modes/`, `FactoryModes/` and `Serialization/` samples, together with the regions in those families that no page embeds.

**Findings to fix:**
- C-04: Remote mode sends "all operations" to the server (D18);
- C-07: a `[Factory]` child built with `new`;
- C-19/C-20;
- I-01;
- T-22;
- the invented log messages at `factory-modes.md:75`;
- ordinal order called "alphabetical";
- the interface wire shape;
- "resolves from DI", which holds only for Named;
- "Remote mode only";
- the LazyLoad section (AC-5).

**Design's own stale statements:** `SerializationTests.cs:35, :41, :47-49, :58-66, :95-114, :195-199`.

**First step: probe reference preservation.** A `[Factory]` aggregate with a back-reference, or a shared reference, round-trips through the client/server containers in both formats. If it duplicates the reference, cycles or throws, STOP and ask. That is a library behaviour, and the references section waits on the ruling.

---

## Carried from EXSET-006's recon and plan review

**Ordinal order and membership.**
- Order is base class first, then `StringComparer.Ordinal` (`FactoryGenerator.Types.cs:410-419`).
- Only properties with a public, internal or init setter are included (`:436-444`).
- Order has no non-public setter, and adding one goes against R-M. Membership is pinned by the get-only `Total` being left out; the private-setter rule is stated in prose (review callout 7).

**Named fallback.** A type gets the Named shape when it is not:
- partial;
- non-nested;
- a non-collection;
- possessed of at least one ordinal property;
- possessed of a usable parameterless constructor, or for a record, a primary constructor (`FactoryModelBuilder.cs:308-313`; review callout 8).

**Construction and wire shape.**
- Ordinal builds with `new T{...}` (`OrdinalRenderer.cs:355-362`). Named uses DI only for a registered type.
- An interface is written as `{"$type": FullName, "$value": ...}` (`NeatooInterfaceJsonTypeConverter.cs:83-94`).

**References.**
- The generated ordinal converter writes no `$id` or `$ref`, and nothing reads `NeatooReferenceResolver.Current`.
- Preservation is tested only on plain types (`SharedReferenceTests.cs:128-150`).

**Remote mode.** Only `IsRemote` methods are reassigned to the remote call (`ClassFactoryRenderer.cs:263-275`).

**Logging.** The messages `factory-modes.md:75` quotes exist nowhere. The real ones are `Factory operation {Operation} started for {TypeName}` (generated, under the user's namespace) and `Remote delegate call started`.

**Process.** `mdsnippets.json` has `TreatMissingAsWarning: true`, so the log must be searched for missing-snippet warnings.
