using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Parameters;

#region Remote Read Operations with Multiple Services

/// <summary>
/// Test target with remote operations using multiple [Service] parameters.
/// Services should be resolved on the server side for remote execution.
/// </summary>
[Factory]
public partial class MultiServiceRemoteTarget
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? ServiceInfo { get; set; }

    [Create]
    public MultiServiceRemoteTarget() { }

    /// <summary>
    /// Remote Create with two services - services resolved on server.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        CreateCalled = true;
        ServiceInfo = $"Remote Create Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Remote Create with three services.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        ArgumentNullException.ThrowIfNull(service3);
        CreateCalled = true;
        ServiceInfo = $"Remote Create Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    /// <summary>
    /// Remote Create with business params and multiple services.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithParamsAndServices(
        int id,
        string name,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        CreateCalled = true;
        ServiceInfo = $"Remote Create Id: {id}, Name: {name}, Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Remote Fetch with two services.
    /// </summary>
    [Fetch]
    [Remote]
    public void FetchRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        FetchCalled = true;
        ServiceInfo = $"Remote Fetch Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Remote async Create with multiple services.
    /// </summary>
    [Create]
    [Remote]
    public Task CreateRemoteWithTwoServicesAsync([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        CreateCalled = true;
        ServiceInfo = $"Remote Async Create Service2: {service2.GetValue()}";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Remote async Create with Task&lt;bool&gt;.
    /// </summary>
    [Create]
    [Remote]
    public Task<bool> CreateRemoteWithTwoServicesBoolAsync([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        CreateCalled = true;
        ServiceInfo = $"Remote Async Bool Create Service2: {service2.GetValue()}";
        return Task.FromResult(true);
    }
}

#endregion

#region Remote Write Operations with Multiple Services

/// <summary>
/// Test target with remote write operations using multiple [Service] parameters.
/// </summary>
[Factory]
public partial class MultiServiceRemoteWriteTarget : IFactorySaveMeta
{
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public string? ServiceInfo { get; set; }

    [Create]
    public MultiServiceRemoteWriteTarget() { }

    /// <summary>
    /// Remote Insert with multiple services.
    /// </summary>
    [Insert]
    [Remote]
    public void InsertRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        InsertCalled = true;
        ServiceInfo = $"Remote Insert Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Remote Update with multiple services.
    /// </summary>
    [Update]
    [Remote]
    public void UpdateRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        UpdateCalled = true;
        ServiceInfo = $"Remote Update Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Remote Delete with multiple services.
    /// </summary>
    [Delete]
    [Remote]
    public void DeleteRemoteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        DeleteCalled = true;
        ServiceInfo = $"Remote Delete Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Remote Insert with three services and business params.
    /// </summary>
    [Insert]
    [Remote]
    public Task InsertRemoteWithThreeServicesAsync(
        int priority,
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        ArgumentNullException.ThrowIfNull(service3);
        InsertCalled = true;
        ServiceInfo = $"Remote Insert Priority: {priority}, Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
        return Task.CompletedTask;
    }
}

#endregion
