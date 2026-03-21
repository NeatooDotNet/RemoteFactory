using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Phase 1 exploration tests for shared reference handling.
/// These tests reproduce the three problem scenarios (shared identity, records,
/// circular references) using ONLY RemoteFactory's serialization infrastructure
/// to determine whether the issues are inherent STJ limitations or caused by
/// the current NeatooJsonSerializer configuration.
///
/// Each test documents what it proves and whether the result was expected.
/// These tests become the baseline for Phase 2 acceptance testing.
/// </summary>
public class SharedReferenceExplorationTests
{
	// ============================================================================
	// Scenario 1: Shared Dictionary -- identity preserved
	//
	// Rule 7: WHEN a mutable reference type (Dictionary) is assigned to two
	// properties and serialized through NeatooJsonSerializer, THEN after
	// deserialization the two properties reference the same object instance.
	//
	// Originally documented the broken behavior (identity lost). After wiring
	// NeatooPreserveReferenceHandler + RecordBypassConverterFactory, shared
	// identity is now preserved for mutable types.
	// ============================================================================

	[Fact]
	public void Scenario1_SharedDictionary_IdentityPreserved()
	{
		// Arrange -- same Dictionary instance assigned to both properties
		var scopes = ClientServerContainers.Scopes();
		var serializer = scopes.server.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();

		var sharedDict = new Dictionary<string, string>
		{
			["key1"] = "value1",
			["key2"] = "value2"
		};

		var holder = new SharedDictionaryHolder
		{
			DictionaryA = sharedDict,
			DictionaryB = sharedDict
		};

		// Verify the setup: same instance before serialization
		Assert.True(ReferenceEquals(holder.DictionaryA, holder.DictionaryB),
			"Setup: DictionaryA and DictionaryB should be the same instance before serialization");

		// Act -- serialize and deserialize through NeatooJsonSerializer (with ReferenceHandler)
		var json = serializer.Serialize(holder);
		var result = serializer.Deserialize<SharedDictionaryHolder>(json!);

		// Assert -- shared identity IS preserved via NeatooPreserveReferenceHandler
		Assert.NotNull(result);
		Assert.True(ReferenceEquals(result.DictionaryA, result.DictionaryB),
			"Shared dictionary identity should be preserved after round-trip " +
			"because NeatooJsonSerializer now has NeatooPreserveReferenceHandler on options");

		// Both dictionaries should still have the correct values
		Assert.Equal(2, result.DictionaryA!.Count);
		Assert.Equal("value1", result.DictionaryA["key1"]);
		Assert.Equal("value2", result.DictionaryA["key2"]);
	}

	// ============================================================================
	// Scenario 2: Record round-trip -- current behavior (no ReferenceHandler)
	//
	// Rule 2: WHEN NeatooJsonSerializer serializes a record with a parameterized
	// constructor (no ReferenceHandler), THEN deserialization succeeds.
	//
	// This proves records work correctly in the current configuration.
	// ============================================================================

	[Fact]
	public void Scenario2_RecordRoundTrip_CurrentBehavior_Succeeds()
	{
		// Arrange
		var scopes = ClientServerContainers.Scopes();
		var serializer = scopes.server.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();

		var items = new List<InterfaceRecordItem>
		{
			new(1, "First"),
			new(2, "Second"),
			new(3, "Third")
		};
		var record = new InterfaceRecordWithCollection("Test", items);

		// Act -- serialize and deserialize through NeatooJsonSerializer
		var json = serializer.Serialize(record);
		var result = serializer.Deserialize<InterfaceRecordWithCollection>(json!);

		// Assert -- record deserialized successfully with all properties intact
		Assert.NotNull(result);
		Assert.Equal("Test", result.Name);
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
	// Scenario 3: Record with ReferenceHandler.Preserve -- STJ limitation
	//
	// Rule 3: WHEN ReferenceHandler.Preserve is set AND the same record instance
	// is assigned to two properties (forcing $ref emission), THEN STJ throws
	// NotSupportedException on deserialization.
	//
	// Per developer review concern C1: the record must be referenced TWICE to
	// trigger $ref emission. A single-occurrence record only gets $id, which
	// does not cause the exception.
	//
	// Uses bare JsonSerializerOptions (NOT NeatooJsonSerializer) to test STJ
	// behavior in isolation.
	// ============================================================================

	[Fact]
	public void Scenario3_RecordWithReferenceHandlerPreserve_ThrowsOnDeserialization()
	{
		// Arrange -- bare STJ options with ReferenceHandler.Preserve
		var options = new JsonSerializerOptions
		{
			ReferenceHandler = ReferenceHandler.Preserve,
			WriteIndented = true
		};

		var items = new List<InterfaceRecordItem>
		{
			new(1, "First"),
			new(2, "Second")
		};
		var sharedRecord = new InterfaceRecordWithCollection("Shared", items);

		// The same record instance assigned to BOTH properties forces STJ to emit
		// $id on the first occurrence and $ref on the second occurrence.
		var holder = new SharedRecordHolder
		{
			RecordA = sharedRecord,
			RecordB = sharedRecord
		};

		// Act -- serialize succeeds (STJ writes $id and $ref)
		var json = JsonSerializer.Serialize(holder, options);

		// Verify $ref is in the JSON (confirms the record IS being shared)
		Assert.Contains("$ref", json);

		// Assert -- deserialization throws because STJ cannot construct the
		// immutable record from a $ref pointer.
		// The internal STJ error is ObjectWithParameterizedCtorRefMetadataNotSupported,
		// but the exception message is the user-facing string.
		var ex = Assert.Throws<NotSupportedException>(() =>
			JsonSerializer.Deserialize<SharedRecordHolder>(json, options));

		Assert.Contains("Reference metadata is not supported when deseriali", ex.Message);
	}

	// ============================================================================
	// Scenario 4: Circular reference -- handled
	//
	// Rule 9: WHEN a mutable reference type has a circular reference and is
	// serialized through NeatooJsonSerializer, THEN the circular reference is
	// preserved after deserialization.
	//
	// Originally documented the broken behavior (JsonException thrown). After
	// wiring NeatooPreserveReferenceHandler, circular references are now
	// handled via $id/$ref tracking.
	// ============================================================================

	[Fact]
	public void Scenario4_CircularReference_Handled()
	{
		// Arrange -- create a circular graph: a.Next = b, b.Next = a
		var scopes = ClientServerContainers.Scopes();
		var serializer = scopes.server.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();

		var nodeA = new CircularNode { Name = "A" };
		var nodeB = new CircularNode { Name = "B" };
		nodeA.Next = nodeB;
		nodeB.Next = nodeA;

		// Act -- serialize and deserialize through NeatooJsonSerializer (with ReferenceHandler)
		var json = serializer.Serialize(nodeA);
		var result = serializer.Deserialize<CircularNode>(json!);

		// Assert -- circular reference is preserved
		Assert.NotNull(result);
		Assert.Equal("A", result.Name);
		Assert.NotNull(result.Next);
		Assert.Equal("B", result.Next.Name);
		Assert.NotNull(result.Next.Next);
		Assert.True(ReferenceEquals(result, result.Next.Next),
			"Circular reference should be preserved: a.Next.Next should be the same instance as a");
	}

	// ============================================================================
	// Scenario 5: Shared Dictionary with ReferenceHandler.Preserve
	//
	// Rule 5: WHEN a Dictionary is assigned to two properties AND
	// ReferenceHandler.Preserve is set on bare JsonSerializerOptions,
	// THEN ReferenceEquals returns true after deserialization.
	//
	// Uses bare JsonSerializerOptions (NOT NeatooJsonSerializer) to confirm
	// that STJ's ReferenceHandler.Preserve correctly handles shared Dictionary
	// identity. This is the "happy path" that Phase 2 aims to integrate.
	// ============================================================================

	[Fact]
	public void Scenario5_SharedDictionary_ReferenceHandlerPreserve_IdentityPreserved()
	{
		// Arrange -- bare STJ options with ReferenceHandler.Preserve
		var options = new JsonSerializerOptions
		{
			ReferenceHandler = ReferenceHandler.Preserve,
			WriteIndented = true
		};

		var sharedDict = new Dictionary<string, string>
		{
			["key1"] = "value1",
			["key2"] = "value2"
		};

		var holder = new SharedDictionaryHolder
		{
			DictionaryA = sharedDict,
			DictionaryB = sharedDict
		};

		// Verify the setup
		Assert.True(ReferenceEquals(holder.DictionaryA, holder.DictionaryB));

		// Act -- serialize and deserialize with ReferenceHandler.Preserve
		var json = JsonSerializer.Serialize(holder, options);
		var result = JsonSerializer.Deserialize<SharedDictionaryHolder>(json, options);

		// Assert -- shared identity IS preserved
		Assert.NotNull(result);
		Assert.True(ReferenceEquals(result.DictionaryA, result.DictionaryB),
			"ReferenceHandler.Preserve correctly preserves shared dictionary identity");

		// Values are intact
		Assert.Equal(2, result.DictionaryA.Count);
		Assert.Equal("value1", result.DictionaryA["key1"]);
		Assert.Equal("value2", result.DictionaryA["key2"]);
	}

	// ============================================================================
	// Scenario 6: Custom ReferenceHandler delegating to NeatooReferenceResolver
	//
	// Rule 6: WHEN options.ReferenceHandler uses a custom ReferenceHandler
	// subclass that delegates CreateResolver() to NeatooReferenceResolver.Current,
	// THEN STJ's built-in converters emit $id/$ref and shared identity is preserved.
	//
	// This validates the Phase 2 approach: a custom ReferenceHandler that bridges
	// STJ's API to the existing NeatooReferenceResolver infrastructure.
	//
	// The test manually manages the NeatooReferenceResolver lifecycle (normally
	// done by NeatooJsonSerializer) to isolate the custom handler behavior.
	// ============================================================================

	[Fact]
	public void Scenario6_CustomReferenceHandler_NeatooReferenceResolver_IdentityPreserved()
	{
		// Arrange -- custom ReferenceHandler that delegates to NeatooReferenceResolver.Current
		var options = new JsonSerializerOptions
		{
			ReferenceHandler = new TestNeatooPreserveReferenceHandler(),
			WriteIndented = true
		};

		var sharedDict = new Dictionary<string, string>
		{
			["key1"] = "value1",
			["key2"] = "value2"
		};

		var holder = new SharedDictionaryHolder
		{
			DictionaryA = sharedDict,
			DictionaryB = sharedDict
		};

		Assert.True(ReferenceEquals(holder.DictionaryA, holder.DictionaryB));

		// Act -- manually manage the resolver lifecycle (as NeatooJsonSerializer would)
		// Serialize
		string json;
		using (var rr = new NeatooReferenceResolver())
		{
			NeatooReferenceResolver.Current = rr;
			try
			{
				json = JsonSerializer.Serialize(holder, options);
			}
			finally
			{
				NeatooReferenceResolver.Current = null;
			}
		}

		// Verify $ref is in the JSON (the second dictionary reference)
		Assert.Contains("$ref", json);

		// Deserialize -- requires a fresh resolver
		SharedDictionaryHolder? result;
		using (var rr = new NeatooReferenceResolver())
		{
			NeatooReferenceResolver.Current = rr;
			try
			{
				result = JsonSerializer.Deserialize<SharedDictionaryHolder>(json, options);
			}
			finally
			{
				NeatooReferenceResolver.Current = null;
			}
		}

		// Assert -- shared identity IS preserved through the custom handler
		Assert.NotNull(result);
		Assert.True(ReferenceEquals(result.DictionaryA, result.DictionaryB),
			"Custom ReferenceHandler delegating to NeatooReferenceResolver preserves shared identity");

		// Values are intact
		Assert.Equal(2, result.DictionaryA.Count);
		Assert.Equal("value1", result.DictionaryA["key1"]);
		Assert.Equal("value2", result.DictionaryA["key2"]);
	}

	// ============================================================================
	// Temporary custom ReferenceHandler for Phase 1 exploration (Scenario 6).
	// In Phase 2, this becomes NeatooPreserveReferenceHandler in the production code.
	// ============================================================================

	private sealed class TestNeatooPreserveReferenceHandler : ReferenceHandler
	{
		public override ReferenceResolver CreateResolver()
		{
			return NeatooReferenceResolver.Current
				?? throw new InvalidOperationException(
					"NeatooReferenceResolver.Current is null. " +
					"A NeatooReferenceResolver must be created and set as Current before serialization.");
		}
	}
}
