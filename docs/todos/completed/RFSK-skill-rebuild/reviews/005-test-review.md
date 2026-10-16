# Test Review — RFSK-005 — 2026-10-16 — Round 1

Written by the orchestrator from the test-reviewer's report.

**Verdict: APPROVED**

**Logs:** main build 0 errors, 2 warnings (the WASM `NativeFileReference` notices from `OrderEntry.BlazorClient`); Design build clean. Unit 871, integration 664 (5 skipped), Design 180 (1 skipped), both TFMs; `Person.DomainModel`, `Person.Client` and `Person.Server` appear in the build log.

## Veto-tier

None. All three bullets serve AC-10, Optional.

- **Bullet 1:** the diff removes `[JsonInclude]`, `[DynamicDependency]`, `[DynamicallyAccessedMembers]`, the private setters and the two usings; the class is `internal partial class PersonModel`; no annotation remains outside `Generated/`.
- **Bullet 2:** the generated `PersonModel.Ordinal.g.cs`, timestamped with the build, reads `IsDeleted` and `IsNew` at positions 3 and 4, writes both, lists both in the property names, the values array and the from-values constructor; the factory registers the converter (`PersonModelFactory.g.cs:687`).
- **Bullet 3:** the logs confirm it.

## Callouts

None. The change moves `PersonModel` from reflection-based serialization to the generated ordinal converter. That path is generated code the library's own tests cover (`RemoteSaveRoundTripTests`, `DeleteBehaviorTests`, `UpdateBehaviorTests`, `MixedWriteRoundTripTests`); pinning it for Person would need a test project that does not exist, so "no place to pin" is honest.

## Could not verify / Theoretical

None.
