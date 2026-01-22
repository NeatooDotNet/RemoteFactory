using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

/// <summary>
/// Test domain objects for aggregate serialization round-trip tests.
/// </summary>

/// <summary>
/// Child item in an aggregate - supports local Create and server-side Fetch.
/// </summary>
[Factory]
public class AggregateChild
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal Value { get; set; }
    public bool FetchWasCalled { get; set; }

    [Create]
    public AggregateChild()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]
    public void Fetch(Guid id, string name, decimal value)
    {
        Id = id;
        Name = name;
        Value = value;
        FetchWasCalled = true;
    }
}

/// <summary>
/// Child collection for aggregate.
/// </summary>
[Factory]
public class AggregateChildList : List<AggregateChild>
{
    [Create]
    public AggregateChildList() { }

    [Fetch]
    public void Fetch(
        IEnumerable<(Guid id, string name, decimal value)> items,
        [Service] IAggregateChildFactory childFactory)
    {
        foreach (var item in items)
        {
            var child = childFactory.Fetch(item.id, item.name, item.value);
            Add(child);
        }
    }
}

/// <summary>
/// Aggregate root with child collection - demonstrates parent-child factory patterns.
/// </summary>
[Factory]
public class AggregateRoot : IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public decimal Total => Children?.Sum(c => c.Value) ?? 0;
    public AggregateChildList Children { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    public bool CreateWasCalled { get; set; }
    public bool FetchWasCalled { get; set; }
    public bool InsertWasCalled { get; set; }
    public bool UpdateWasCalled { get; set; }
    public bool DeleteWasCalled { get; set; }

    /// <summary>
    /// Server-side Create - injects child list factory to initialize children.
    /// </summary>
    [Remote, Create]
    public void Create([Service] IAggregateChildListFactory childListFactory)
    {
        Id = Guid.NewGuid();
        Children = childListFactory.Create();
        CreateWasCalled = true;
    }

    /// <summary>
    /// Server-side Fetch - loads aggregate with children.
    /// </summary>
    [Remote, Fetch]
    public void Fetch(
        Guid id,
        [Service] IAggregateChildListFactory childListFactory)
    {
        Id = id;
        Name = $"Fetched-{id}";

        // Simulate loading children from "database"
        var items = new[]
        {
            (Guid.NewGuid(), "Child1", 10.0m),
            (Guid.NewGuid(), "Child2", 20.0m),
            (Guid.NewGuid(), "Child3", 30.0m)
        };
        Children = childListFactory.Fetch(items);
        FetchWasCalled = true;
        IsNew = false;
    }

    [Remote, Insert]
    public Task Insert()
    {
        InsertWasCalled = true;
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update()
    {
        UpdateWasCalled = true;
        return Task.CompletedTask;
    }

    [Remote, Delete]
    public Task Delete()
    {
        DeleteWasCalled = true;
        return Task.CompletedTask;
    }
}
