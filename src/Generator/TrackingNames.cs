namespace Neatoo;

/// <summary>
/// Names attached to the incremental pipeline's provider nodes via
/// <c>WithTrackingName</c>. Roslyn only records tracked steps when a driver is
/// created with <c>trackIncrementalGeneratorSteps: true</c>, so these cost
/// nothing in a normal build — they exist so the caching regression test can
/// identify which branch lost its cache rather than reporting one anonymous
/// aggregate failure.
/// </summary>
/// <remarks>
/// The caching guard (RemoteFactory.UnitTests IncrementalCacheTests) mirrors
/// these values as string literals and asserts every one of them is present in
/// the tracked-step set, so renaming a constant here without updating the test
/// fails loudly instead of silently dropping a branch from the guard.
/// </remarks>
internal static class TrackingNames
{
	/// <summary>[Factory] class and record declarations.</summary>
	public const string FactoryClass = "FactoryClass";

	/// <summary>[Factory] interface declarations.</summary>
	public const string FactoryInterface = "FactoryInterface";

	/// <summary>[FactoryEventHandler&lt;T&gt;] classes.</summary>
	public const string RelayHandler = "RelayHandler";

	/// <summary>Collected FactoryEventBase descendants for the preservation registrar.</summary>
	public const string FactoryEvents = "FactoryEvents";
}
