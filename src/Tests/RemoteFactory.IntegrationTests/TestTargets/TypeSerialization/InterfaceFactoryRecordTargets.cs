using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

// ============================================================================
// Plain record types (no [Factory] attribute) returned by Interface Factories.
// These types fall through to STJ built-in serialization and previously failed
// with ObjectWithParameterizedCtorRefMetadataNotSupported when $id/$ref
// metadata was emitted by NeatooJsonSerializer's ReferenceHandler.Preserve.
// ============================================================================

/// <summary>
/// Simple record with a primary constructor for basic round-trip testing.
/// </summary>
public record InterfaceRecordResult(string Name, int Value);

/// <summary>
/// Item record for collection-containing record tests.
/// </summary>
public record InterfaceRecordItem(int Id, string Description);

/// <summary>
/// Record containing a collection of other records -- the minimal repro
/// for the $ref deserialization bug with parameterized constructors.
/// </summary>
public record InterfaceRecordWithCollection(string Name, IReadOnlyList<InterfaceRecordItem> Items);

/// <summary>
/// Outer record containing a nested inner record.
/// </summary>
public record InterfaceNestedOuter(string Label, InterfaceNestedInner Child);

/// <summary>
/// Inner record used as a nested property.
/// </summary>
public record InterfaceNestedInner(int Id);

// ============================================================================
// Interface Factory that returns plain record types.
// The generator creates a proxy implementation that serializes calls
// across the client-server boundary.
// ============================================================================

/// <summary>
/// Interface Factory returning plain record types (no [Factory] on the records).
/// Methods return records with parameterized constructors that previously failed
/// deserialization when $id/$ref metadata was present.
/// </summary>
[Factory]
public interface IRecordReturnService
{
	Task<InterfaceRecordResult> GetSimpleRecord(string name, int value);
	Task<InterfaceRecordWithCollection> GetRecordWithCollection(string name);
	Task<InterfaceNestedOuter> GetNestedRecord(string label, int childId);
	Task<InterfaceRecordResult?> GetNullableRecord(bool returnNull);
}

// ============================================================================
// Server-side implementation of the Interface Factory.
// Only registered in the server container.
// ============================================================================

/// <summary>
/// Server implementation that creates and returns plain record instances.
/// </summary>
public class RecordReturnService : IRecordReturnService
{
	public Task<InterfaceRecordResult> GetSimpleRecord(string name, int value)
	{
		return Task.FromResult(new InterfaceRecordResult(name, value));
	}

	public Task<InterfaceRecordWithCollection> GetRecordWithCollection(string name)
	{
		var items = new List<InterfaceRecordItem>
		{
			new(1, "First"),
			new(2, "Second"),
			new(3, "Third")
		};
		return Task.FromResult(new InterfaceRecordWithCollection(name, items));
	}

	public Task<InterfaceNestedOuter> GetNestedRecord(string label, int childId)
	{
		return Task.FromResult(new InterfaceNestedOuter(label, new InterfaceNestedInner(childId)));
	}

	public Task<InterfaceRecordResult?> GetNullableRecord(bool returnNull)
	{
		if (returnNull)
		{
			return Task.FromResult<InterfaceRecordResult?>(null);
		}
		return Task.FromResult<InterfaceRecordResult?>(new InterfaceRecordResult("NotNull", 99));
	}
}
