# Fix NeatooFactory.Logical - Remove Incorrect Serialization

## Status: Open

## History

The three-mode enum was introduced in commit `dfe021ab` on May 13, 2025 (version 9.15.0). Before this, there were only two modes: `Local` and `Remote`.

The commit added the `Local` mode (later renamed to `Logical`) with this comment:
```csharp
else if (remoteLocal == NeatooFactory.Local)
{
    // Client Only
    // We still Serialize the objects
    // but we don't need to make a call to the server
    services.AddScoped<IMakeRemoteDelegateRequest, MakeLocalSerializedDelegateRequest>();
}
```

The serialization was included from the very beginning - this was not a regression but an original (incorrect) design decision.

## The Issue

`NeatooFactory.Logical` is performing JSON serialization when it should not.

### Intended Semantics

| Mode | Description | Serialization |
|------|-------------|---------------|
| `NeatooFactory.Server` | Server in 3-tier architecture | Deserializes requests, serializes responses |
| `NeatooFactory.Remote` | Client in 3-tier architecture | Serializes requests, deserializes responses |
| `NeatooFactory.Logical` | Single-tier app, no server | **NO serialization** |

### Current (Wrong) Behavior

`MakeLocalSerializedDelegateRequest` explicitly serializes and deserializes:
```csharp
// Serialize and Deserialize the request so that a different object is returned
var duplicatedRemoteRequestDto = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);
var duplicatedRemoteRequest = this.NeatooJsonSerializer.DeserializeRemoteDelegateRequest(duplicatedRemoteRequestDto);
```

### Why the Original Decision Was Unnecessary

Investigation of the integration test infrastructure (`ClientServerContainers.cs`) reveals that serialization testing does **not** depend on `NeatooFactory.Logical`:

1. The **client container** uses `NeatooFactory.Remote` but **overrides** `IMakeRemoteDelegateRequest` with a custom `MakeSerializedServerStandinDelegateRequest`
2. This custom implementation serializes the request, calls the actual server container, and deserializes the response
3. The **local container** uses `NeatooFactory.Logical` but is intended for non-serialization scenarios

```csharp
// From ClientServerContainers.cs - client container setup
clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, serializationOptions, Assembly.GetExecutingAssembly());
clientCollection.AddScoped<ServerServiceProvider>();
clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();  // Override!
```

The integration tests have their own dedicated mechanism for serialization testing. The serialization in `NeatooFactory.Logical` was never needed.

## Goal

1. **Remove serialization from `NeatooFactory.Logical`** - Logical mode should execute factory methods directly without any JSON serialization overhead

2. **Preserve integration test capabilities** - The existing `MakeSerializedServerStandinDelegateRequest` pattern in test infrastructure handles serialization testing independently

3. **Add tests to verify correct behavior for all three modes**:
   - `NeatooFactory.Server`: Handles incoming serialized requests correctly
   - `NeatooFactory.Remote`: Serializes outgoing requests correctly
   - `NeatooFactory.Logical`: NO serialization occurs

## Files Involved

- `src/RemoteFactory/AddRemoteFactoryServices.cs` - DI registration (needs fix)
- `src/RemoteFactory/Internal/MakeLocalSerializedDelegateRequest.cs` - Current wrong implementation (may be deleted or repurposed)
- `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs` - Has correct pattern for serialization testing
