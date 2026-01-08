using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Tracks pending asynchronous event tasks for fire-and-forget operations.
/// Logs exceptions from failed tasks without re-throwing.
/// </summary>
internal sealed class EventTracker : IEventTracker
{
	private readonly ILogger<EventTracker> _logger;
	private readonly ConcurrentDictionary<int, Task> _pendingTasks = new();
	private int _taskIdCounter;

	public EventTracker(ILogger<EventTracker> logger)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	public int PendingCount => _pendingTasks.Count;

	/// <inheritdoc />
	public void Track(Task eventTask)
	{
		if (eventTask.IsCompleted)
		{
			// Task already completed - log any exception but don't track
			if (eventTask.IsFaulted)
			{
				LogTaskException(eventTask);
			}
			return;
		}

		var taskId = Interlocked.Increment(ref _taskIdCounter);
		_pendingTasks.TryAdd(taskId, eventTask);

		// Cleanup when task completes
		eventTask.ContinueWith(
			t =>
			{
				_pendingTasks.TryRemove(taskId, out _);

				if (t.IsFaulted)
				{
					LogTaskException(t);
				}
			},
			TaskScheduler.Default);
	}

	/// <inheritdoc />
	public async Task WaitAllAsync(CancellationToken ct = default)
	{
		var tasks = _pendingTasks.Values.ToArray();

		if (tasks.Length == 0)
		{
			return;
		}

		_logger.WaitingForPendingEvents(tasks.Length);

		try
		{
			await Task.WhenAll(tasks).WaitAsync(ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			_logger.PendingEventsCancelled(_pendingTasks.Count);
			throw;
		}
		catch (AggregateException ex)
		{
			// Log aggregate exception but don't throw - we've already logged individual exceptions
			_logger.PendingEventsShutdownFailed(ex);
		}
	}

	private void LogTaskException(Task task)
	{
		var exception = task.Exception?.Flatten().InnerException ?? task.Exception;

		if (exception != null)
		{
			_logger.EventHandlerFailed(exception);
		}
	}
}
