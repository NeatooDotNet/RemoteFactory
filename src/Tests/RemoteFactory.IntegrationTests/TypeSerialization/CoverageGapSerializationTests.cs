using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Serialization tests for identified coverage gaps:
/// - Dictionary types
/// - Enum types
/// - Large objects
///
/// These tests verify that complex types survive client-server round-trips.
/// </summary>
public class CoverageGapSerializationTests
{
    #region Dictionary Serialization Tests

    [Fact]
    public async Task Dictionary_StringString_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IDictionaryTargetFactory>();

        // Act
        var result = await factory.FetchWithData();

        // Assert
        Assert.NotNull(result.StringDictionary);
        Assert.Equal(3, result.StringDictionary.Count);
        Assert.Equal("value1", result.StringDictionary["key1"]);
        Assert.Equal("value2", result.StringDictionary["key2"]);
        Assert.Equal("value3", result.StringDictionary["key3"]);
    }

    [Fact]
    public async Task Dictionary_IntKey_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IDictionaryTargetFactory>();

        // Act
        var result = await factory.FetchWithData();

        // Assert
        Assert.NotNull(result.IntKeyDictionary);
        Assert.Equal(3, result.IntKeyDictionary.Count);
        Assert.Equal("one", result.IntKeyDictionary[1]);
        Assert.Equal("two", result.IntKeyDictionary[2]);
        Assert.Equal("forty-two", result.IntKeyDictionary[42]);
    }

    [Fact]
    public async Task Dictionary_IntValue_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IDictionaryTargetFactory>();

        // Act
        var result = await factory.FetchWithData();

        // Assert
        Assert.NotNull(result.IntValueDictionary);
        Assert.Equal(3, result.IntValueDictionary.Count);
        Assert.Equal(1, result.IntValueDictionary["a"]);
        Assert.Equal(2, result.IntValueDictionary["b"]);
        Assert.Equal(100, result.IntValueDictionary["c"]);
    }

    [Fact]
    public async Task Dictionary_GuidKey_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IDictionaryTargetFactory>();

        // Act
        var result = await factory.FetchWithData();

        // Assert
        Assert.NotNull(result.GuidKeyDictionary);
        Assert.Equal(2, result.GuidKeyDictionary.Count);
        Assert.Equal("first", result.GuidKeyDictionary[Guid.Parse("11111111-1111-1111-1111-111111111111")]);
        Assert.Equal("second", result.GuidKeyDictionary[Guid.Parse("22222222-2222-2222-2222-222222222222")]);
    }

    #endregion

    #region Enum Serialization Tests

    [Fact]
    public async Task Enum_OrderStatus_Pending_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IEnumTargetFactory>();

        // Act
        var result = await factory.FetchByStatus(OrderStatus.Pending);

        // Assert
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal(Priority.Low, result.Priority);
    }

    [Fact]
    public async Task Enum_OrderStatus_AllValues_SurviveRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IEnumTargetFactory>();

        foreach (OrderStatus status in Enum.GetValues<OrderStatus>())
        {
            // Act
            var result = await factory.FetchByStatus(status);

            // Assert
            Assert.Equal(status, result.Status);
        }
    }

    [Fact]
    public async Task Enum_NullableStatus_WithValue_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IEnumTargetFactory>();

        // Act
        var result = await factory.FetchByStatus(OrderStatus.Shipped);

        // Assert
        Assert.NotNull(result.NullableStatus);
        Assert.Equal(OrderStatus.Shipped, result.NullableStatus);
    }

    [Fact]
    public async Task Enum_NullableStatus_Null_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IEnumTargetFactory>();

        // Act
        var result = await factory.FetchByStatus(OrderStatus.Cancelled);

        // Assert
        Assert.Null(result.NullableStatus);
    }

    [Fact]
    public void EnumRecord_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IEnumRecordFactory>();

        // Act
        var result = factory.Create("TestOrder", OrderStatus.Processing, Priority.High);

        // Assert
        Assert.Equal("TestOrder", result.Name);
        Assert.Equal(OrderStatus.Processing, result.Status);
        Assert.Equal(Priority.High, result.Priority);
    }

    #endregion

    #region Large Object Serialization Tests

    [Fact]
    public async Task LargeObject_String_1KB_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ILargeObjectTargetFactory>();
        const int charCount = 1024;

        // Act
        var result = await factory.FetchLargeString(charCount);

        // Assert
        Assert.Equal(charCount, result.LargeString.Length);
        Assert.True(result.LargeString.All(c => c == 'X'));
    }

    [Fact]
    public async Task LargeObject_String_100KB_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ILargeObjectTargetFactory>();
        const int charCount = 100 * 1024;

        // Act
        var result = await factory.FetchLargeString(charCount);

        // Assert
        Assert.Equal(charCount, result.LargeString.Length);
    }

    [Fact]
    public async Task LargeObject_List_1000Items_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ILargeObjectTargetFactory>();
        const int itemCount = 1000;

        // Act
        var result = await factory.FetchLargeList(itemCount);

        // Assert
        Assert.Equal(itemCount, result.LargeList.Count);
        Assert.Equal("Item-000000", result.LargeList[0]);
        Assert.Equal("Item-000999", result.LargeList[999]);
    }

    [Fact]
    public async Task LargeObject_List_10000Items_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ILargeObjectTargetFactory>();
        const int itemCount = 10000;

        // Act
        var result = await factory.FetchLargeList(itemCount);

        // Assert
        Assert.Equal(itemCount, result.LargeList.Count);
    }

    [Fact]
    public async Task LargeObject_Dictionary_1000Items_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ILargeObjectTargetFactory>();
        const int itemCount = 1000;

        // Act
        var result = await factory.FetchLargeDictionary(itemCount);

        // Assert
        Assert.Equal(itemCount, result.LargeDictionary.Count);
        Assert.Equal("Value-000000", result.LargeDictionary[0]);
        Assert.Equal("Value-000999", result.LargeDictionary[999]);
    }

    [Fact]
    public async Task LargeObject_Binary_1KB_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ILargeObjectTargetFactory>();
        const int byteCount = 1024;

        // Act
        var result = await factory.FetchBinaryData(byteCount);

        // Assert
        Assert.Equal(byteCount, result.BinaryData.Length);
        Assert.Equal(0, result.BinaryData[0]);
        Assert.Equal(255, result.BinaryData[255]);
    }

    [Fact]
    public async Task LargeObject_Binary_1MB_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ILargeObjectTargetFactory>();
        const int byteCount = 1024 * 1024; // 1 MB

        // Act
        var result = await factory.FetchBinaryData(byteCount);

        // Assert
        Assert.Equal(byteCount, result.BinaryData.Length);
    }

    #endregion

    #region Enum Dictionary Combination Tests

    [Fact]
    public async Task EnumDictionary_StatusByName_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IEnumDictionaryTargetFactory>();

        // Act
        var result = await factory.FetchWithData();

        // Assert
        Assert.NotNull(result.StatusByName);
        Assert.Equal(3, result.StatusByName.Count);
        Assert.Equal(OrderStatus.Pending, result.StatusByName["Order1"]);
        Assert.Equal(OrderStatus.Shipped, result.StatusByName["Order2"]);
        Assert.Equal(OrderStatus.Delivered, result.StatusByName["Order3"]);
    }

    [Fact]
    public async Task EnumDictionary_NameByStatus_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IEnumDictionaryTargetFactory>();

        // Act
        var result = await factory.FetchWithData();

        // Assert
        Assert.NotNull(result.NameByStatus);
        Assert.Equal(3, result.NameByStatus.Count);
        Assert.Equal("Waiting", result.NameByStatus[OrderStatus.Pending]);
        Assert.Equal("In Progress", result.NameByStatus[OrderStatus.Processing]);
        Assert.Equal("On the Way", result.NameByStatus[OrderStatus.Shipped]);
    }

    #endregion
}
