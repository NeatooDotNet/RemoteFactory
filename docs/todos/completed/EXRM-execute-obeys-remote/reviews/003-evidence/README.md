# EXRM-003 — trimmed-artifact evidence

All runs on `exrm-003-trimming-gate-pair`, Windows, `net9.0`, `win-x64`, self-contained,
`PublishTrimmed=true` / `TrimMode=full`, feature switch `Neatoo.RemoteFactory.IsServerRuntime=false`.
Command per the harness README: `dotnet publish -c Release -r win-x64 --self-contained true`,
then `verify-trimmed.sh` against the **RID** publish directory (`bin/Release/net9.0/win-x64/publish/`) —
the non-RID directory holds a stale build, the trap recorded by the TRIM arc.

| Run | Files | Gate exit | Harness exit | `ok` checks |
|---|---|---|---|---|
| **Baseline** — arc @ `f93f353`, before any EXRM-003 edit | `baseline-publish.log`, `baseline-gate.txt` | 0 | not run | 60 |
| **Known-bad** — all three bare targets carrying `[Remote]` | `knownbad-publish.log`, `knownbad-gate.txt`, `knownbad-harness.txt` | **1** | **1** | 60 |
| **Final** — as committed | `003-publish.log`, `003-gate.txt`, `003-harness.txt` | 0 | 0 | 63 |

## The three pairs, measured

| Pair | `[Remote, Execute]` marker | Bare `[Execute]` marker |
|---|---|---|
| Static, sync | `_DoWork`, `_ProcessRecord` — **absent** | `BareStaticBody_MARKER` — **present** |
| Static, async | `_DoAsyncWork`, `StaticAsyncBody_MARKER` — **absent** | `BareTallyAsyncBody_MARKER` — **present** |
| Class, async | `ClassExecBody_MARKER` — **absent** | `BareClassBody_MARKER` — **present** |

Each pair is matched on async-ness within itself. That axis is why there are three pairs and not two: TRIM-009 located the class leg's defect in async emission, so measuring only synchronous bare bodies would have generalised across the one boundary this arc has already been burned by.

The async bare marker is `BareTallyAsyncBody_MARKER`, not the obvious `BareStaticAsyncBody_MARKER` — the latter **contains** `StaticAsyncBody_MARKER`, which is `_DoAsyncWork`'s marker and is asserted absent, so under the gate's substring grep it would have satisfied that absence check and reddened its own pair's `[Remote]` half for the wrong reason.

### What the pairs control, and what they do not

Each pair lives on one class, one generated registrar and one holder, with one `[Service]`
injection style, and is matched on **async-ness** — the static pair is synchronous on both
halves, the class pair awaits a port call on both. That axis is deliberate: TRIM-009 located
the class leg's defect in async emission, so a pair varying in it would be uncontrolled
exactly where the risk lives.

One thing is not controlled, and it is stated rather than papered over:

- **The `[Service]` type differs**, of necessity — the `[Remote]` half's port is server-only
  by construction, the bare half's must resolve on the client. The known-bad run neutralises
  it: `IClientTallyPort` stays registered unconditionally there, and adding `[Remote]` alone
  still took all three markers to MISSING. So the bare bodies survive because no guard is
  emitted, not because their port is rooted from DI.

## Red before green

The known-bad run is the falsification required before the three new `check_present`
assertions are trusted. All three went red there (`BareStaticBody_MARKER`,
`BareTallyAsyncBody_MARKER`, `BareClassBody_MARKER` — each `is MISSING`) while **all 60**
incumbent positive controls and absence checks stayed green in the same run, and the harness
independently failed all three invocation checks with `NotSupportedException: The trimming
harness never sends HTTP requests` — the `[Remote]` variant routing to the wire instead of
running locally.

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
