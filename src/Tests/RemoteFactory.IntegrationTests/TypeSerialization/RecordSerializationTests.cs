using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Serialization round-trip tests for record support in RemoteFactory.
/// These tests validate that records survive the client to server to client
/// serialization cycle using the two DI container approach.
/// </summary>
public class RecordSerializationTests
{
    // ============================================================================
    // MemberData providers for client and local containers
    // ============================================================================

    public static IEnumerable<object[]> RemoteRecordFactory_Client()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRemoteRecordFactory>() };
    }

    public static IEnumerable<object[]> RemoteRecordFactory_Local()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[] { scopes.local.ServiceProvider.GetRequiredService<IRemoteRecordFactory>() };
    }

    public static IEnumerable<object[]> ComplexRecordFactory_Client()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IComplexRecordFactory>() };
    }

    public static IEnumerable<object[]> ComplexRecordFactory_Local()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[] { scopes.local.ServiceProvider.GetRequiredService<IComplexRecordFactory>() };
    }

    public static IEnumerable<object[]> RecordWithCollectionFactory_Client()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithCollectionFactory>() };
    }

    public static IEnumerable<object[]> RecordWithCollectionFactory_Local()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[] { scopes.local.ServiceProvider.GetRequiredService<IRecordWithCollectionFactory>() };
    }

    public static IEnumerable<object[]> RecordWithNullableFactory_Client()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithNullableFactory>() };
    }

    public static IEnumerable<object[]> RecordWithNullableFactory_Local()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[] { scopes.local.ServiceProvider.GetRequiredService<IRecordWithNullableFactory>() };
    }

    public static IEnumerable<object[]> OuterRecordFactory_Client()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[]
        {
            scopes.client.ServiceProvider.GetRequiredService<IOuterRecordFactory>(),
            scopes.client.ServiceProvider.GetRequiredService<IInnerRecordFactory>()
        };
    }

    public static IEnumerable<object[]> OuterRecordFactory_Local()
    {
        var scopes = ClientServerContainers.Scopes();
        yield return new object[]
        {
            scopes.local.ServiceProvider.GetRequiredService<IOuterRecordFactory>(),
            scopes.local.ServiceProvider.GetRequiredService<IInnerRecordFactory>()
        };
    }

    // ============================================================================
    // Record Remote Fetch Tests - Full serialization round-trip
    // ============================================================================

    [Theory]
    [MemberData(nameof(RemoteRecordFactory_Client))]
    [MemberData(nameof(RemoteRecordFactory_Local))]
    public async Task RemoteFetch_SimpleRecord_RoundTrips(IRemoteRecordFactory factory)
    {
        // Act - goes through serialization for client container
        var result = await factory.FetchRemote("test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }

    [Theory]
    [MemberData(nameof(RemoteRecordFactory_Client))]
    [MemberData(nameof(RemoteRecordFactory_Local))]
    public async Task RemoteFetchAsync_SimpleRecord_RoundTrips(IRemoteRecordFactory factory)
    {
        // Act - goes through serialization for client container
        var result = await factory.FetchRemoteAsync("async-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("RemoteAsync-async-test", result.Name);
    }

    // ============================================================================
    // Complex Record Serialization Tests
    // ============================================================================

    [Theory]
    [MemberData(nameof(ComplexRecordFactory_Client))]
    [MemberData(nameof(ComplexRecordFactory_Local))]
    public void Create_ComplexRecord_AllTypesSerialize(IComplexRecordFactory factory)
    {
        // Arrange
        var now = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var guid = new Guid("12345678-1234-1234-1234-123456789012");

        // Act
        var record = factory.Create(
            "TestString",
            42,
            9876543210L,
            3.14159265359,
            123.45m,
            true,
            now,
            guid);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestString", record.StringProp);
        Assert.Equal(42, record.IntProp);
        Assert.Equal(9876543210L, record.LongProp);
        Assert.Equal(3.14159265359, record.DoubleProp, precision: 10);
        Assert.Equal(123.45m, record.DecimalProp);
        Assert.True(record.BoolProp);
        Assert.Equal(now, record.DateTimeProp);
        Assert.Equal(guid, record.GuidProp);
    }

    // ============================================================================
    // Collection Property Serialization Tests
    // ============================================================================

    [Theory]
    [MemberData(nameof(RecordWithCollectionFactory_Client))]
    [MemberData(nameof(RecordWithCollectionFactory_Local))]
    public void Create_RecordWithCollection_CollectionSerializes(IRecordWithCollectionFactory factory)
    {
        // Arrange
        var items = new List<string> { "Alpha", "Beta", "Gamma", "Delta" };

        // Act
        var record = factory.Create("CollectionTest", items);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("CollectionTest", record.Name);
        Assert.NotNull(record.Items);
        Assert.Equal(4, record.Items.Count);
        Assert.Equal(items, record.Items);
    }

    [Theory]
    [MemberData(nameof(RecordWithCollectionFactory_Client))]
    [MemberData(nameof(RecordWithCollectionFactory_Local))]
    public void Create_RecordWithEmptyCollection_EmptyCollectionSerializes(IRecordWithCollectionFactory factory)
    {
        // Arrange
        var items = new List<string>();

        // Act
        var record = factory.Create("EmptyCollection", items);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("EmptyCollection", record.Name);
        Assert.NotNull(record.Items);
        Assert.Empty(record.Items);
    }

    // ============================================================================
    // Nullable Property Serialization Tests
    // ============================================================================

    [Theory]
    [MemberData(nameof(RecordWithNullableFactory_Client))]
    [MemberData(nameof(RecordWithNullableFactory_Local))]
    public void Create_RecordWithNullValue_NullSerializes(IRecordWithNullableFactory factory)
    {
        // Act
        var record = factory.Create("NullTest", null);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("NullTest", record.Name);
        Assert.Null(record.Description);
    }

    [Theory]
    [MemberData(nameof(RecordWithNullableFactory_Client))]
    [MemberData(nameof(RecordWithNullableFactory_Local))]
    public void Create_RecordWithNonNullValue_ValueSerializes(IRecordWithNullableFactory factory)
    {
        // Act
        var record = factory.Create("NonNullTest", "This is a description");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("NonNullTest", record.Name);
        Assert.Equal("This is a description", record.Description);
    }

    // ============================================================================
    // Nested Record Serialization Tests
    // ============================================================================

    [Theory]
    [MemberData(nameof(OuterRecordFactory_Client))]
    [MemberData(nameof(OuterRecordFactory_Local))]
    public void Create_NestedRecords_BothRecordsSerialize(
        IOuterRecordFactory outerFactory,
        IInnerRecordFactory innerFactory)
    {
        // Arrange
        var inner = innerFactory.Create("InnerTestValue");

        // Act
        var outer = outerFactory.Create("OuterTestName", inner);

        // Assert
        Assert.NotNull(outer);
        Assert.Equal("OuterTestName", outer.Name);
        Assert.NotNull(outer.Inner);
        Assert.Equal("InnerTestValue", outer.Inner.InnerValue);
    }

    // ============================================================================
    // Value Equality After Serialization Tests
    // ============================================================================

    [Fact]
    public void Serialization_RecordEquality_PreservedAfterRoundTrip()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.ServiceProvider.GetRequiredService<IEqualityTestRecordFactory>();
        var timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act - Create two records with same values (through client which serializes)
        var record1 = clientFactory.Create("EqualTest", 42, timestamp);
        var record2 = clientFactory.Create("EqualTest", 42, timestamp);

        // Assert - Records should be equal due to value-based equality
        Assert.Equal(record1, record2);
        Assert.Equal(record1.GetHashCode(), record2.GetHashCode());
    }

    [Fact]
    public void Serialization_DifferentRecords_NotEqual()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.ServiceProvider.GetRequiredService<IEqualityTestRecordFactory>();
        var timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var record1 = clientFactory.Create("Test1", 42, timestamp);
        var record2 = clientFactory.Create("Test2", 42, timestamp);

        // Assert
        Assert.NotEqual(record1, record2);
    }
}

/// <summary>
/// Client-Server container tests that validate record operations in the
/// different container configurations (client, server, local).
/// </summary>
public class RecordClientServerTests
{
    [Fact]
    public async Task ClientContainer_RemoteFetch_SerializesCorrectly()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.ServiceProvider
            .GetRequiredService<IRemoteRecordFactory>();

        // Act - goes through full serialization round-trip
        var result = await clientFactory.FetchRemote("test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }

    [Fact]
    public async Task LocalContainer_RemoteFetch_ExecutesDirectly()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var localFactory = scopes.local.ServiceProvider
            .GetRequiredService<IRemoteRecordFactory>();

        // Act - local container executes directly without serialization
        var result = await localFactory.FetchRemote("test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }

    [Fact]
    public async Task ClientContainer_RemoteAsyncFetch_SerializesCorrectly()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.ServiceProvider
            .GetRequiredService<IRemoteRecordFactory>();

        // Act - goes through full serialization round-trip
        var result = await clientFactory.FetchRemoteAsync("async-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("RemoteAsync-async-test", result.Name);
    }

    [Fact]
    public void ServerContainer_ReceivesCorrectParameters_AfterSerialization()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.ServiceProvider
            .GetRequiredService<IComplexRecordFactory>();

        var testGuid = Guid.NewGuid();
        var testDate = new DateTime(2024, 3, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act - Complex parameters go through serialization
        var result = clientFactory.Create(
            "SerializedString",
            12345,
            9876543210L,
            3.14159,
            99.99m,
            true,
            testDate,
            testGuid);

        // Assert - All parameters preserved after serialization round-trip
        Assert.NotNull(result);
        Assert.Equal("SerializedString", result.StringProp);
        Assert.Equal(12345, result.IntProp);
        Assert.Equal(9876543210L, result.LongProp);
        Assert.Equal(3.14159, result.DoubleProp, precision: 5);
        Assert.Equal(99.99m, result.DecimalProp);
        Assert.True(result.BoolProp);
        Assert.Equal(testDate, result.DateTimeProp);
        Assert.Equal(testGuid, result.GuidProp);
    }
}
