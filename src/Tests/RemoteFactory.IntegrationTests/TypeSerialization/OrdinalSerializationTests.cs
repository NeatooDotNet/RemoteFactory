using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Tests for ordinal (compact array) serialization format.
/// These tests validate that the ordinal serialization format works correctly
/// for types implementing IOrdinalSerializable.
/// </summary>
public class OrdinalSerializationTests
{
    // ============================================================================
    // MemberData providers for different serialization formats
    // ============================================================================

    public static IEnumerable<object[]> OrdinalFormat_Client()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRemoteRecordFactory>() };
    }

    public static IEnumerable<object[]> OrdinalFormat_Local()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.local.ServiceProvider.GetRequiredService<IRemoteRecordFactory>() };
    }

    public static IEnumerable<object[]> NamedFormat_Client()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRemoteRecordFactory>() };
    }

    public static IEnumerable<object[]> NamedFormat_Local()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.local.ServiceProvider.GetRequiredService<IRemoteRecordFactory>() };
    }

    // ============================================================================
    // Ordinal Format Tests
    // ============================================================================

    [Theory]
    [MemberData(nameof(OrdinalFormat_Client))]
    [MemberData(nameof(OrdinalFormat_Local))]
    public async Task OrdinalFormat_SimpleRecord_RoundTrips(IRemoteRecordFactory factory)
    {
        // Act
        var result = await factory.FetchRemote("test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }

    [Theory]
    [MemberData(nameof(OrdinalFormat_Client))]
    [MemberData(nameof(OrdinalFormat_Local))]
    public async Task OrdinalFormat_AsyncFetch_RoundTrips(IRemoteRecordFactory factory)
    {
        // Act
        var result = await factory.FetchRemoteAsync("async-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("RemoteAsync-async-test", result.Name);
    }

    // ============================================================================
    // Named Format Tests (backwards compatibility)
    // ============================================================================

    [Theory]
    [MemberData(nameof(NamedFormat_Client))]
    [MemberData(nameof(NamedFormat_Local))]
    public async Task NamedFormat_SimpleRecord_RoundTrips(IRemoteRecordFactory factory)
    {
        // Act
        var result = await factory.FetchRemote("test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }

    [Theory]
    [MemberData(nameof(NamedFormat_Client))]
    [MemberData(nameof(NamedFormat_Local))]
    public async Task NamedFormat_AsyncFetch_RoundTrips(IRemoteRecordFactory factory)
    {
        // Act
        var result = await factory.FetchRemoteAsync("async-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("RemoteAsync-async-test", result.Name);
    }

    // ============================================================================
    // Complex Type Tests with Both Formats
    // ============================================================================

    public static IEnumerable<object[]> ComplexRecordFactory_Ordinal()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IComplexRecordFactory>() };
    }

    public static IEnumerable<object[]> ComplexRecordFactory_Named()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IComplexRecordFactory>() };
    }

    [Theory]
    [MemberData(nameof(ComplexRecordFactory_Ordinal))]
    [MemberData(nameof(ComplexRecordFactory_Named))]
    public void BothFormats_ComplexRecord_AllTypesSerialize(IComplexRecordFactory factory)
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
    // Collection Tests with Both Formats
    // ============================================================================

    public static IEnumerable<object[]> CollectionFactory_Ordinal()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithCollectionFactory>() };
    }

    public static IEnumerable<object[]> CollectionFactory_Named()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithCollectionFactory>() };
    }

    [Theory]
    [MemberData(nameof(CollectionFactory_Ordinal))]
    [MemberData(nameof(CollectionFactory_Named))]
    public void BothFormats_Collection_Serializes(IRecordWithCollectionFactory factory)
    {
        // Arrange
        var items = new List<string> { "Alpha", "Beta", "Gamma" };

        // Act
        var record = factory.Create("CollectionTest", items);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("CollectionTest", record.Name);
        Assert.Equal(items, record.Items);
    }

    // ============================================================================
    // Nullable Tests with Both Formats
    // ============================================================================

    public static IEnumerable<object[]> NullableFactory_Ordinal()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithNullableFactory>() };
    }

    public static IEnumerable<object[]> NullableFactory_Named()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithNullableFactory>() };
    }

    [Theory]
    [MemberData(nameof(NullableFactory_Ordinal))]
    [MemberData(nameof(NullableFactory_Named))]
    public void BothFormats_NullValue_Serializes(IRecordWithNullableFactory factory)
    {
        // Act
        var record = factory.Create("NullTest", null);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("NullTest", record.Name);
        Assert.Null(record.Description);
    }

    [Theory]
    [MemberData(nameof(NullableFactory_Ordinal))]
    [MemberData(nameof(NullableFactory_Named))]
    public void BothFormats_NonNullValue_Serializes(IRecordWithNullableFactory factory)
    {
        // Act
        var record = factory.Create("NonNullTest", "Description here");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("NonNullTest", record.Name);
        Assert.Equal("Description here", record.Description);
    }

    // ============================================================================
    // Nested Record Tests with Both Formats
    // ============================================================================

    public static IEnumerable<object[]> NestedRecordFactory_Ordinal()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IOuterRecordFactory>() };
    }

    public static IEnumerable<object[]> NestedRecordFactory_Named()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IOuterRecordFactory>() };
    }

    [Theory]
    [MemberData(nameof(NestedRecordFactory_Ordinal))]
    [MemberData(nameof(NestedRecordFactory_Named))]
    public void BothFormats_NestedRecord_Serializes(IOuterRecordFactory factory)
    {
        // Arrange
        var inner = new InnerRecord("InnerValue");

        // Act
        var record = factory.Create("OuterName", inner);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("OuterName", record.Name);
        Assert.NotNull(record.Inner);
        Assert.Equal("InnerValue", record.Inner.InnerValue);
    }

    // ============================================================================
    // Default Values Tests with Both Formats
    // ============================================================================

    public static IEnumerable<object[]> DefaultsFactory_Ordinal()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithDefaultsFactory>() };
    }

    public static IEnumerable<object[]> DefaultsFactory_Named()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithDefaultsFactory>() };
    }

    [Theory]
    [MemberData(nameof(DefaultsFactory_Ordinal))]
    [MemberData(nameof(DefaultsFactory_Named))]
    public void BothFormats_RecordWithDefaults_UsesDefaultValues(IRecordWithDefaultsFactory factory)
    {
        // Act - use the default values by passing them explicitly
        var record = factory.Create("default", 42);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("default", record.Name);
        Assert.Equal(42, record.Value);
    }

    [Theory]
    [MemberData(nameof(DefaultsFactory_Ordinal))]
    [MemberData(nameof(DefaultsFactory_Named))]
    public void BothFormats_RecordWithCustomValues_Serializes(IRecordWithDefaultsFactory factory)
    {
        // Act - use custom values
        var record = factory.Create("custom", 100);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("custom", record.Name);
        Assert.Equal(100, record.Value);
    }

    // ============================================================================
    // Nullable Collection Tests (CS8639 Regression)
    // ============================================================================

    public static IEnumerable<object[]> NullableCollectionFactory_Ordinal()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithNullableCollectionFactory>() };
    }

    public static IEnumerable<object[]> NullableCollectionFactory_Named()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithNullableCollectionFactory>() };
    }

    [Theory]
    [MemberData(nameof(NullableCollectionFactory_Ordinal))]
    [MemberData(nameof(NullableCollectionFactory_Named))]
    public void BothFormats_NullableCollection_WithValues_Serializes(IRecordWithNullableCollectionFactory factory)
    {
        // Arrange
        var items = new List<string> { "A", "B", "C" };

        // Act
        var record = factory.Create("Test", items);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("Test", record.Name);
        Assert.NotNull(record.Items);
        Assert.Equal(items, record.Items);
    }

    [Theory]
    [MemberData(nameof(NullableCollectionFactory_Ordinal))]
    [MemberData(nameof(NullableCollectionFactory_Named))]
    public void BothFormats_NullableCollection_WithNull_Serializes(IRecordWithNullableCollectionFactory factory)
    {
        // Act
        var record = factory.Create("NullItems", null);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("NullItems", record.Name);
        Assert.Null(record.Items);
    }

    // ============================================================================
    // Complex Nullable Generics Tests (CS8639 Regression)
    // ============================================================================

    public static IEnumerable<object[]> ComplexNullableGenericsFactory_Ordinal()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithComplexNullableGenericsFactory>() };
    }

    public static IEnumerable<object[]> ComplexNullableGenericsFactory_Named()
    {
        var scopes = ClientServerContainers.Scopes(SerializationFormat.Named);
        yield return new object[] { scopes.client.ServiceProvider.GetRequiredService<IRecordWithComplexNullableGenericsFactory>() };
    }

    [Theory]
    [MemberData(nameof(ComplexNullableGenericsFactory_Ordinal))]
    [MemberData(nameof(ComplexNullableGenericsFactory_Named))]
    public void BothFormats_ComplexNullableGenerics_WithValues_Serializes(IRecordWithComplexNullableGenericsFactory factory)
    {
        // Arrange
        var metadata = new Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } };
        var nullableItems = new List<string?> { "A", null, "C" };

        // Act
        var record = factory.Create("Test", metadata, nullableItems);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("Test", record.Name);
        Assert.NotNull(record.Metadata);
        Assert.Equal(2, record.Metadata!.Count);
        Assert.NotNull(record.NullableItems);
        Assert.Equal(3, record.NullableItems!.Count);
    }

    [Theory]
    [MemberData(nameof(ComplexNullableGenericsFactory_Ordinal))]
    [MemberData(nameof(ComplexNullableGenericsFactory_Named))]
    public void BothFormats_ComplexNullableGenerics_AllNull_Serializes(IRecordWithComplexNullableGenericsFactory factory)
    {
        // Act
        var record = factory.Create("AllNull", null, null);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("AllNull", record.Name);
        Assert.Null(record.Metadata);
        Assert.Null(record.NullableItems);
    }
}

/// <summary>
/// Tests that verify the generated PropertyTypes arrays are correct.
/// </summary>
public class PropertyTypesGenerationTests
{
    [Fact]
    public void RecordWithNullableCollection_PropertyTypes_NoNullableAnnotation()
    {
        // Arrange & Act
        var propertyTypes = RecordWithNullableCollection.PropertyTypes;

        // Assert
        Assert.Equal(2, propertyTypes.Length);
        Assert.Equal(typeof(List<string>), propertyTypes[0]);
        Assert.Equal(typeof(string), propertyTypes[1]);
    }

    [Fact]
    public void RecordWithComplexNullableGenerics_PropertyTypes_NoNullableAnnotation()
    {
        // Arrange & Act
        var propertyTypes = RecordWithComplexNullableGenerics.PropertyTypes;

        // Assert
        Assert.Equal(3, propertyTypes.Length);
        Assert.Equal(typeof(Dictionary<string, int>), propertyTypes[0]);
        Assert.Equal(typeof(string), propertyTypes[1]);
        Assert.Equal(typeof(List<string>), propertyTypes[2]);
    }

    [Fact]
    public void RecordWithNullable_PropertyTypes_SimpleNullableString()
    {
        // Arrange & Act
        var propertyTypes = RecordWithNullable.PropertyTypes;

        // Assert
        Assert.Equal(2, propertyTypes.Length);
        Assert.Equal(typeof(string), propertyTypes[0]);
        Assert.Equal(typeof(string), propertyTypes[1]);
    }

    [Fact]
    public void RecordWithNullableValueTypes_PropertyTypes_NoNullableAnnotation()
    {
        // Arrange & Act
        var propertyTypes = RecordWithNullableValueTypes.PropertyTypes;

        // Assert
        Assert.Equal(5, propertyTypes.Length);
        Assert.Equal(typeof(string), propertyTypes[0]); // Name
        Assert.Equal(typeof(DateTime), propertyTypes[1]); // NullableDateTime
        Assert.Equal(typeof(decimal), propertyTypes[2]); // NullableDecimal
        Assert.Equal(typeof(Guid), propertyTypes[3]); // NullableGuid
        Assert.Equal(typeof(int), propertyTypes[4]); // NullableInt
    }

    [Fact]
    public void RecordWithNullableValueTypes_ToOrdinalArray_SerializesCorrectly()
    {
        // Arrange
        var record = new RecordWithNullableValueTypes(
            "Test",
            42,
            new DateTime(2024, 1, 15),
            Guid.Parse("12345678-1234-1234-1234-123456789012"),
            123.45m);

        // Act
        var array = record.ToOrdinalArray();

        // Assert
        Assert.Equal(5, array.Length);
        Assert.Equal("Test", array[0]);
        Assert.Equal(new DateTime(2024, 1, 15), array[1]);
        Assert.Equal(123.45m, array[2]);
        Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789012"), array[3]);
        Assert.Equal(42, array[4]);
    }

    [Fact]
    public void RecordWithNullableValueTypes_FromOrdinalArray_DeserializesCorrectly()
    {
        // Arrange
        var array = new object?[]
        {
            "Test",
            new DateTime(2024, 1, 15),
            123.45m,
            Guid.Parse("12345678-1234-1234-1234-123456789012"),
            42
        };

        // Act
        var record = (RecordWithNullableValueTypes)RecordWithNullableValueTypes.FromOrdinalArray(array);

        // Assert
        Assert.Equal("Test", record.Name);
        Assert.Equal(42, record.NullableInt);
        Assert.Equal(new DateTime(2024, 1, 15), record.NullableDateTime);
        Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789012"), record.NullableGuid);
        Assert.Equal(123.45m, record.NullableDecimal);
    }

    [Fact]
    public void RecordWithNullableValueTypes_FromOrdinalArray_HandlesNullValues()
    {
        // Arrange
        var array = new object?[]
        {
            "Test",
            null, // NullableDateTime
            null, // NullableDecimal
            null, // NullableGuid
            null  // NullableInt
        };

        // Act
        var record = (RecordWithNullableValueTypes)RecordWithNullableValueTypes.FromOrdinalArray(array);

        // Assert
        Assert.Equal("Test", record.Name);
        Assert.Null(record.NullableInt);
        Assert.Null(record.NullableDateTime);
        Assert.Null(record.NullableGuid);
        Assert.Null(record.NullableDecimal);
    }
}

/// <summary>
/// Tests that verify the serialization format configuration works correctly.
/// </summary>
public class SerializationFormatConfigurationTests
{
    [Fact]
    public void DefaultFormat_IsOrdinal()
    {
        // Arrange
        var options = new NeatooSerializationOptions();

        // Assert
        Assert.Equal(SerializationFormat.Ordinal, options.Format);
    }

    [Fact]
    public void FormatHeaderValue_Ordinal_ReturnsOrdinal()
    {
        // Arrange
        var options = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };

        // Assert
        Assert.Equal("ordinal", options.FormatHeaderValue);
    }

    [Fact]
    public void FormatHeaderValue_Named_ReturnsNamed()
    {
        // Arrange
        var options = new NeatooSerializationOptions { Format = SerializationFormat.Named };

        // Assert
        Assert.Equal("named", options.FormatHeaderValue);
    }

    [Theory]
    [InlineData("ordinal", SerializationFormat.Ordinal)]
    [InlineData("ORDINAL", SerializationFormat.Ordinal)]
    [InlineData("Ordinal", SerializationFormat.Ordinal)]
    [InlineData("named", SerializationFormat.Named)]
    [InlineData("NAMED", SerializationFormat.Named)]
    [InlineData("Named", SerializationFormat.Named)]
    [InlineData(null, SerializationFormat.Ordinal)]
    [InlineData("", SerializationFormat.Ordinal)]
    [InlineData("unknown", SerializationFormat.Ordinal)]
    public void ParseHeaderValue_ParsesCorrectly(string? headerValue, SerializationFormat expected)
    {
        // Act
        var result = NeatooSerializationOptions.ParseHeaderValue(headerValue);

        // Assert
        Assert.Equal(expected, result);
    }
}
