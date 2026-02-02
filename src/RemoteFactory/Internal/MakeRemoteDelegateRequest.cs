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
	/// Sends an event to the server. Client awaits HTTP acknowledgment (delivery confirmation).
	/// Server handles the event in a new scope with fire-and-forget semantics.
	/// Handler failures are invisible to the client.
	/// </summary>
	Task ForDelegateEvent(Type delegateType, object?[]? parameters);

	// Backward-compatible overloads without CancellationToken
	Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters)
		=> ForDelegate<T>(delegateType, parameters, default);
	Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters)
		=> ForDelegateNullable<T>(delegateType, parameters, default);
}

public class MakeRemoteDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly MakeRemoteDelegateRequestHttpCall MakeRemoteDelegateRequestCall;
	private readonly ICorrelationContext _correlationContext;
	private readonly ILogger<MakeRemoteDelegateRequest> logger;

	public MakeRemoteDelegateRequest(
		INeatooJsonSerializer neatooJsonSerializer,
		MakeRemoteDelegateRequestHttpCall sendRemoteDelegateRequestToServer,
		ICorrelationContext correlationContext)
		: this(neatooJsonSerializer, sendRemoteDelegateRequestToServer, correlationContext, null)
	{
	}

	public MakeRemoteDelegateRequest(
		INeatooJsonSerializer neatooJsonSerializer,
		MakeRemoteDelegateRequestHttpCall sendRemoteDelegateRequestToServer,
		ICorrelationContext correlationContext,
		ILogger<MakeRemoteDelegateRequest>? logger)
	{
		this.NeatooJsonSerializer = neatooJsonSerializer;
		this.MakeRemoteDelegateRequestCall = sendRemoteDelegateRequestToServer;
		_correlationContext = correlationContext;
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

			return this.NeatooJsonSerializer.DeserializeRemoteResponse<T>(result);
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
	public async Task ForDelegateEvent(Type delegateType, object?[]? parameters)
	{
		ArgumentNullException.ThrowIfNull(delegateType);

		var correlationId = EnsureCorrelationId();
		var delegateTypeName = delegateType.Name;

		logger.RemoteCallStarted(correlationId, delegateTypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			var remoteDelegateRequest = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

			// For events, we only await HTTP acknowledgment - we don't care about the response content
			// The server will handle the event in a new scope (fire-and-forget at handler level)
			await this.MakeRemoteDelegateRequestCall(remoteDelegateRequest, default);

			sw.Stop();
			logger.RemoteCallCompleted(correlationId, delegateTypeName, sw.ElapsedMilliseconds);
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
