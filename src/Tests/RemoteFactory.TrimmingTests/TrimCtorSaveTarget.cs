using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// A public-setter <see cref="IFactorySaveMeta"/> class that gets NO generated ordinal
/// converter, because its only constructor takes a service. The ordinal converter is
/// what roots a factory class's property accessors on a trimmed client; without it,
/// nothing on the client reads <c>IsNew</c> or <c>IsDeleted</c>, and before the
/// generator emitted <c>PreserveProperties</c> for this shape the trimmer removed
/// their accessors. The flags then serialized as <c>{}</c>, and every Save routed to
/// Insert on the server.
/// </summary>
/// <remarks>
/// Nothing in the harness may read or write <c>IsNew</c>/<c>IsDeleted</c> on this
/// type: Program.cs and the smoke tests are never trimmed, so any such access roots
/// the accessor and hides the failure. <see cref="SaveMetaRoundTripSmokeTest"/>
/// touches them only through the serializer.
/// </remarks>
[Factory]
public partial class TrimCtorSaveTarget : IFactorySaveMeta
{
    private readonly IClientTallyPort _port;

    public TrimCtorSaveTarget(IClientTallyPort port)
    {
        _port = port;
    }

    public bool IsNew { get; set; } = true;

    public bool IsDeleted { get; set; }

    [Create]
    public void Create()
    {
    }

    [Remote]
    [Insert]
    internal Task Insert()
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote]
    [Update]
    internal Task Update() => Task.CompletedTask;

    [Remote]
    [Delete]
    internal Task Delete() => Task.CompletedTask;

    public string Tally(string input) => _port.ClientTallyCompute(input);
}
