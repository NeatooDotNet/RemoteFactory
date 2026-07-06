# TRIM-002 — `[Factory]` entity property-graph DTO discovery

**Plan #:** 002
**Status:** Draft
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft
**Related Todo:** [../todo.md](../todo.md)

## Scope

Extend DTO discovery to descend into `[Factory]`-annotated types' public property graphs without treating the entity itself as a DTO. Today `WalkFactoryReturn` rejects a `[Factory]` root (correct — entities are preserved via DI registration) but returns before walking its properties, so a plain DTO reachable *only* as an entity property is never discovered and gets trimmed on the client. Consumer evidence from the zTreatment cut-over: `TreatmentBanner` (a record property on the `[Execute]`-opened `TreatmentContext` aggregate) and `DashboardContactResult` (a `List<T>` property on the `PatientSearchQuery` factory entity) both required manual LinkerConfig entries. The descent must reuse the same bucket-sort emission as TRIM-001 (Register vs PreserveType), share the visited-set for cycle safety across entity graphs (entities referencing entities, child lists), and skip entity-typed properties themselves while walking through them for DTO-typed leaves. This is the most design-open of the three plans — settle the walk's boundary rules (which factory-rooted types get their properties walked: all `[Factory]` types in the compilation, or only those reachable from factory method signatures) at draft time. Includes a publish-trimmed `RemoteFactory.TrimmingTests` case (DTO reachable only via entity property) and a `docs/trimming.md` update. Does NOT change entity preservation itself (already handled by `NeatooFactoryRegistrar` + DI registration).
