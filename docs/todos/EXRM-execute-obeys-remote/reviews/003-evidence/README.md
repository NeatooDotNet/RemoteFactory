# EXRM-003 — trimmed-artifact evidence

All runs on `exrm-003-trimming-gate-pair`, Windows, `net9.0`, `win-x64`, self-contained,
`PublishTrimmed=true` / `TrimMode=full`, feature switch `Neatoo.RemoteFactory.IsServerRuntime=false`.
Command per the harness README: `dotnet publish -c Release -r win-x64 --self-contained true`,
then `verify-trimmed.sh` against the **RID** publish directory (`bin/Release/net9.0/win-x64/publish/`) —
the non-RID directory holds a stale build, the trap recorded by the TRIM arc.

| Run | Files | Gate exit | Harness exit | `ok` checks |
|---|---|---|---|---|
| **Baseline** — arc @ `f93f353`, before any EXRM-003 edit | `baseline-publish.log`, `baseline-gate.txt` | 0 | not run | 60 |
| **Known-bad** — both bare targets carrying `[Remote]` | `knownbad-publish.log`, `knownbad-gate.txt`, `knownbad-harness.txt` | **1** | **1** | 60 |
| **Final** — as committed | `003-publish.log`, `003-gate.txt`, `003-harness.txt` | 0 | 0 | 62 |

## The pair, measured

| Shape | `[Remote, Execute]` marker | Bare `[Execute]` marker |
|---|---|---|
| Static factory | `_DoWork`, `_ProcessRecord`, `_DoAsyncWork`, `StaticAsyncBody_MARKER` — **absent** | `BareStaticBody_MARKER` — **present** |
| Class factory | `ClassExecBody_MARKER` — **absent** | `BareClassBody_MARKER` — **present** |

Each pair lives on one class, one generated registrar, one holder, with one `[Service]`
injection style — `[Remote]` is the only variable.

## Red before green

The known-bad run is the falsification required before the two new `check_present`
assertions are trusted. Both went red there (`'BareStaticBody_MARKER' is MISSING`,
`'BareClassBody_MARKER' is MISSING`) while **all 60** incumbent positive controls and
absence checks stayed green in the same run, and the harness independently failed both
invocation checks with `NotSupportedException: The trimming harness never sends HTTP
requests` — the `[Remote]` variant routing to the wire instead of running locally.

The archived pre-EXRM-003 artifact would have been the wrong known-bad build: there the
targets do not exist, so the checks would go red for the reason the gate's positive-control
STOP already halts the run over.

## What the first falsification attempt found

The first known-bad run **passed the gate** (exit 0) while failing the harness. That was
not a property of the variant — it was a defect in the new checks.

`Program.cs` asserted `result.Contains("BareStaticBody_MARKER")`. Program.cs is the entry
point and is never trimmed, so those literals were rooted in the published assembly by the
*checking code itself*, and the gate's `present()` grep found them regardless of what
happened to the `[Execute]` bodies. A check that could not go red — the exact defect this
gate's header was written to prevent, reintroduced in the block meant to retire it.

Fixed by keeping the marker literals in the `[Execute]` bodies and nowhere else: the bodies
stamp their result through `IClientTallyPort`, and the harness matches that stamp
(`|tallied:`) plus the caller's own input token. Re-running the identical variant then
produced the red result above. Both facts are in the file, because "the check was fixed"
and "the check was always sound" are not the same claim.
