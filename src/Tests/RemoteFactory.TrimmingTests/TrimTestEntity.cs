using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// Domain entity used to test IL trimming of server-only dependencies.
/// The Create method uses a server-only [Service] parameter.
/// When published with IsServerRuntime=false and PublishTrimmed=true,
/// the LocalCreate method body should be eliminated by the trimmer,
/// and IServerOnlyRepository/ServerOnlyRepository should be absent
/// from the published output.
/// </summary>
/// <summary>
/// DTO types reachable ONLY as properties of <see cref="TrimTestEntity"/> — never
/// in any factory method signature and never constructed in client-reachable code.
/// Their trimming survival depends solely on the entity property-graph discovery
/// (TRIM-002): the generator walks TrimTestEntity's properties and emits
/// Register/PreserveType in the entity's own registrar. EntityPropertyDtoSmokeTest
/// deserializes them from JSON literals to prove that preservation.
/// </summary>
public class TrimEntityCarriedInfo
{
    public string? Text { get; set; }
}

public record TrimEntityCarriedBanner(string Text, string Severity);

[Factory]
public class TrimTestEntity
{
    public string? Name { get; set; }
    public string? ServerResult { get; set; }

    // Reachable only via these properties — see comment above (TRIM-002).
    public TrimEntityCarriedInfo? Info { get; set; }
    public TrimEntityCarriedBanner? Banner { get; set; }

    [Remote]
    [Create]
    internal void Create(string name, [Service] IServerOnlyRepository repo)
    {
        Name = name;
        ServerResult = repo.DoServerWork(name);
    }
}
