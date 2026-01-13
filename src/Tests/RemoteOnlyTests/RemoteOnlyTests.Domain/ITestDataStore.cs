namespace RemoteOnlyTests.Domain;

/// <summary>
/// Data transfer object for aggregate data.
/// </summary>
public class AggregateData
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public IEnumerable<(Guid id, string name, decimal value)> ChildData { get; set; } = [];
}

/// <summary>
/// In-memory data store interface for testing.
/// Server-only - not used on client.
/// </summary>
public interface ITestDataStore
{
    AggregateData GetAggregate(Guid id);
    void Insert(ITestAggregate aggregate);
    void Update(ITestAggregate aggregate);
    void Delete(Guid id);
}
