namespace Neatoo.RemoteFactory;

/// <summary>
/// Tracks pending asynchronous event tasks for fire-and-forget operations.
/// Enables graceful shutdown by waiting for all pending events to complete.
/// </summary>
public interface IEventTracker
{
	/// <summary>
	/// Tracks a fire-and-forget event task for monitoring.
	/// Failed tasks are logged but not re-thrown.
	/// </summary>
	/// <param name="eventTask">The task to track.</param>
	void Track(Task eventTask);

	/// <summary>
	/// Waits for all pending event tasks to complete.
	/// Used during application shutdown to ensure all events finish processing.
	/// </summary>
	/// <param name="ct">Cancellation token to abort waiting.</param>
	/// <returns>A task that completes when all pending events have finished.</returns>
	Task WaitAllAsync(CancellationToken ct = default);

	/// <summary>
	/// Gets the number of pending event tasks.
	/// </summary>
	int PendingCount { get; }
}
