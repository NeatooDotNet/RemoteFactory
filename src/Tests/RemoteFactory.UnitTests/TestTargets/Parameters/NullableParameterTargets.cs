using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.TestTargets.Parameters;

/// <summary>
/// Test target for nullable parameter handling.
/// </summary>
[Factory]
public partial class NullableParameterTarget
{
    public bool CreateCalled { get; set; }

    [Create]
    public void Create(int? p)
    {
        CreateCalled = true;
    }
}
