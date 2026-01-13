using RemoteOnlyTests.Domain;

namespace RemoteOnlyTests.Server;

/// <summary>
/// In-memory data store for testing (no database required).
/// </summary>
public class TestDataStore : ITestDataStore
{
    private readonly Dictionary<Guid, AggregateData> _data = new();

    public AggregateData GetAggregate(Guid id)
    {
        if (_data.TryGetValue(id, out var data))
        {
            return data;
        }

        // Return test data for any ID that doesn't exist
        return new AggregateData
        {
            Id = id,
            Name = $"Test-{id}",
            ChildData = new[]
            {
                (Guid.NewGuid(), "Child1", 10.0m),
                (Guid.NewGuid(), "Child2", 20.0m),
                (Guid.NewGuid(), "Child3", 30.0m)
            }
        };
    }

    public void Insert(ITestAggregate aggregate)
    {
        _data[aggregate.Id] = ToData(aggregate);
    }

    public void Update(ITestAggregate aggregate)
    {
        _data[aggregate.Id] = ToData(aggregate);
    }

    public void Delete(Guid id)
    {
        _data.Remove(id);
    }

    private static AggregateData ToData(ITestAggregate aggregate)
    {
        return new AggregateData
        {
            Id = aggregate.Id,
            Name = aggregate.Name,
            ChildData = aggregate.Children.Select(c => (c.Id, c.Name ?? "", c.Value))
        };
    }
}
