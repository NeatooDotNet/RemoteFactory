using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Neatoo.RemoteFactory.Internal;


public interface IMakeRemoteDelegateRequest
{
	Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken);
	Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken);

	/// <summary>
	/// Sends an event to the server and awaits until every server-side
	/// <c>[FactoryEventHandler&lt;T&gt;]</c> handler has completed. The HTTP connection stays
	/// open for the full handler chain so a server handler exception surfaces to the client.
	/// </summary>
	Task ForDelegateEvent(Type delegateType, object?[]? parameters, CancellationToken cancellationToken);

	// Backward-compatible overloads without CancellationToken
	Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters)
		=> ForDelegate<T>(delegateType, parameters, default);
	Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters)
		=> ForDelegateNullable<T>(delegateType, parameters, default);
	Task ForDelegateEvent(Type delegateType, object?[]? parameters)
		=> ForDelegateEvent(delegateType, parameters, default);
}

public class MakeRemoteDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly MakeRemoteDelegateRequestHttpCall MakeRemoteDelegateRequestCall;
	private readonly ICorrelationContext _correlationContext;
	private readonly IFactoryEventRelay? _relay;
	private readonly ILogger<MakeRemoteDelegateRequest> logger;

	public MakeRemoteDelegateRequest(
		INeatooJsonSerializer neatooJsonSerializer,
		MakeRemoteDelegateRequestHttpCall sendRemoteDelegateRequestToServer,
		ICorrelationContext correlationContext)
		: this(neatooJsonSerializer, sendRemoteDelegateRequestToServer, correlationContext, null, null)
	{
	}

	public MakeRemoteDelegateRequest(
		INeatooJsonSerializer neatooJsonSerializer,
		MakeRemoteDelegateRequestHttpCall sendRemoteDelegateRequestToServer,
		ICorrelationContext correlationContext,
		IFactoryEventRelay? relay)
		: this(neatooJsonSerializer, sendRemoteDelegateRequestToServer, correlationContext, relay, null)
	{
	}

	public MakeRemoteDelegateRequest(
		INeatooJsonSerializer neatooJsonSerializer,
		MakeRemoteDelegateRequestHttpCall sendRemoteDelegateRequestToServer,
		ICorrelationContext correlationContext,
		IFactoryEventRelay? relay,
		ILogger<MakeRemoteDelegateRequest>? logger)
	{
		this.NeatooJsonSerializer = neatooJsonSerializer;
		this.MakeRemoteDelegateRequestCall = sendRemoteDelegateRequestToServer;
		_correlationContext = correlationContext;
		_relay = relay;
		this.logger = logger ?? NullLogger<MakeRemoteDelegateRequest>.Instance;
	}

	public async Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
	{
		var result = await this.ForDelegateNullable<T>(delegateType, parameters, cancellationToken);

		if (result == null)
		{
			throw new InvalidOperationException($"The result of the remote delegate call was null, but a non-nullable type was expected.");
		}

		return result;
	}

	public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(delegateType);

		var correlationId = EnsureCorrelationId();
		var delegateTypeName = delegateType.Name;

		logger.RemoteCallStarted(correlationId, delegateTypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			var remoteDelegateRequest = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

			var result = await this.MakeRemoteDelegateRequestCall(remoteDelegateRequest, cancellationToken);

			sw.Stop();
			logger.RemoteCallCompleted(correlationId, delegateTypeName, sw.ElapsedMilliseconds);

			if (result == null)
			{
				return default;
			}

			var deserialized = this.NeatooJsonSerializer.DeserializeRemoteResponse<T>(result);

			// Fire-and-forget relay. One [Remote] call = one Relay call (may be empty batch)
			// unless deserialization fails, in which case the batch is aborted and logged.
			// Task.Run + Task.Yield pushes execution to a separate continuation so the
			// caller's `await` resumes first and assignments like `_x = await factory(...)`
			// are observable to the relay's handler.
			if (_relay != null)
			{
				var rawEvents = result.RelayedEvents;
				var relay = _relay;
				var serializer = this.NeatooJsonSerializer;
				var relayLogger = this.logger;
				var relayCorrelationId = correlationId;
#pragma warning disable CA1031 // Relay / deserialization exceptions must never propagate to the factory caller.
				_ = Task.Run(async () =>
				{
					await Task.Yield();
					IReadOnlyList<FactoryEventBase> events;
					try
					{
						events = rawEvents is { Count: > 0 }
							? FactoryEventDeserializer.Deserialize(rawEvents, serializer)
							: Array.Empty<FactoryEventBase>();
					}
					catch (Exception ex)
					{
						relayLogger.FactoryEventDeserializationFailed(relayCorrelationId, ex);
						return;
					}

					try
					{
						await relay.Relay(events).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						relayLogger.FactoryEventRelayFailed(relayCorrelationId, ex);
					}
				}, CancellationToken.None);
#pragma warning restore CA1031
			}

			return deserialized;
		}
		catch (OperationCanceledException)
		{
			sw.Stop();
			logger.RemoteCallCancelled(correlationId, delegateTypeName);
			throw;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.RemoteCallError(correlationId, delegateTypeName, ex.Message, ex);
			throw;
		}
	}

	/// <inheritdoc />
	public async Task ForDelegateEvent(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(delegateType);

		var correlationId = EnsureCorrelationId();
		var delegateTypeName = delegateType.Name;

		logger.RemoteCallStarted(correlationId, delegateTypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			var remoteDelegateRequest = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

			// Await the full HTTP round-trip: the server awaits every [FactoryEventHandler<T>]
			// in the caller's scope, so this call returns only after all handlers complete.
			// A server handler exception propagates back as an HTTP error and rethrows here.
			await this.MakeRemoteDelegateRequestCall(remoteDelegateRequest, cancellationToken);

			sw.Stop();
			logger.RemoteCallCompleted(correlationId, delegateTypeName, sw.ElapsedMilliseconds);
		}
		catch (OperationCanceledException)
		{
			sw.Stop();
			logger.RemoteCallCancelled(correlationId, delegateTypeName);
			throw;
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.RemoteCallError(correlationId, delegateTypeName, ex.Message, ex);
			throw;
		}
	}

	/// <summary>
	/// Ensures a correlation ID exists for the current request, generating one if needed.
	/// </summary>
	private string EnsureCorrelationId()
	{
		if (string.IsNullOrEmpty(_correlationContext.CorrelationId))
		{
			_correlationContext.CorrelationId = CorrelationContextImpl.GenerateCorrelationId();
		}
		return _correlationContext.CorrelationId!;
	}
}
