using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.BugScenarios;

#region Test Target Classes

/// <summary>
/// Factory methods can return whatever they want.
/// If it's not bool, the return value is ignored.
/// This is useful for unit testing purposes.
/// </summary>
[Factory]
public partial class IgnoreMethodReturnObj : IFactorySaveMeta
{
    [Create]
    public IgnoreMethodReturnObj()
    {
    }

    public bool InsertCalled { get; private set; }
    public bool IsDeleted => false;
    public bool IsNew => true;

    /// <summary>
    /// Returns int - value should be ignored by the factory.
    /// </summary>
    [Insert]
    public int Insert()
    {
        InsertCalled = true;
        return 1;
    }

    /// <summary>
    /// Returns string - value should be ignored by the factory.
    /// </summary>
    [Update]
    public string? Update()
    {
        return string.Empty;
    }
}

#endregion

/// <summary>
/// Tests that write methods can return non-bool types and the return value is ignored.
/// </summary>
public class IgnoreWriteMethodReturnTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IIgnoreMethodReturnObjFactory _factory;

    public IgnoreWriteMethodReturnTests()
    {
        _provider = new ServerContainerBuilder().Build();
        _factory = _provider.GetRequiredService<IIgnoreMethodReturnObjFactory>();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    public void Save_IgnoresNonBoolReturnValue()
    {
        var obj = _factory.Create();

        Assert.False(obj.InsertCalled);

        obj = _factory.Save(obj);

        Assert.True(obj.InsertCalled);
    }
}
