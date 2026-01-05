using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.DocsExamples.Infrastructure;

namespace Neatoo.RemoteFactory.DocsExamples.Concepts;

#region docs-record-simple
/// <summary>
/// Simple positional record with [Create] on type.
/// The primary constructor parameters become the Create method parameters.
/// </summary>
[Factory]
[Create]
public partial record SimpleRecordExample(string Name, int Value);
#endregion docs-record-simple

#region docs-record-fetch
/// <summary>
/// Record with Fetch operation.
/// Static Fetch methods return record instances.
/// </summary>
[Factory]
[Create]
public partial record FetchableRecordExample(string Id, string Data)
{
    [Fetch]
    public static FetchableRecordExample FetchById(string id)
        => new FetchableRecordExample(id, $"Fetched-{id}");

    [Fetch]
    public static Task<FetchableRecordExample> FetchByIdAsync(string id)
        => Task.FromResult(new FetchableRecordExample(id, $"AsyncFetched-{id}"));
}
#endregion docs-record-fetch

#region docs-record-service
/// <summary>
/// Calculator service interface for record examples.
/// </summary>
public interface ICalculatorService
{
    int Add(int a, int b);
}

/// <summary>
/// Calculator service implementation.
/// </summary>
public class CalculatorService : ICalculatorService
{
    public int Add(int a, int b) => a + b;
}

/// <summary>
/// Record with service injection in primary constructor.
/// Use [Service] attribute on parameters that should be injected.
/// </summary>
[Factory]
[Create]
public partial record ServiceRecordExample(string Name, [Service] ICalculatorService Calculator)
{
    public int Calculate(int a, int b) => Calculator.Add(a, b);
}
#endregion docs-record-service

#region docs-record-remote
/// <summary>
/// Record with remote fetch operation.
/// The [Remote] attribute causes the fetch to execute on the server.
/// </summary>
[Factory]
[Create]
public partial record RemoteFetchRecordExample(string Name)
{
    [Fetch]
    [Remote]
    public static RemoteFetchRecordExample FetchFromServer(string name)
        => new RemoteFetchRecordExample($"Server-{name}");
}
#endregion docs-record-remote

#region docs-record-explicit-constructor
/// <summary>
/// Record with explicit constructor.
/// Use when you need custom initialization logic.
/// </summary>
[Factory]
public partial record ExplicitConstructorRecordExample
{
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    [Create]
    public ExplicitConstructorRecordExample(string name)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }
}
#endregion docs-record-explicit-constructor

/// <summary>
/// Tests for record examples.
/// </summary>
public class RecordTests : DocsTestBase<ISimpleRecordExampleFactory>
{
    [Fact]
    public void SimpleRecord_Create()
    {
        // Act
        var record = factory.Create("TestName", 42);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestName", record.Name);
        Assert.Equal(42, record.Value);
    }

    [Fact]
    public void SimpleRecord_ValueEquality()
    {
        // Act
        var record1 = factory.Create("Same", 100);
        var record2 = factory.Create("Same", 100);

        // Assert - Records have value-based equality
        Assert.Equal(record1, record2);
    }

    [Fact]
    public void FetchableRecord_FetchById()
    {
        // Arrange
        var fetchFactory = clientScope.ServiceProvider.GetRequiredService<IFetchableRecordExampleFactory>();

        // Act
        var record = fetchFactory.FetchById("test-id");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("test-id", record.Id);
        Assert.Equal("Fetched-test-id", record.Data);
    }

    [Fact]
    public async Task FetchableRecord_FetchByIdAsync()
    {
        // Arrange
        var fetchFactory = clientScope.ServiceProvider.GetRequiredService<IFetchableRecordExampleFactory>();

        // Act
        var record = await fetchFactory.FetchByIdAsync("async-id");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("async-id", record.Id);
        Assert.Equal("AsyncFetched-async-id", record.Data);
    }

    [Fact]
    public void ServiceRecord_InjectsService()
    {
        // Arrange
        var serviceFactory = clientScope.ServiceProvider.GetRequiredService<IServiceRecordExampleFactory>();

        // Act
        var record = serviceFactory.Create("WithService");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("WithService", record.Name);
        Assert.NotNull(record.Calculator);
        Assert.Equal(5, record.Calculate(2, 3));
    }

    [Fact]
    public void ExplicitConstructorRecord_Create()
    {
        // Arrange
        var explicitFactory = clientScope.ServiceProvider.GetRequiredService<IExplicitConstructorRecordExampleFactory>();

        // Act
        var record = explicitFactory.Create("ExplicitName");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("ExplicitName", record.Name);
        Assert.True(record.CreatedAt <= DateTime.UtcNow);
    }
}
