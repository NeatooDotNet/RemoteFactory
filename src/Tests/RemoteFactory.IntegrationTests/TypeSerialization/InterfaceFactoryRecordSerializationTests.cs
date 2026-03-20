using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Tests that Interface Factory methods returning plain record types (no [Factory])
/// complete the full client-server-client round-trip. These records have parameterized
/// constructors and previously failed with ObjectWithParameterizedCtorRefMetadataNotSupported
/// when $id/$ref metadata was emitted by NeatooJsonSerializer.
/// </summary>
public class InterfaceFactoryRecordSerializationTests
{
	private readonly IServiceScope _clientScope;
	private readonly IServiceScope _serverScope;
	private readonly IRecordReturnService _factory;

	public InterfaceFactoryRecordSerializationTests()
	{
		var (client, server, _) = ClientServerContainers.Scopes(
			configureServer: services =>
			{
				services.AddScoped<IRecordReturnService, RecordReturnService>();
			});

		_clientScope = client;
		_serverScope = server;
		_factory = _clientScope.ServiceProvider.GetRequiredService<IRecordReturnService>();
	}

	// ============================================================================
	// Scenario 1: Simple record round-trip (Rules 1, 3)
	// ============================================================================

	[Fact]
	public async Task InterfaceFactory_SimpleRecord_RoundTrip()
	{
		// Act - call through client, serializes to server and back
		var result = await _factory.GetSimpleRecord("TestName", 42);

		// Assert - record deserialized with all properties intact
		Assert.NotNull(result);
		Assert.Equal("TestName", result.Name);
		Assert.Equal(42, result.Value);
	}

	// ============================================================================
	// Scenario 2: Record with collection round-trip (Rules 1, 3, 4)
	// ============================================================================

	[Fact]
	public async Task InterfaceFactory_RecordWithCollection_RoundTrip()
	{
		// Act
		var result = await _factory.GetRecordWithCollection("CollectionTest");

		// Assert - record deserialized with collection containing all items
		Assert.NotNull(result);
		Assert.Equal("CollectionTest", result.Name);
		Assert.NotNull(result.Items);
		Assert.Equal(3, result.Items.Count);
		Assert.Equal(1, result.Items[0].Id);
		Assert.Equal("First", result.Items[0].Description);
		Assert.Equal(2, result.Items[1].Id);
		Assert.Equal("Second", result.Items[1].Description);
		Assert.Equal(3, result.Items[2].Id);
		Assert.Equal("Third", result.Items[2].Description);
	}

	// ============================================================================
	// Scenario 3: Nested record round-trip (Rules 1, 3)
	// ============================================================================

	[Fact]
	public async Task InterfaceFactory_NestedRecord_RoundTrip()
	{
		// Act
		var result = await _factory.GetNestedRecord("OuterLabel", 7);

		// Assert - both records deserialized with all properties intact
		Assert.NotNull(result);
		Assert.Equal("OuterLabel", result.Label);
		Assert.NotNull(result.Child);
		Assert.Equal(7, result.Child.Id);
	}

	// ============================================================================
	// Scenario 4: Nullable record returns null (Rule 5)
	// ============================================================================

	[Fact]
	public async Task InterfaceFactory_NullableRecord_ReturnsNull()
	{
		// Act
		var result = await _factory.GetNullableRecord(returnNull: true);

		// Assert - client receives null without error
		Assert.Null(result);
	}

	[Fact]
	public async Task InterfaceFactory_NullableRecord_ReturnsValue()
	{
		// Act
		var result = await _factory.GetNullableRecord(returnNull: false);

		// Assert - non-null record deserialized correctly
		Assert.NotNull(result);
		Assert.Equal("NotNull", result.Name);
		Assert.Equal(99, result.Value);
	}

	// ============================================================================
	// Scenario 8: JSON output for non-Neatoo types has no $id/$ref (Rule 1)
	// ============================================================================

	[Fact]
	public void InterfaceFactory_NonNeatooType_NoRefMetadata()
	{
		// Arrange - get the serializer from the server container
		var serializer = _serverScope.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();
		var record = new InterfaceRecordWithCollection(
			"MetadataTest",
			new List<InterfaceRecordItem>
			{
				new(1, "Item1"),
				new(2, "Item2")
			});

		// Act - serialize via the serializer that now selects PlainOptions for non-Neatoo types
		var json = serializer.Serialize(record);

		// Assert - JSON must not contain $id or $ref metadata
		Assert.NotNull(json);
		Assert.DoesNotContain("$id", json);
		Assert.DoesNotContain("$ref", json);
	}
}
