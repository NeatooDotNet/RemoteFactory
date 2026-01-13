using Neatoo.RemoteFactory;

namespace RemoteOnlyTests.Domain;

/// <summary>
/// Test aggregate root demonstrating client-server separation.
/// Client version has placeholder methods that throw.
/// Server version has real implementations with [Service] parameters.
/// </summary>
[Factory]
public class TestAggregate : ITestAggregate, IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public ITestChildList Children { get; set; } = null!;
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

#if CLIENT
    /// <summary>
    /// Client placeholder - throws if called directly.
    /// </summary>
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Client stub - use ITestAggregateFactory.Create()");
    }

    /// <summary>
    /// Client placeholder for Fetch.
    /// </summary>
    [Remote, Fetch]
    public Task Fetch(Guid id)
    {
        throw new InvalidOperationException("Client stub - use ITestAggregateFactory.Fetch()");
    }

    /// <summary>
    /// Client placeholder for Insert.
    /// </summary>
    [Remote, Insert]
    public Task Insert()
    {
        throw new InvalidOperationException("Client stub - use ITestAggregateFactory.Save()");
    }

    /// <summary>
    /// Client placeholder for Update.
    /// </summary>
    [Remote, Update]
    public Task Update()
    {
        throw new InvalidOperationException("Client stub - use ITestAggregateFactory.Save()");
    }

    /// <summary>
    /// Client placeholder for Delete.
    /// </summary>
    [Remote, Delete]
    public Task Delete()
    {
        throw new InvalidOperationException("Client stub - use ITestAggregateFactory.Save()");
    }
#else
    /// <summary>
    /// Server implementation - creates aggregate with child list.
    /// </summary>
    [Remote, Create]
    public void Create([Service] ITestChildListFactory childListFactory)
    {
        Id = Guid.NewGuid();
        Children = childListFactory.Create();
    }

    /// <summary>
    /// Server implementation - fetches aggregate with children from data store.
    /// </summary>
    [Remote, Fetch]
    public Task Fetch(
        Guid id,
        [Service] ITestDataStore dataStore,
        [Service] ITestChildListFactory childListFactory)
    {
        var data = dataStore.GetAggregate(id);
        Id = data.Id;
        Name = data.Name;
        Children = childListFactory.Fetch(data.ChildData);
        IsNew = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Server implementation - inserts new aggregate.
    /// </summary>
    [Remote, Insert]
    public Task Insert([Service] ITestDataStore dataStore)
    {
        dataStore.Insert(this);
        IsNew = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Server implementation - updates existing aggregate.
    /// </summary>
    [Remote, Update]
    public Task Update([Service] ITestDataStore dataStore)
    {
        dataStore.Update(this);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Server implementation - deletes aggregate.
    /// </summary>
    [Remote, Delete]
    public Task Delete([Service] ITestDataStore dataStore)
    {
        dataStore.Delete(Id);
        return Task.CompletedTask;
    }
#endif
}
