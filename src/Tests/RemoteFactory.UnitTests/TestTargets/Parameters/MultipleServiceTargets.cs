using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Parameters;

#region Read Operations with Multiple Services

/// <summary>
/// Test target with methods that use multiple [Service] parameters for Create/Fetch operations.
/// </summary>
[Factory]
public partial class MultiServiceReadTarget
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? ServiceInfo { get; set; }

    /// <summary>
    /// Create with two services.
    /// </summary>
    [Create]
    public void CreateWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        CreateCalled = true;
        ServiceInfo = $"Service2Value: {service2.GetValue()}";
    }

    /// <summary>
    /// Create with three services.
    /// </summary>
    [Create]
    public void CreateWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        ArgumentNullException.ThrowIfNull(service3);
        CreateCalled = true;
        ServiceInfo = $"Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    /// <summary>
    /// Create with business params and two services.
    /// </summary>
    [Create]
    public void CreateWithParamsAndTwoServices(
        int id,
        string name,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        CreateCalled = true;
        ServiceInfo = $"Id: {id}, Name: {name}, Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Async Create with two services.
    /// </summary>
    [Create]
    public Task CreateWithTwoServicesAsync([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        CreateCalled = true;
        ServiceInfo = $"Async Service2Value: {service2.GetValue()}";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Async Create with Task&lt;bool&gt; and two services.
    /// </summary>
    [Create]
    public Task<bool> CreateWithTwoServicesBoolAsync([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        CreateCalled = true;
        ServiceInfo = $"Async Bool Service2Value: {service2.GetValue()}";
        return Task.FromResult(true);
    }

    /// <summary>
    /// Fetch with two services.
    /// </summary>
    [Fetch]
    public void FetchWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        FetchCalled = true;
        ServiceInfo = $"Fetch Service2Value: {service2.GetValue()}";
    }

    /// <summary>
    /// Fetch with three services.
    /// </summary>
    [Fetch]
    public void FetchWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        ArgumentNullException.ThrowIfNull(service3);
        FetchCalled = true;
        ServiceInfo = $"Fetch Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    /// <summary>
    /// Fetch with business params and two services.
    /// </summary>
    [Fetch]
    public void FetchWithParamsAndTwoServices(
        Guid id,
        [Service] IService service1,
        [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        FetchCalled = true;
        ServiceInfo = $"FetchId: {id}, Service2: {service2.GetValue()}";
    }
}

#endregion

#region Write Operations with Multiple Services

/// <summary>
/// Test target with methods that use multiple [Service] parameters for Insert/Update/Delete operations.
/// </summary>
[Factory]
public partial class MultiServiceWriteTarget : IFactorySaveMeta
{
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public string? ServiceInfo { get; set; }

    /// <summary>
    /// Insert with two services.
    /// </summary>
    [Insert]
    public void InsertWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        InsertCalled = true;
        ServiceInfo = $"Insert Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Insert with three services.
    /// </summary>
    [Insert]
    public void InsertWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        ArgumentNullException.ThrowIfNull(service3);
        InsertCalled = true;
        ServiceInfo = $"Insert Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    /// <summary>
    /// Update with two services.
    /// </summary>
    [Update]
    public void UpdateWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        UpdateCalled = true;
        ServiceInfo = $"Update Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Update with three services.
    /// </summary>
    [Update]
    public void UpdateWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        ArgumentNullException.ThrowIfNull(service3);
        UpdateCalled = true;
        ServiceInfo = $"Update Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    /// <summary>
    /// Delete with two services.
    /// </summary>
    [Delete]
    public void DeleteWithTwoServices([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        DeleteCalled = true;
        ServiceInfo = $"Delete Service2: {service2.GetValue()}";
    }

    /// <summary>
    /// Delete with three services.
    /// </summary>
    [Delete]
    public void DeleteWithThreeServices(
        [Service] IService service1,
        [Service] IService2 service2,
        [Service] IService3 service3)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        ArgumentNullException.ThrowIfNull(service3);
        DeleteCalled = true;
        ServiceInfo = $"Delete Service2: {service2.GetValue()}, Service3: {service3.GetName()}";
    }

    /// <summary>
    /// Async Insert with two services.
    /// </summary>
    [Insert]
    public Task InsertWithTwoServicesAsync([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        InsertCalled = true;
        ServiceInfo = $"Async Insert Service2: {service2.GetValue()}";
        return Task.CompletedTask;
    }

    /// <summary>
    /// Async Update with two services returning Task&lt;bool&gt;.
    /// </summary>
    [Update]
    public Task<bool> UpdateWithTwoServicesBoolAsync([Service] IService service1, [Service] IService2 service2)
    {
        ArgumentNullException.ThrowIfNull(service1);
        ArgumentNullException.ThrowIfNull(service2);
        UpdateCalled = true;
        ServiceInfo = $"Async Bool Update Service2: {service2.GetValue()}";
        return Task.FromResult(true);
    }
}

#endregion
