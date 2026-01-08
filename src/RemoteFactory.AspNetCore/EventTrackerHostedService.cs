using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory.AspNetCore;

/// <summary>
/// Hosted service that waits for all pending event tasks to complete during graceful shutdown.
/// </summary>
internal sealed class EventTrackerHostedService : IHostedService
{
	private readonly IEventTracker _eventTracker;
	private readonly ILogger<EventTrackerHostedService> _logger;
	private readonly TimeSpan _shutdownTimeout;

	public EventTrackerHostedService(
		IEventTracker eventTracker,
		IHostApplicationLifetime lifetime,
		ILogger<EventTrackerHostedService> logger,
		TimeSpan? shutdownTimeout = null)
	{
		_eventTracker = eventTracker;
		_logger = logger;
		_shutdownTimeout = shutdownTimeout ?? TimeSpan.FromSeconds(30);
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		// No startup work needed
		return Task.CompletedTask;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		var pendingCount = _eventTracker.PendingCount;

		if (pendingCount == 0)
		{
			_logger.NoPendingEventsAtShutdown();
			return;
		}

		_logger.WaitingForPendingEventsAtShutdown(pendingCount);

		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(_shutdownTimeout);

			await _eventTracker.WaitAllAsync(cts.Token).ConfigureAwait(false);

			_logger.AllPendingEventsCompleted();
		}
		catch (OperationCanceledException)
		{
			var remaining = _eventTracker.PendingCount;
			if (remaining > 0)
			{
				_logger.ShutdownTimeoutReached(remaining);
			}
		}
		catch (AggregateException ex)
		{
			_logger.ShutdownWaitError(ex);
		}
	}
}
