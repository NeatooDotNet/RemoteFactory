using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.BugScenarios;

#region Test Target Classes

/// <summary>
/// When there is no Delete method, the Save return type doesn't need to be nullable.
/// </summary>
[Factory]
public partial class SaveNotNullableNoDeleteObj : IFactorySaveMeta
{
    [Create]
    public SaveNotNullableNoDeleteObj()
    {
    }

    public bool InsertCalled { get; private set; }
    public bool IsDeleted => false;
    public bool IsNew => true;

    [Insert]
    public void Insert()
    {
        InsertCalled = true;
    }

    [Update]
    public void Update()
    {
    }
}

#endregion

/// <summary>
/// Tests that Save is not nullable when there is no Delete method.
/// Delete is the only operation that can return null (when bool returns false).
/// </summary>
public class SaveNotNullableNoDeleteTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly ISaveNotNullableNoDeleteObjFactory _factory;

    public SaveNotNullableNoDeleteTests()
    {
        _provider = new ServerContainerBuilder().Build();
        _factory = _provider.GetRequiredService<ISaveNotNullableNoDeleteObjFactory>();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    public void Save_IsNotNullable_WhenNoDeleteMethod()
    {
        var obj = _factory.Create();

        Assert.False(obj.InsertCalled);

        // Save returns non-nullable SaveNotNullableNoDeleteObj (not SaveNotNullableNoDeleteObj?)
        obj = _factory.Save(obj);

        Assert.True(obj.InsertCalled);
    }
}
