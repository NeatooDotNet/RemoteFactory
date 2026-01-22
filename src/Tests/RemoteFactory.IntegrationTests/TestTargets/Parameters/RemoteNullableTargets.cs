using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Parameters;

/// <summary>
/// Test target for nullable parameter handling in remote operations.
/// </summary>
[Factory]
public partial class RemoteNullableTarget
{
    public bool CreateCalled { get; set; }
    public int? ReceivedValue { get; set; }

    [Create]
    public RemoteNullableTarget() { }

    [Remote]
    [Create]
    public void CreateRemote(int? p)
    {
        CreateCalled = true;
        ReceivedValue = p;
    }
}
