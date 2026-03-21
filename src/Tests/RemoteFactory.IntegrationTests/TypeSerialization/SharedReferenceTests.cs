using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Phase 2 acceptance tests for shared reference handling after the fix.
/// These tests validate that NeatooJsonSerializer (with NeatooPreserveReferenceHandler)
/// preserves shared-instance identity for non-custom types, handles circular
/// references, and does not break record deserialization.
///
/// Each test corresponds to a scenario in the plan's Test Scenarios table.
/// </summary>
public class SharedReferenceTests
{
	// ============================================================================
	// Scenario 7: Shared Dictionary -- after fix
	//
	// Rule 7: WHEN a mutable reference type (Dictionary) is assigned to two
	// properties and serialized/deserialized through NeatooJsonSerializer,
	// THEN after deserialization the two properties reference the same object
	// instance (ReferenceEquals returns true).
	//
	// This is the primary acceptance test: the fix restores shared identity.
	// ============================================================================

	[Fact]
	public void Scenario7_SharedDictionary_AfterFix_IdentityPreserved()
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

		// Act -- serialize and deserialize through NeatooJsonSerializer (now with ReferenceHandler)
		var json = serializer.Serialize(holder);
		var result = serializer.Deserialize<SharedDictionaryHolder>(json!);

		// Assert -- shared identity IS preserved after the fix
		Assert.NotNull(result);
		Assert.True(ReferenceEquals(result.DictionaryA, result.DictionaryB),
			"After fix: shared dictionary identity should be preserved after round-trip " +
			"because NeatooJsonSerializer now has NeatooPreserveReferenceHandler on options");

		// Values are intact
		Assert.Equal(2, result.DictionaryA!.Count);
		Assert.Equal("value1", result.DictionaryA["key1"]);
		Assert.Equal("value2", result.DictionaryA["key2"]);
	}

	// ============================================================================
	// Scenario 8: Record round-trip -- after fix
	//
	// Rule 8: WHEN a record type with a parameterized constructor is
	// serialized/deserialized through NeatooJsonSerializer, THEN deserialization
	// succeeds without error and all property values are preserved.
	//
	// Per Phase 1 Finding F3: STJ adds $id to records (does NOT skip them).
	// A single-occurrence record with $id deserializes correctly. Only shared
	// records (same instance in two properties, triggering $ref) would fail.
	// This test verifies the common case: a single-occurrence record.
	// ============================================================================

	[Fact]
	public void Scenario8_RecordRoundTrip_AfterFix_Succeeds()
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

		// Act -- serialize and deserialize through NeatooJsonSerializer (now with ReferenceHandler)
		var json = serializer.Serialize(record);
		var result = serializer.Deserialize<InterfaceRecordWithCollection>(json!);

		// Assert -- record deserialized successfully with all properties intact
		// even though STJ now adds $id metadata to the record
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
	// Scenario 9: Circular reference -- after fix
	//
	// Rule 9: WHEN a mutable reference type has a circular reference
	// (a.Next = b, b.Next = a) AND is serialized/deserialized through
	// NeatooJsonSerializer, THEN the circular reference is preserved after
	// deserialization (a.Next.Next is the same instance as a).
	//
	// Before the fix, this threw JsonException (max depth exceeded).
	// Now with ReferenceHandler, STJ detects cycles via the resolver.
	// ============================================================================

	[Fact]
	public void Scenario9_CircularReference_AfterFix_IdentityPreserved()
	{
		// Arrange -- create a circular graph: a.Next = b, b.Next = a
		var scopes = ClientServerContainers.Scopes();
		var serializer = scopes.server.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();

		var nodeA = new CircularNode { Name = "A" };
		var nodeB = new CircularNode { Name = "B" };
		nodeA.Next = nodeB;
		nodeB.Next = nodeA;

		// Act -- serialize and deserialize through NeatooJsonSerializer
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
	// Scenario 10: Cross-type shared reference
	//
	// Rule 10: WHEN a Neatoo type with a custom converter is serialized alongside
	// non-custom types in the same object graph, THEN both use the SAME
	// NeatooReferenceResolver instance.
	//
	// Neatoo custom converters (NeatooBaseJsonTypeConverter, NeatooListBaseJsonTypeConverter)
	// are in the Neatoo repository, not RemoteFactory. We cannot test a true
	// cross-converter-type scenario here. Instead, we verify that the serializer
	// preserves shared Dictionary identity in a graph that also contains other
	// property types (strings, ints), confirming the resolver handles mixed-type
	// graphs correctly. The full cross-converter test requires the downstream
	// Neatoo repository.
	//
	// This test uses a holder that has both a shared Dictionary AND additional
	// scalar properties to verify the reference tracking does not interfere
	// with normal property serialization.
	// ============================================================================

	[Fact]
	public void Scenario10_CrossTypeSharedReference_DictionaryWithMixedProperties()
	{
		// Arrange -- a graph with both a shared Dictionary and scalar properties
		var scopes = ClientServerContainers.Scopes();
		var serializer = scopes.server.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();

		var sharedDict = new Dictionary<string, string>
		{
			["key1"] = "value1",
			["key2"] = "value2"
		};

		var holder = new CrossTypeSharedReferenceHolder
		{
			Name = "TestHolder",
			Count = 42,
			DictionaryA = sharedDict,
			DictionaryB = sharedDict
		};

		Assert.True(ReferenceEquals(holder.DictionaryA, holder.DictionaryB));

		// Act -- serialize and deserialize
		var json = serializer.Serialize(holder);
		var result = serializer.Deserialize<CrossTypeSharedReferenceHolder>(json!);

		// Assert -- shared Dictionary identity preserved AND scalar properties intact
		Assert.NotNull(result);
		Assert.Equal("TestHolder", result.Name);
		Assert.Equal(42, result.Count);
		Assert.True(ReferenceEquals(result.DictionaryA, result.DictionaryB),
			"Shared dictionary identity should be preserved in a mixed-property graph");
		Assert.Equal(2, result.DictionaryA!.Count);
		Assert.Equal("value1", result.DictionaryA["key1"]);
	}

	// ============================================================================
	// Scenario 11: Record in graph with shared mutable refs
	//
	// Rule 11: WHEN a record type with a parameterized constructor appears in a
	// graph with shared mutable references, THEN the record itself deserializes
	// correctly AND the mutable reference identity is preserved.
	//
	// Per Phase 1 Finding F3: STJ adds $id to records but does NOT skip them.
	// This test verifies that a single-occurrence record coexists safely with
	// shared mutable references in the same serialization graph.
	// ============================================================================

	[Fact]
	public void Scenario11_RecordInGraphWithSharedMutableRefs()
	{
		// Arrange -- a graph containing a record AND a shared Dictionary
		var scopes = ClientServerContainers.Scopes();
		var serializer = scopes.server.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();

		var sharedDict = new Dictionary<string, string>
		{
			["key1"] = "value1"
		};

		var record = new InterfaceRecordItem(1, "TestItem");

		var holder = new RecordWithSharedDictionaryHolder
		{
			Record = record,
			DictionaryA = sharedDict,
			DictionaryB = sharedDict
		};

		Assert.True(ReferenceEquals(holder.DictionaryA, holder.DictionaryB));

		// Act -- serialize and deserialize
		var json = serializer.Serialize(holder);
		var result = serializer.Deserialize<RecordWithSharedDictionaryHolder>(json!);

		// Assert -- record deserializes correctly
		Assert.NotNull(result);
		Assert.NotNull(result.Record);
		Assert.Equal(1, result.Record.Id);
		Assert.Equal("TestItem", result.Record.Description);

		// Assert -- shared Dictionary identity is preserved
		Assert.True(ReferenceEquals(result.DictionaryA, result.DictionaryB),
			"Shared dictionary identity should be preserved even when a record is in the same graph");
		Assert.NotNull(result.DictionaryA);
		Assert.Single(result.DictionaryA);
		Assert.Equal("value1", result.DictionaryA["key1"]);
	}
}
