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
[Factory]
public class TrimTestEntity
{
    public string? Name { get; set; }
    public string? ServerResult { get; set; }

    [Remote]
    [Create]
    internal void Create(string name, [Service] IServerOnlyRepository repo)
    {
        Name = name;
        ServerResult = repo.DoServerWork(name);
    }
}
