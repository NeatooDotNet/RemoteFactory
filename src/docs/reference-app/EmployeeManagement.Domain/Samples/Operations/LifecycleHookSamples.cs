using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-lifecycle-onstart
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
    public EmployeeLifecycleOnStart()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Called before any factory operation executes.
    /// Use for pre-operation validation and preparation.
    /// </summary>
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        OnStartCalled = true;
        LastOperation = factoryOperation;

        // Validate: cannot delete an employee with empty ID
        if (factoryOperation == FactoryOperation.Delete && Id == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot delete an employee that has not been saved.");
        }
    }
}
#endregion

#region operations-lifecycle-oncomplete
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
    public EmployeeLifecycleOnComplete()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Called after factory operation completes successfully.
    /// Use for post-operation logic: audit logging, cache invalidation, notifications, etc.
    /// </summary>
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        OnCompleteCalled = true;
        CompletedOperation = factoryOperation;

        // Post-operation logic: logging, notifications, etc.
    }
}
#endregion

#region operations-lifecycle-oncancelled
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
    public EmployeeLifecycleOnCancelled()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Called when factory operation is cancelled via CancellationToken.
    /// Use for cleanup logic when operation was cancelled.
    /// </summary>
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        OnCancelledCalled = true;
        CancelledOperation = factoryOperation;

        // Cleanup logic when operation was cancelled
    }
}
#endregion
