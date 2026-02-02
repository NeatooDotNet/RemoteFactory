using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

/// <summary>
/// Employee aggregate implementing IFactoryOnStart for pre-operation validation.
/// </summary>
[Factory]
public partial class EmployeeLifecycleOnStart : IFactoryOnStart
{
    public Guid Id { get; private set; }
    public bool OnStartCalled { get; private set; }
    public FactoryOperation? LastOperation { get; private set; }

    [Create]
    public EmployeeLifecycleOnStart() => Id = Guid.NewGuid();

    #region operations-lifecycle-onstart
    // IFactoryOnStart - called before any factory operation executes
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        OnStartCalled = true;
        LastOperation = factoryOperation;
        if (factoryOperation == FactoryOperation.Delete && Id == Guid.Empty)
            throw new InvalidOperationException("Cannot delete unsaved employee.");
    }
    #endregion
}

/// <summary>
/// Employee aggregate implementing IFactoryOnComplete for post-operation processing.
/// </summary>
[Factory]
public partial class EmployeeLifecycleOnComplete : IFactoryOnComplete
{
    public Guid Id { get; private set; }
    public bool OnCompleteCalled { get; private set; }
    public FactoryOperation? CompletedOperation { get; private set; }

    [Create]
    public EmployeeLifecycleOnComplete() => Id = Guid.NewGuid();

    #region operations-lifecycle-oncomplete
    // IFactoryOnComplete - called after operation succeeds (audit, cache invalidation, etc.)
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        OnCompleteCalled = true;
        CompletedOperation = factoryOperation;
    }
    #endregion
}

/// <summary>
/// Employee aggregate implementing IFactoryOnCancelled for cancellation handling.
/// </summary>
[Factory]
public partial class EmployeeLifecycleOnCancelled : IFactoryOnCancelled
{
    public Guid Id { get; private set; }
    public bool OnCancelledCalled { get; private set; }
    public FactoryOperation? CancelledOperation { get; private set; }

    [Create]
    public EmployeeLifecycleOnCancelled() => Id = Guid.NewGuid();

    #region operations-lifecycle-oncancelled
    // IFactoryOnCancelled - called when operation cancelled via CancellationToken
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        OnCancelledCalled = true;
        CancelledOperation = factoryOperation;
    }
    #endregion
}
