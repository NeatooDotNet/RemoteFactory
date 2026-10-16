# Code Review — RFSK-005 — 2026-10-16

Written by the orchestrator from the code-reviewer's report.

**Verdict: APPROVED**

**Logs:** main build 2 warnings, both the WASM `NativeFileReference` notice on `OrderEntry.BlazorClient`; Design clean. Unit 871, integration 664 (5 skipped), Design 180 (1 skipped), both TFMs.

## Direction and shape

Confined to `PersonModel.cs`. The flags are public `get; set;`; the class is `internal partial`; no `[JsonInclude]`, `[DynamicDependency]`, `[DynamicallyAccessedMembers]` or their comments remain; `MarkDeleted` and its reworded comment stay. It matches R-M and `trimming.md`.

- **Wire round-trip.** The generated `PersonModel.Ordinal.g.cs` reads and writes all nine public properties (`Created`, `Email`, `FirstName`, `IsDeleted`, `IsNew`, `LastName`, `Modified`, `Notes`, `Phone`), the `field`-backed ones through their setters, as the named path did. It builds with `new PersonModel()` and overwrites the constructor's `DateTime.Now` from the payload, as before.
- **Registration.** `PersonModelFactory.g.cs:687` registers the converter. Neither `Person.Client/Program.cs` nor `Person.Server/Program.cs` sets a format, and `NeatooSerializationOptions.Format` defaults to `Ordinal`, so the converter is active both ways; Insert, Update and Delete routing holds.
- **What `partial` changes.** Nothing observable: `PropertyChanged` during deserialization reaches no subscriber, as before; the `IPersonModelAuth` checks and registration, the event preservation and the raised events are unchanged; `Home.razor` uses only `IPersonModel` members and the factory.
- **#150** asked for exactly this.

## Veto-tier / Callouts

None.

## Could not verify

- The published, trimmed WASM client at runtime: no test project, no publish run. Resolved: the claim rests on the generated code, inspected, and the library's own round-trip tests cover that path.

## Theoretical (both taken)

- `using Microsoft.EntityFrameworkCore.Metadata.Conventions;` was unused, from before this change. **Taken:** removed; the build stays green.
- The new comment's "trimmed or not" holds for the Ordinal format; under Named, STJ reflection serves. **Taken:** the comment says "in the default (ordinal) format".
