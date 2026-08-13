# RemoteFactory Trimming Verification Tests

Console app that verifies whether the IL trimmer removes server-only types
when `NeatooRuntime.IsServerRuntime` is set to `false` via `RuntimeHostConfigurationOption`,
and that types RemoteFactory must preserve (factories, delegates, event records) survive
`PublishTrimmed=true`.

**This harness is a CI gate.** The process exits non-zero if any check fails; the
`Trimming verification` step in `.github/workflows/build.yml` publishes the trimmed
exe on every push/PR build, asserts the server-only marker strings are absent from
the published assembly, and runs the harness. A `FAILED` line in the output always
comes with a non-zero exit — new checks must follow that contract (append to
`failedChecks` in `Program.cs`; return `bool` from check methods).

## How It Works

1. `TrimTestEntity` has a `[Remote, Create]` method with a `[Service] IServerOnlyRepository` parameter
2. The generated factory's `LocalCreate` method is guarded by `if (!NeatooRuntime.IsServerRuntime) throw ...`
3. The project sets `IsServerRuntime=false` via `RuntimeHostConfigurationOption` with `Trim="true"`
4. When published with `PublishTrimmed=true`, the trimmer constant-folds `IsServerRuntime` to `false`
5. Everything after the throw guard becomes dead code and is removed

## Running the Verification

```bash
# Clean and publish with trimming (net9.0)
dotnet publish -c Release -r win-x64 --self-contained true

# Run the full absence gate — the same script CI runs. It checks every leg, names
# the leg on failure, and carries positive controls so a missing or unreadable
# artifact fails loudly instead of passing silently.
./verify-trimmed.sh bin/Release/net9.0/win-x64/publish/RemoteFactory.TrimmingTests.dll

# NOTE: do not hand-roll `grep -a` checks for the *_MARKER strings. Those are string
# literals, which live UTF-16LE in the #US metadata heap, so a raw-byte grep can never
# match them and reports "absent" for markers that are demonstrably present. Metadata
# NAMES (types, methods) are UTF-8 and do match. verify-trimmed.sh searches both.
#
# This README previously exempted the IServerOnlyRepository interface name from the
# absence check, on the grounds that guarded-dead LocalCreate bodies retained it
# (attributed to TRIM-005). That diagnosis was disproven. The real cause was the
# static-factory registrar attribute naming the consumer's class, which made
# [DynamicallyAccessedMembers] retain every method on it — fixed in TRIM-008. The
# interface name is now measured absent and the exemption is gone.

# Decompile to inspect what the trimmer left
ilspycmd bin/Release/net9.0/win-x64/publish/RemoteFactory.TrimmingTests.dll

# Run the app (should print "Client mode - server-only code trimmed." and
# "All checks passed."; exit code 0 = pass, non-zero = at least one check failed)
./bin/Release/net9.0/win-x64/publish/RemoteFactory.TrimmingTests.exe
echo $?
```

On Linux/CI the RID is `linux-x64` and the binary has no `.exe` suffix.

## Important: TargetFrameworks Override

The `Directory.Build.props` sets `TargetFrameworks` (plural) to `net9.0;net10.0`.
This project explicitly clears that and sets `TargetFramework` (singular) to `net9.0`.
Without clearing `TargetFrameworks`, the ILLink trimmer targets are not properly imported
by the SDK, and trimming silently does not run.

## Results

Verified working on both net9.0 and net10.0. See the exploration plan for detailed findings.
