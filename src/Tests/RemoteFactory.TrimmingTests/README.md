# RemoteFactory Trimming Verification Tests

Standalone console app that verifies whether the IL trimmer removes server-only types
when `NeatooRuntime.IsServerRuntime` is set to `false` via `RuntimeHostConfigurationOption`.

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

# Search for server-only types in output (should return nothing)
grep -aob "ServerOnly" bin/Release/net9.0/win-x64/publish/RemoteFactory.TrimmingTests.dll

# Decompile to inspect what the trimmer left
ilspycmd bin/Release/net9.0/win-x64/publish/RemoteFactory.TrimmingTests.dll

# Run the app (should print "Client mode - server-only code trimmed.")
./bin/Release/net9.0/win-x64/publish/RemoteFactory.TrimmingTests.exe
```

## Important: TargetFrameworks Override

The `Directory.Build.props` sets `TargetFrameworks` (plural) to `net9.0;net10.0`.
This project explicitly clears that and sets `TargetFramework` (singular) to `net9.0`.
Without clearing `TargetFrameworks`, the ILLink trimmer targets are not properly imported
by the SDK, and trimming silently does not run.

## Results

Verified working on both net9.0 and net10.0. See the exploration plan for detailed findings.
