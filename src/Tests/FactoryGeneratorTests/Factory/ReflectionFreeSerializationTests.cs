using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for the reflection-free ordinal serialization feature.
/// These tests verify that the generated ordinal converters work correctly
/// and that converters are properly registered during factory initialization.
/// </summary>
public class ReflectionFreeSerializationTests
{
	// ============================================================================
	// Converter Registration Tests (PROVES AOT PATH IS USED)
	// ============================================================================

	[Fact]
	public void ConvertersAreRegisteredViaDI()
	{
		// Create DI container - this calls FactoryServiceRegistrar which registers converters
		var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);

		// Verify converters were registered (may be more if other tests ran first)
		var count = NeatooOrdinalConverterFactory.RegisteredConverterCount;

		// Should have registered multiple converters
		Assert.True(count >= 10,
			$"Expected at least 10 converters registered via DI, got {count}");
	}

	[Fact]
	public void RegisteredConverterIsUsedInsteadOfReflection()
	{
		// This test verifies that once a converter is registered (whether by DI or manually),
		// the factory returns the cached converter instead of creating a new one via reflection.

		// 1. Get a converter from the cache (or trigger registration via CreateOrdinalConverter)
		var customConverter = SimpleRecord.CreateOrdinalConverter();

		// 2. Register it (this is a no-op if already registered, but ensures it's in cache)
		NeatooOrdinalConverterFactory.RegisterConverter(customConverter);

		// 3. Create factory and request converter
		var serializationOptions = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
		var factory = new NeatooOrdinalConverterFactory(serializationOptions);
		var options = new JsonSerializerOptions();
		var converter = factory.CreateConverter(typeof(SimpleRecord), options);

		// 4. Should be the cached instance (not a new reflection-created one)
		// The key insight: if AOT path is NOT used, we'd get a NeatooOrdinalConverter<T>
		// created via Activator.CreateInstance, not our SimpleRecordOrdinalConverter
		Assert.IsType<SimpleRecordOrdinalConverter>(converter);
	}

	// ============================================================================
	// Converter Interface Tests
	// ============================================================================

	[Fact]
	public void Type_ImplementsIOrdinalConverterProvider()
	{
		// Verify that generated types implement the provider interface
		Assert.True(typeof(IOrdinalConverterProvider<SimpleRecord>).IsAssignableFrom(typeof(SimpleRecord)));
		Assert.True(typeof(IOrdinalConverterProvider<ComplexRecord>).IsAssignableFrom(typeof(ComplexRecord)));
		Assert.True(typeof(IOrdinalConverterProvider<RecordWithNullable>).IsAssignableFrom(typeof(RecordWithNullable)));
	}

	[Fact]
	public void Type_ImplementsIOrdinalSerializable()
	{
		// Verify that generated types implement ordinal serializable
		Assert.True(typeof(IOrdinalSerializable).IsAssignableFrom(typeof(SimpleRecord)));
		Assert.True(typeof(IOrdinalSerializationMetadata).IsAssignableFrom(typeof(SimpleRecord)));
	}

	[Fact]
	public void GeneratedConverter_CanBeCreatedViaProviderInterface()
	{
		// Verify that the generated converter can be created via the static interface method
		var converter = SimpleRecord.CreateOrdinalConverter();
		Assert.NotNull(converter);
	}

	// ============================================================================
	// Serialization Correctness Tests
	// ============================================================================

	[Fact]
	public void GeneratedConverter_SerializesSimpleRecord()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var record = new SimpleRecord("Test", 42);

		// Act
		var json = JsonSerializer.Serialize(record, options);

		// Assert - ordinal format is an array
		Assert.StartsWith("[", json);
		Assert.EndsWith("]", json);
		// Properties are alphabetical: Name, Value
		Assert.Equal("[\"Test\",42]", json);
	}

	[Fact]
	public void GeneratedConverter_SerializesComplexRecord()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var now = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
		var guid = new Guid("12345678-1234-1234-1234-123456789012");
		var record = new ComplexRecord(
			"TestString",
			42,
			9876543210L,
			3.14159265359,
			123.45m,
			true,
			now,
			guid);

		// Act
		var json = JsonSerializer.Serialize(record, options);

		// Assert - Properties are alphabetical: BoolProp, DateTimeProp, DecimalProp, DoubleProp, GuidProp, IntProp, LongProp, StringProp
		Assert.StartsWith("[", json);
		Assert.EndsWith("]", json);
		Assert.Contains("true", json); // BoolProp
		Assert.Contains("42", json);   // IntProp
	}

	[Fact]
	public void GeneratedConverter_SerializesNull()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		SimpleRecord? record = null;

		// Act
		var json = JsonSerializer.Serialize(record, options);

		// Assert
		Assert.Equal("null", json);
	}

	// ============================================================================
	// Deserialization Correctness Tests
	// ============================================================================

	[Fact]
	public void GeneratedConverter_DeserializesSimpleRecord()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var json = "[\"Test\",42]"; // Alphabetical: Name, Value

		// Act
		var record = JsonSerializer.Deserialize<SimpleRecord>(json, options);

		// Assert
		Assert.NotNull(record);
		Assert.Equal("Test", record.Name);
		Assert.Equal(42, record.Value);
	}

	[Fact]
	public void GeneratedConverter_DeserializesNull()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var json = "null";

		// Act
		var record = JsonSerializer.Deserialize<SimpleRecord?>(json, options);

		// Assert
		Assert.Null(record);
	}

	// ============================================================================
	// Nullable Property Tests
	// ============================================================================

	[Fact]
	public void GeneratedConverter_HandlesNullablePropertyWithValue()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var record = new RecordWithNullable("TestName", "TestDescription");

		// Act
		var json = JsonSerializer.Serialize(record, options);
		var deserialized = JsonSerializer.Deserialize<RecordWithNullable>(json, options);

		// Assert
		Assert.NotNull(deserialized);
		Assert.Equal("TestName", deserialized.Name);
		Assert.Equal("TestDescription", deserialized.Description);
	}

	[Fact]
	public void GeneratedConverter_HandlesNullablePropertyWithNull()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var record = new RecordWithNullable("TestName", null);

		// Act
		var json = JsonSerializer.Serialize(record, options);
		var deserialized = JsonSerializer.Deserialize<RecordWithNullable>(json, options);

		// Assert
		Assert.NotNull(deserialized);
		Assert.Equal("TestName", deserialized.Name);
		Assert.Null(deserialized.Description);
	}

	// ============================================================================
	// Collection Tests
	// ============================================================================

	[Fact]
	public void GeneratedConverter_HandlesCollectionProperty()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var items = new List<string> { "Alpha", "Beta", "Gamma" };
		var record = new RecordWithCollection("CollectionTest", items);

		// Act
		var json = JsonSerializer.Serialize(record, options);
		var deserialized = JsonSerializer.Deserialize<RecordWithCollection>(json, options);

		// Assert
		Assert.NotNull(deserialized);
		Assert.Equal("CollectionTest", deserialized.Name);
		Assert.Equal(items, deserialized.Items);
	}

	[Fact]
	public void GeneratedConverter_HandlesEmptyCollection()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var record = new RecordWithCollection("EmptyTest", new List<string>());

		// Act
		var json = JsonSerializer.Serialize(record, options);
		var deserialized = JsonSerializer.Deserialize<RecordWithCollection>(json, options);

		// Assert
		Assert.NotNull(deserialized);
		Assert.Equal("EmptyTest", deserialized.Name);
		Assert.Empty(deserialized.Items);
	}

	// ============================================================================
	// Nested Type Tests
	// ============================================================================

	[Fact]
	public void GeneratedConverter_HandlesNestedRecords()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var inner = new InnerRecord("InnerValue");
		var outer = new OuterRecord("OuterName", inner);

		// Act
		var json = JsonSerializer.Serialize(outer, options);
		var deserialized = JsonSerializer.Deserialize<OuterRecord>(json, options);

		// Assert
		Assert.NotNull(deserialized);
		Assert.Equal("OuterName", deserialized.Name);
		Assert.NotNull(deserialized.Inner);
		Assert.Equal("InnerValue", deserialized.Inner.InnerValue);
	}

	// ============================================================================
	// Round-Trip Tests
	// ============================================================================

	[Fact]
	public void GeneratedConverter_RoundTripsComplexRecord()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var now = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
		var guid = new Guid("12345678-1234-1234-1234-123456789012");
		var original = new ComplexRecord(
			"TestString",
			42,
			9876543210L,
			3.14159265359,
			123.45m,
			true,
			now,
			guid);

		// Act
		var json = JsonSerializer.Serialize(original, options);
		var deserialized = JsonSerializer.Deserialize<ComplexRecord>(json, options);

		// Assert
		Assert.NotNull(deserialized);
		Assert.Equal(original.StringProp, deserialized.StringProp);
		Assert.Equal(original.IntProp, deserialized.IntProp);
		Assert.Equal(original.LongProp, deserialized.LongProp);
		Assert.Equal(original.DoubleProp, deserialized.DoubleProp, precision: 10);
		Assert.Equal(original.DecimalProp, deserialized.DecimalProp);
		Assert.Equal(original.BoolProp, deserialized.BoolProp);
		Assert.Equal(original.DateTimeProp, deserialized.DateTimeProp);
		Assert.Equal(original.GuidProp, deserialized.GuidProp);
	}

	[Fact]
	public void GeneratedConverter_MaintainsValueEquality()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var now = DateTime.UtcNow;
		var original = new EqualityTestRecord("Test", 42, now);

		// Act
		var json = JsonSerializer.Serialize(original, options);
		var deserialized = JsonSerializer.Deserialize<EqualityTestRecord>(json, options);

		// Assert - Record value equality should hold after serialization
		Assert.Equal(original, deserialized);
	}

	// ============================================================================
	// Client/Server Round-Trip Tests
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

	[Theory]
	[MemberData(nameof(OrdinalFormat_Client))]
	[MemberData(nameof(OrdinalFormat_Local))]
	public async Task GeneratedConverter_ClientServerRoundTrip(IRemoteRecordFactory factory)
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
	public async Task GeneratedConverter_AsyncClientServerRoundTrip(IRemoteRecordFactory factory)
	{
		// Act
		var result = await factory.FetchRemoteAsync("async-test");

		// Assert
		Assert.NotNull(result);
		Assert.Equal("RemoteAsync-async-test", result.Name);
	}

	// ============================================================================
	// Error Handling Tests
	// ============================================================================

	[Fact]
	public void GeneratedConverter_ThrowsOnMalformedJson_WrongTokenType()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var json = "{\"Name\":\"Test\"}"; // Object instead of array

		// Act & Assert - should throw because ordinal format expects array
		var ex = Assert.Throws<JsonException>(() =>
			JsonSerializer.Deserialize<SimpleRecord>(json, options));
		Assert.Contains("StartArray", ex.Message);
	}

	[Fact]
	public void GeneratedConverter_ThrowsOnTooManyValues()
	{
		// Arrange
		var options = CreateOrdinalOptions();
		var json = "[\"Test\",42,\"extra\"]"; // Too many values

		// Act & Assert
		var ex = Assert.Throws<JsonException>(() =>
			JsonSerializer.Deserialize<SimpleRecord>(json, options));
		Assert.Contains("Too many values", ex.Message);
	}

	// ============================================================================
	// Helper Methods
	// ============================================================================

	private static JsonSerializerOptions CreateOrdinalOptions()
	{
		var options = new JsonSerializerOptions();
		var serializationOptions = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
		options.Converters.Add(new NeatooOrdinalConverterFactory(serializationOptions));
		return options;
	}
}
