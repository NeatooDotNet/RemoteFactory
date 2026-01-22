using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Core;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Core;

/// <summary>
/// Integration tests for static factory methods.
/// Tests static [Create] and [Fetch] methods with service injection and authorization.
/// </summary>
/// <remarks>
/// These tests go through client-server serialization to verify that static factory methods
/// work correctly in a remote execution context.
/// </remarks>
public class StaticFactoryMethodTests
{
    private readonly IServiceScope _clientScope;

    public StaticFactoryMethodTests()
    {
        var scopes = ClientServerContainers.Scopes();
        _clientScope = scopes.client;
    }

    #region Static Create Tests

    [Fact]
    public void StaticCreate_ReturnsObjectCreatedByStaticMethod()
    {
        var factory = _clientScope.GetRequiredService<IStaticCreateTargetFactory>();

        var obj = factory.Create();

        Assert.NotNull(obj);
        Assert.True(obj.UsedStaticMethod);
    }

    #endregion

    #region Static Async Fetch Tests

    [Fact]
    public async Task StaticAsyncFetch_NoParams_ReturnsObjectCreatedByStaticMethod()
    {
        var factory = _clientScope.GetRequiredService<IStaticAsyncFetchTargetFactory>();

        var obj = await factory.Fetch();

        Assert.NotNull(obj);
        Assert.True(obj.UsedStaticMethod);
    }

    [Fact]
    public async Task StaticAsyncFetch_WithParams_PassesParamsAndInjectsService()
    {
        var factory = _clientScope.GetRequiredService<IStaticAsyncFetchWithParamsTargetFactory>();

        var obj = await factory.Fetch(1);

        Assert.NotNull(obj);
        Assert.True(obj.UsedStaticMethod);
    }

    #endregion

    #region Static Fetch with Authorization Tests

    [Fact]
    public async Task StaticFetchWithAuth_AllowedValue_ReturnsObject()
    {
        var factory = _clientScope.GetRequiredService<IStaticFetchWithAuthTargetFactory>();

        var obj = await factory.Fetch(1);

        Assert.NotNull(obj);
        Assert.True(obj.UsedStaticMethod);
    }

    [Fact]
    public async Task StaticFetchWithAuth_AllowedValue10_ReturnsObject()
    {
        var factory = _clientScope.GetRequiredService<IStaticFetchWithAuthTargetFactory>();

        var obj = await factory.Fetch(10);

        Assert.NotNull(obj);
        Assert.True(obj.UsedStaticMethod);
    }

    [Fact]
    public async Task StaticFetchWithAuth_DeniedValue20_ReturnsNull()
    {
        var factory = _clientScope.GetRequiredService<IStaticFetchWithAuthTargetFactory>();

        var obj = await factory.Fetch(20);

        Assert.Null(obj);
    }

    #endregion

    #region Static Fetch with Nullable Return Tests

    [Fact]
    public async Task StaticFetchNullable_AllowedValue_ReturnsObject()
    {
        var factory = _clientScope.GetRequiredService<IStaticFetchNullableTargetFactory>();

        var obj = await factory.Fetch(10);

        Assert.NotNull(obj);
        Assert.True(obj.UsedStaticMethod);
    }

    [Fact]
    public async Task StaticFetchNullable_NullValue_ReturnsNull()
    {
        var factory = _clientScope.GetRequiredService<IStaticFetchNullableTargetFactory>();

        var obj = await factory.Fetch(20);

        Assert.Null(obj);
    }

    #endregion
}
