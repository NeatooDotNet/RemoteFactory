using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore.TestLibrary;

namespace RemoteFactory.AspNetCore.Tests;

/// <summary>
/// HTTP integration tests for record support in RemoteFactory.
/// These tests validate that records work correctly through the full
/// HTTP pipeline using WebApplicationFactory.
/// </summary>
public class RecordHttpTests : IClassFixture<ContainerFixture>
{
    private readonly IHttpTestRecordFactory _recordFactory;
    private readonly IHttpFetchableRecordFactory _fetchableFactory;
    private readonly IHttpAuthorizedRecordFactory _authorizedFactory;

    public RecordHttpTests(ContainerFixture container)
    {
        var sp = container.CreateScope.ServiceProvider;
        _recordFactory = sp.GetRequiredService<IHttpTestRecordFactory>();
        _fetchableFactory = sp.GetRequiredService<IHttpFetchableRecordFactory>();
        _authorizedFactory = sp.GetRequiredService<IHttpAuthorizedRecordFactory>();
    }

    // ============================================================================
    // Basic Record Create Tests
    // ============================================================================

    [Fact]
    public void HttpPost_CreateRecord_ReturnsRecord()
    {
        // Act
        var record = _recordFactory.Create("TestName", 42);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestName", record.Name);
        Assert.Equal(42, record.Value);
    }

    [Fact]
    public void HttpPost_CreateRecord_ValueEquality()
    {
        // Act
        var record1 = _recordFactory.Create("Same", 100);
        var record2 = _recordFactory.Create("Same", 100);

        // Assert - Records should have value equality
        Assert.Equal(record1, record2);
    }

    // ============================================================================
    // Remote Fetch Tests (HTTP Round-Trip)
    // ============================================================================

    [Fact]
    public async Task HttpPost_FetchRecord_ReturnsRecord()
    {
        // Act - Async because remote operations return Task
        var record = await _fetchableFactory.FetchById("http-test");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("http-test", record.Id);
        Assert.Equal("HttpFetched-http-test", record.Data);
    }

    [Fact]
    public async Task HttpPost_FetchRecordAsync_ReturnsRecord()
    {
        // Act
        var record = await _fetchableFactory.FetchByIdAsync("async-http-test");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("async-http-test", record.Id);
        Assert.Equal("HttpAsyncFetched-async-http-test", record.Data);
    }

    // ============================================================================
    // Authorization Tests
    // ============================================================================

    [Fact]
    public async Task HttpPost_RecordWithAuth_Authorized()
    {
        // Act - This should succeed because "TestPolicy" and "Test role" pass
        var record = await _authorizedFactory.FetchAuthorized("auth-test");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("Authorized-auth-test", record.Name);
    }

    [Fact]
    public async Task HttpPost_RecordWithAuth_Denied()
    {
        // Act - Unauthorized should return null (not throw) because "No auth" role fails
        var record = await _authorizedFactory.FetchUnauthorized("no-auth-test");

        // Assert
        Assert.Null(record);
    }

    [Fact]
    public async Task HttpPost_CanFetchAuthorized_ReturnsTrue()
    {
        // Act - CanFetch methods check if caller has access (no parameters)
        var result = await _authorizedFactory.CanFetchAuthorized();

        // Assert
        Assert.True(result.HasAccess);
    }

    [Fact]
    public async Task HttpPost_CanFetchUnauthorized_ReturnsFalse()
    {
        // Act
        var result = await _authorizedFactory.CanFetchUnauthorized();

        // Assert
        Assert.False(result.HasAccess);
    }
}
