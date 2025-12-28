using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for domain classes with multiple [Service] parameters (2+).
/// This addresses GAP-003 from the test plan: Multiple Service Parameters - completely untested.
///
/// Tests verify:
/// - Create/Fetch operations with multiple [Service] parameters
/// - Insert/Update/Delete operations with multiple [Service] parameters
/// - Remote operations with multiple [Service] parameters
/// - All services are resolved correctly on server side
/// - Parameter ordering is preserved (business params first, then services)
/// </summary>

#region Domain Classes

/// <summary>
/// Domain class with methods that use multiple [Service] parameters for Create/Fetch operations.
/// </summary>
[Factory]
public class MultiServiceReadObject
{
    // Tracking properties
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? ServiceInfo { get; set; }

    // Create with two services
    [Create]
    public void CreateWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.CreateCalled = true;
        this.ServiceInfo = $"Service2Value: {service2.GetValue()}";
    }

    // Create with three services
    [Create]
    public void CreateWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        this.CreateCalled = true;
        this.ServiceInfo = $"Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    // Create with business params and two services
    [Create]
    public void CreateWithParamsAndTwoServices(
        int id,
        string name,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Equal(42, id);
        Assert.Equal("TestName", name);
        this.CreateCalled = true;
        this.ServiceInfo = $"Id: {id}, Name: {name}, Service2: {service2.GetValue()}";
    }

    // Async Create with two services
    [Create]
    public Task CreateWithTwoServicesAsync([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.CreateCalled = true;
        this.ServiceInfo = $"Async Service2Value: {service2.GetValue()}";
        return Task.CompletedTask;
    }

    // Async Create with Task<bool> and two services
    [Create]
    public Task<bool> CreateWithTwoServicesBoolAsync([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.CreateCalled = true;
        this.ServiceInfo = $"Async Bool Service2Value: {service2.GetValue()}";
        return Task.FromResult(true);
    }

    // Fetch with two services
    [Fetch]
    public void FetchWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.FetchCalled = true;
        this.ServiceInfo = $"Fetch Service2Value: {service2.GetValue()}";
    }

    // Fetch with three services
    [Fetch]
    public void FetchWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        this.FetchCalled = true;
        this.ServiceInfo = $"Fetch Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    // Fetch with business params and two services
    [Fetch]
    public void FetchWithParamsAndTwoServices(
        Guid id,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.FetchCalled = true;
        this.ServiceInfo = $"FetchId: {id}, Service2: {service2.GetValue()}";
    }
}

/// <summary>
/// Domain class with methods that use multiple [Service] parameters for Insert/Update/Delete operations.
/// </summary>
[Factory]
public class MultiServiceWriteObject : IFactorySaveMeta
{
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    // Tracking properties
    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public string? ServiceInfo { get; set; }

    // Insert with two services
    [Insert]
    public void InsertWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.InsertCalled = true;
        this.ServiceInfo = $"Insert Service2: {service2.GetValue()}";
    }

    // Insert with three services
    [Insert]
    public void InsertWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        this.InsertCalled = true;
        this.ServiceInfo = $"Insert Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    // Update with two services
    [Update]
    public void UpdateWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.UpdateCalled = true;
        this.ServiceInfo = $"Update Service2: {service2.GetValue()}";
    }

    // Update with three services
    [Update]
    public void UpdateWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        this.UpdateCalled = true;
        this.ServiceInfo = $"Update Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    // Delete with two services
    [Delete]
    public void DeleteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.DeleteCalled = true;
        this.ServiceInfo = $"Delete Service2: {service2.GetValue()}";
    }

    // Delete with three services
    [Delete]
    public void DeleteWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        this.DeleteCalled = true;
        this.ServiceInfo = $"Delete Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    // Async Insert with two services
    [Insert]
    public Task InsertWithTwoServicesAsync([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.InsertCalled = true;
        this.ServiceInfo = $"Async Insert Service2: {service2.GetValue()}";
        return Task.CompletedTask;
    }

    // Async Update with two services returning Task<bool>
    [Update]
    public Task<bool> UpdateWithTwoServicesBoolAsync([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.UpdateCalled = true;
        this.ServiceInfo = $"Async Bool Update Service2: {service2.GetValue()}";
        return Task.FromResult(true);
    }
}

/// <summary>
/// Domain class with remote operations using multiple [Service] parameters.
/// Services should be resolved on the server side for remote execution.
/// </summary>
[Factory]
public class MultiServiceRemoteObject
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? ServiceInfo { get; set; }

    // Remote Create with two services - services resolved on server
    [Create]
    [Remote]
    public void CreateRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.CreateCalled = true;
        this.ServiceInfo = $"Remote Create Service2: {service2.GetValue()}";
    }

    // Remote Create with three services
    [Create]
    [Remote]
    public void CreateRemoteWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        this.CreateCalled = true;
        this.ServiceInfo = $"Remote Create Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    // Remote Create with business params and multiple services
    [Create]
    [Remote]
    public void CreateRemoteWithParamsAndServices(
        int id,
        string name,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Equal(100, id);
        Assert.Equal("RemoteTest", name);
        this.CreateCalled = true;
        this.ServiceInfo = $"Remote Create Id: {id}, Name: {name}, Service2: {service2.GetValue()}";
    }

    // Remote Fetch with two services
    [Fetch]
    [Remote]
    public void FetchRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.FetchCalled = true;
        this.ServiceInfo = $"Remote Fetch Service2: {service2.GetValue()}";
    }

    // Remote async Create with multiple services
    [Create]
    [Remote]
    public Task CreateRemoteWithTwoServicesAsync([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.CreateCalled = true;
        this.ServiceInfo = $"Remote Async Create Service2: {service2.GetValue()}";
        return Task.CompletedTask;
    }

    // Remote async Create with Task<bool>
    [Create]
    [Remote]
    public Task<bool> CreateRemoteWithTwoServicesBoolAsync([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.CreateCalled = true;
        this.ServiceInfo = $"Remote Async Bool Create Service2: {service2.GetValue()}";
        return Task.FromResult(true);
    }
}

/// <summary>
/// Domain class with remote write operations using multiple [Service] parameters.
/// </summary>
[Factory]
public class MultiServiceRemoteWriteObject : IFactorySaveMeta
{
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public string? ServiceInfo { get; set; }

    // Remote Insert with multiple services
    [Insert]
    [Remote]
    public void InsertRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.InsertCalled = true;
        this.ServiceInfo = $"Remote Insert Service2: {service2.GetValue()}";
    }

    // Remote Update with multiple services
    [Update]
    [Remote]
    public void UpdateRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.UpdateCalled = true;
        this.ServiceInfo = $"Remote Update Service2: {service2.GetValue()}";
    }

    // Remote Delete with multiple services
    [Delete]
    [Remote]
    public void DeleteRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        this.DeleteCalled = true;
        this.ServiceInfo = $"Remote Delete Service2: {service2.GetValue()}";
    }

    // Remote Insert with three services and business params
    [Insert]
    [Remote]
    public Task InsertRemoteWithThreeServicesAsync(
        int priority,
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotNull(service3);
        Assert.Equal(5, priority);
        this.InsertCalled = true;
        this.ServiceInfo = $"Remote Insert Priority: {priority}, Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
        return Task.CompletedTask;
    }
}

#endregion

#region Test Class

public class MultipleServiceParameterTests
{
    private readonly IServiceScope clientScope;
    private readonly IServiceScope localScope;

    public MultipleServiceParameterTests()
    {
        var scopes = ClientServerContainers.Scopes();
        this.clientScope = scopes.client;
        this.localScope = scopes.local;
    }

    #region Local Read Tests - Two Services

    [Fact]
    public void MultiServiceRead_CreateWithTwoServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();

        var result = factory.CreateWithTwoServices();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Service2Value: 42", result.ServiceInfo);
    }

    [Fact]
    public void MultiServiceRead_CreateWithThreeServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();

        var result = factory.CreateWithThreeServices();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    [Fact]
    public void MultiServiceRead_CreateWithParamsAndTwoServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();

        var result = factory.CreateWithParamsAndTwoServices(42, "TestName");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Id: 42, Name: TestName, Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRead_CreateWithTwoServicesAsync_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();

        var result = await factory.CreateWithTwoServicesAsync();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Async Service2Value: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRead_CreateWithTwoServicesBoolAsync_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();

        var result = await factory.CreateWithTwoServicesBoolAsync();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Async Bool Service2Value: 42", result.ServiceInfo);
    }

    #endregion

    #region Local Read Tests - Fetch

    [Fact]
    public void MultiServiceRead_FetchWithTwoServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();

        var result = factory.FetchWithTwoServices();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch Service2Value: 42", result.ServiceInfo);
    }

    [Fact]
    public void MultiServiceRead_FetchWithThreeServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();

        var result = factory.FetchWithThreeServices();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    [Fact]
    public void MultiServiceRead_FetchWithParamsAndTwoServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();
        var id = Guid.NewGuid();

        var result = factory.FetchWithParamsAndTwoServices(id);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal($"FetchId: {id}, Service2: 42", result.ServiceInfo);
    }

    #endregion

    #region Local Write Tests

    [Fact]
    public void MultiServiceWrite_SaveInsertWithTwoServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceWriteObjectFactory>();
        var obj = new MultiServiceWriteObject { IsNew = true };

        var result = factory.SaveWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Insert Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public void MultiServiceWrite_SaveInsertWithThreeServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceWriteObjectFactory>();
        var obj = new MultiServiceWriteObject { IsNew = true };

        var result = factory.SaveWithThreeServices(obj);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Insert Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    [Fact]
    public void MultiServiceWrite_SaveUpdateWithTwoServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceWriteObjectFactory>();
        var obj = new MultiServiceWriteObject { IsNew = false, IsDeleted = false };

        var result = factory.SaveWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal("Update Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public void MultiServiceWrite_SaveDeleteWithTwoServices_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceWriteObjectFactory>();
        var obj = new MultiServiceWriteObject { IsDeleted = true };

        var result = factory.SaveWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal("Delete Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceWrite_SaveInsertWithTwoServicesAsync_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceWriteObjectFactory>();
        var obj = new MultiServiceWriteObject { IsNew = true };

        var result = await factory.SaveWithTwoServicesAsync(obj);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Async Insert Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceWrite_SaveUpdateWithTwoServicesBoolAsync_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceWriteObjectFactory>();
        var obj = new MultiServiceWriteObject { IsNew = false, IsDeleted = false };

        var result = await factory.SaveWithTwoServicesBoolAsync(obj);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal("Async Bool Update Service2: 42", result.ServiceInfo);
    }

    #endregion

    #region Remote Read Tests

    [Fact]
    public async Task MultiServiceRemote_CreateWithTwoServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithTwoServices();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Create Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRemote_CreateWithThreeServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithThreeServices();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Create Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRemote_CreateWithParamsAndServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithParamsAndServices(100, "RemoteTest");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Create Id: 100, Name: RemoteTest, Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRemote_FetchWithTwoServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteObjectFactory>();

        var result = await factory.FetchRemoteWithTwoServices();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Remote Fetch Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRemote_CreateAsyncWithTwoServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithTwoServicesAsync();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Async Create Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRemote_CreateAsyncBoolWithTwoServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithTwoServicesBoolAsync();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Async Bool Create Service2: 42", result.ServiceInfo);
    }

    #endregion

    #region Remote Write Tests

    [Fact]
    public async Task MultiServiceRemoteWrite_InsertWithTwoServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteWriteObjectFactory>();
        var obj = new MultiServiceRemoteWriteObject { IsNew = true };

        var result = await factory.SaveRemoteWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Remote Insert Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRemoteWrite_UpdateWithTwoServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteWriteObjectFactory>();
        var obj = new MultiServiceRemoteWriteObject { IsNew = false, IsDeleted = false };

        var result = await factory.SaveRemoteWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal("Remote Update Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRemoteWrite_DeleteWithTwoServices_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteWriteObjectFactory>();
        var obj = new MultiServiceRemoteWriteObject { IsDeleted = true };

        var result = await factory.SaveRemoteWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal("Remote Delete Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task MultiServiceRemoteWrite_InsertWithThreeServicesAsync_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteWriteObjectFactory>();
        var obj = new MultiServiceRemoteWriteObject { IsNew = true };

        var result = await factory.SaveRemoteWithThreeServicesAsync(obj, 5);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Remote Insert Priority: 5, Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    #endregion

    #region Client vs Local Comparison Tests

    [Fact]
    public void MultiServiceRead_CreateWithTwoServices_ClientAndLocalBehaveSame()
    {
        var clientFactory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();
        var localFactory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceReadObjectFactory>();

        var clientResult = clientFactory.CreateWithTwoServices();
        var localResult = localFactory.CreateWithTwoServices();

        Assert.Equal(clientResult.ServiceInfo, localResult.ServiceInfo);
        Assert.True(clientResult.CreateCalled);
        Assert.True(localResult.CreateCalled);
    }

    [Fact]
    public void MultiServiceWrite_SaveInsertWithTwoServices_ClientAndLocalBehaveSame()
    {
        var clientFactory = this.clientScope.ServiceProvider.GetRequiredService<IMultiServiceWriteObjectFactory>();
        var localFactory = this.localScope.ServiceProvider.GetRequiredService<IMultiServiceWriteObjectFactory>();

        var clientObj = new MultiServiceWriteObject { IsNew = true };
        var localObj = new MultiServiceWriteObject { IsNew = true };

        var clientResult = clientFactory.SaveWithTwoServices(clientObj);
        var localResult = localFactory.SaveWithTwoServices(localObj);

        Assert.Equal(clientResult!.ServiceInfo, localResult!.ServiceInfo);
        Assert.True(clientResult.InsertCalled);
        Assert.True(localResult.InsertCalled);
    }

    #endregion
}

#endregion
