# DICT-001 red-first evidence

Recorded 2026-10-05, local machine, .NET SDK 10.0.204, harness target net9.0.

## What ran

The tree at commit `effedce` — arc commit `5457120` plus the four DICT-001 harness changes.
**The generator was not modified.** Both checks therefore ran against the walk as it stood
before DICT-002.

| Run | Command | Exit | Result |
|---|---|---|---|
| Untrimmed harness | `dotnet build -c Release`, then `bin/Release/net9.0/RemoteFactory.TrimmingTests.exe` | 0 | Both new checks PASSED; all checks passed |
| Trimmed gate | `dotnet publish -c Release -r win-x64 --self-contained true`, then `verify-trimmed.sh` on the published DLL | 0 | Every assertion passed; new control `NeatooClassFactoryRegistrar_TrimDictCarrier` present |
| Trimmed harness | the published `RemoteFactory.TrimmingTests.exe` | **1** | **Exactly the two new checks FAILED**; every pre-existing check passed |

## The two failures

```
Dictionary-value DTO smoke FAILED: deserialization threw NotSupportedException: Deserialization of types
without a parameterless constructor, a singular parameterized constructor, or a parameterized constructor
annotated with 'JsonConstructorAttribute' is not supported. Type 'RemoteFactory.TrimmingTests.TrimDictValue'.
Path: $.l-f-1 | LineNumber: 0 | BytePositionInLine: 19.

Public-field DTO smoke FAILED: deserialization threw NotSupportedException: Deserialization of types
without a parameterless constructor, a singular parameterized constructor, or a parameterized constructor
annotated with 'JsonConstructorAttribute' is not supported. Type 'RemoteFactory.TrimmingTests.TrimFieldCarried'.
Path: $.Carried | LineNumber: 0 | BytePositionInLine: 19.
```

The dictionary failure is the same exception, at the same path, that zTreatment's published client
raised for `LocationAssessment`. zTreatment's appeared in resource-key form
(`DeserializeNoConstructor, JsonConstructorAttribute, …`) because Blazor WebAssembly sets
`UseSystemResourceKeys`; this console harness does not.

## Why the red is caused by the walk, not by the check

- **Untrimmed GREEN, trimmed RED, same tree.** The JSON literals and assertions are correct;
  only trimming differs.
- **The generator registers neither type.** `generated-registrar-excerpt.txt` shows the carrier's
  registrar emitting `Register<TrimFieldHost>` only. `TrimDictValue` (dictionary value) and
  `TrimFieldCarried` (public-field type) are absent, so nothing roots their constructors.
- **Nothing else roots them.** Neither type is constructed in client-reachable code. The dictionary
  value's only `new` is in the async `[Remote]` body, which the existing gate measures absent on
  this leg (`ClassAsyncBody_MARKER`, TRIM-009). `INeatooJsonSerializer.Deserialize<T>` carries no
  trimmer annotation and no `new()` constraint.
- **The field host itself constructed.** The public-field failure names `TrimFieldCarried` at
  `$.Carried`, not `TrimFieldHost` at the root: the walk did register the host, and the miss is
  isolated to the field's type.

## Files

| File | Content |
|---|---|
| `untrimmed-run.txt` | Full untrimmed harness output |
| `trimmed-gate.txt` | Full `verify-trimmed.sh` output against the trimmed DLL |
| `trimmed-run.txt` | Full trimmed harness output |
| `generated-registrar-excerpt.txt` | The carrier's generated registrar, `DtoConstructorRegistry` lines |

The build and publish logs are `*.log` and gitignored per the TRIM log-evidence ruling.
