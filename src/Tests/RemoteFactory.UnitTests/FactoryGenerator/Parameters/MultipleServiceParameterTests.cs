using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Parameters;

namespace RemoteFactory.UnitTests.FactoryGenerator.Parameters;

/// <summary>
/// Unit tests for domain classes with multiple [Service] parameters (2+).
/// Tests verify all services are resolved correctly on server side.
/// </summary>
public class MultipleServiceParameterTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public MultipleServiceParameterTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .WithService<IService2, Service2>()
            .WithService<IService3, Service3>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region Create Tests - Two Services

    [Fact]
    public void CreateWithTwoServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceReadTargetFactory>();

        var result = factory.CreateWithTwoServices();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Service2Value: 42", result.ServiceInfo);
    }

    [Fact]
    public void CreateWithThreeServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceReadTargetFactory>();

        var result = factory.CreateWithThreeServices();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    [Fact]
    public void CreateWithParamsAndTwoServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceReadTargetFactory>();

        var result = factory.CreateWithParamsAndTwoServices(42, "TestName");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Id: 42, Name: TestName, Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task CreateWithTwoServicesAsync_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceReadTargetFactory>();

        var result = await factory.CreateWithTwoServicesAsync();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Async Service2Value: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task CreateWithTwoServicesBoolAsync_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceReadTargetFactory>();

        var result = await factory.CreateWithTwoServicesBoolAsync();

        Assert.NotNull(result);
        Assert.True(result!.CreateCalled);
        Assert.Equal("Async Bool Service2Value: 42", result.ServiceInfo);
    }

    #endregion

    #region Fetch Tests

    [Fact]
    public void FetchWithTwoServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceReadTargetFactory>();

        var result = factory.FetchWithTwoServices();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch Service2Value: 42", result.ServiceInfo);
    }

    [Fact]
    public void FetchWithThreeServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceReadTargetFactory>();

        var result = factory.FetchWithThreeServices();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    [Fact]
    public void FetchWithParamsAndTwoServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceReadTargetFactory>();
        var id = Guid.NewGuid();

        var result = factory.FetchWithParamsAndTwoServices(id);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal($"FetchId: {id}, Service2: 42", result.ServiceInfo);
    }

    #endregion

    #region Write Tests - Insert

    [Fact]
    public void SaveInsertWithTwoServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceWriteTargetFactory>();
        var obj = new MultiServiceWriteTarget { IsNew = true };

        var result = factory.SaveWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Insert Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public void SaveInsertWithThreeServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceWriteTargetFactory>();
        var obj = new MultiServiceWriteTarget { IsNew = true };

        var result = factory.SaveWithThreeServices(obj);

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Insert Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    [Fact]
    public async Task SaveInsertWithTwoServicesAsync_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceWriteTargetFactory>();
        var obj = new MultiServiceWriteTarget { IsNew = true };

        var result = await factory.SaveWithTwoServicesAsync(obj);

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Async Insert Service2: 42", result.ServiceInfo);
    }

    #endregion

    #region Write Tests - Update

    [Fact]
    public void SaveUpdateWithTwoServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceWriteTargetFactory>();
        var obj = new MultiServiceWriteTarget { IsNew = false, IsDeleted = false };

        var result = factory.SaveWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result!.UpdateCalled);
        Assert.Equal("Update Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task SaveUpdateWithTwoServicesBoolAsync_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceWriteTargetFactory>();
        var obj = new MultiServiceWriteTarget { IsNew = false, IsDeleted = false };

        var result = await factory.SaveWithTwoServicesBoolAsync(obj);

        Assert.NotNull(result);
        Assert.True(result!.UpdateCalled);
        Assert.Equal("Async Bool Update Service2: 42", result.ServiceInfo);
    }

    #endregion

    #region Write Tests - Delete

    [Fact]
    public void SaveDeleteWithTwoServices_Works()
    {
        var factory = _provider.GetRequiredService<IMultiServiceWriteTargetFactory>();
        var obj = new MultiServiceWriteTarget { IsDeleted = true };

        var result = factory.SaveWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result!.DeleteCalled);
        Assert.Equal("Delete Service2: 42", result.ServiceInfo);
    }

    #endregion
}
