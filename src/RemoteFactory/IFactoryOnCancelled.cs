namespace Neatoo.RemoteFactory;

/// <summary>
/// Implement this interface to receive a callback when a factory operation is cancelled.
/// Called when an OperationCanceledException is thrown during factory method execution.
/// </summary>
public interface IFactoryOnCancelled
{
	void FactoryCancelled(FactoryOperation factoryOperation);
}

/// <summary>
/// Implement this interface to receive an async callback when a factory operation is cancelled.
/// Called when an OperationCanceledException is thrown during factory method execution.
/// </summary>
public interface IFactoryOnCancelledAsync
{
	Task FactoryCancelledAsync(FactoryOperation factoryOperation);
}
