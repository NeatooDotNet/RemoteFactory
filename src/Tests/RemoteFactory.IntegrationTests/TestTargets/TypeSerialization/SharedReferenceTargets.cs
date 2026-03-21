namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

// ============================================================================
// Plain types (no [Factory] attribute) for testing shared reference behavior
// in NeatooJsonSerializer. These types are serialized directly, not through
// factory round-trips, to isolate serialization behavior from factory plumbing.
// ============================================================================

/// <summary>
/// Plain class with two Dictionary properties for testing shared-reference identity.
/// When the same Dictionary instance is assigned to both properties, serialization
/// should ideally preserve that identity (ReferenceEquals is true after round-trip).
/// </summary>
public class SharedDictionaryHolder
{
	public Dictionary<string, string> DictionaryA { get; set; } = new();
	public Dictionary<string, string> DictionaryB { get; set; } = new();
}

/// <summary>
/// Plain class with Name and Next properties for testing circular reference handling.
/// A circular graph (a.Next = b, b.Next = a) requires reference tracking to serialize
/// without infinite recursion.
/// </summary>
public class CircularNode
{
	public string Name { get; set; } = "";
	public CircularNode? Next { get; set; }
}

/// <summary>
/// Wrapper that holds two references to the same record instance.
/// Used to test whether STJ emits $ref for records with parameterized constructors
/// when ReferenceHandler.Preserve is active. The $ref triggers
/// ObjectWithParameterizedCtorRefMetadataNotSupported on deserialization.
/// </summary>
public class SharedRecordHolder
{
	public InterfaceRecordWithCollection? RecordA { get; set; }
	public InterfaceRecordWithCollection? RecordB { get; set; }
}

/// <summary>
/// Plain class with both scalar properties and two shared Dictionary references.
/// Used for Scenario 10 to verify reference tracking works correctly in a
/// graph that mixes Dictionary and scalar properties.
/// </summary>
public class CrossTypeSharedReferenceHolder
{
	public string Name { get; set; } = "";
	public int Count { get; set; }
	public Dictionary<string, string>? DictionaryA { get; set; }
	public Dictionary<string, string>? DictionaryB { get; set; }
}

/// <summary>
/// Plain class containing both a record (immutable, parameterized constructor)
/// and two shared Dictionary references (mutable). Used for Scenario 11 to
/// verify that records and shared mutable references coexist in the same graph.
/// </summary>
public class RecordWithSharedDictionaryHolder
{
	public InterfaceRecordItem? Record { get; set; }
	public Dictionary<string, string>? DictionaryA { get; set; }
	public Dictionary<string, string>? DictionaryB { get; set; }
}
