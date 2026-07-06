# TRIM-001 — Positional-record preservation in factory signatures

**Plan #:** 001
**Status:** Draft
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft
**Related Todo:** [../todo.md](../todo.md)

## Scope

Make the factory-signature DTO walk preserve positional records (types with only parameterized ctors) instead of silently dropping them. Today `DtoTypeWalker.WalkFactoryReturn` requires `HasParameterlessCtor`, so a record like zTreatment's `StartVisitResultV2` — returned from a `[Remote, Execute]` command — gets no preservation and the trimmed client throws `DeserializeNoConstructor`. The fix shape already exists in the codebase's own history: bucket-sort discovered types the way the (now-dead) `WalkEventRoot` did — parameterless ctor → `DtoConstructorRegistry.Register<T>(() => new T())`, parameterized/record → `DtoConstructorRegistry.PreserveType<T>()` (deserialization then flows through the existing `RecordBypassConverterFactory`). Applies uniformly to return types, non-service parameters, and nested properties of discovered DTOs. Includes disposing of the dead `WalkEventRoot` helper (delete or refit as the shared bucket-sort walk), a publish-trimmed test in `RemoteFactory.TrimmingTests` covering record-as-return, record-as-parameter, and record-nested-in-DTO, and the `docs/trimming.md` "What Qualifies as a DTO" correction. Does NOT touch `[Factory]` entity property descent (TRIM-002) or event preservation (TRIM-003).
