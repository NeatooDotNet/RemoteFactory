using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Neatoo.RemoteFactory.Internal;


public interface IMakeRemoteDelegateRequest
{
	Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters);
	Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters);

}

public class MakeRemoteDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly MakeRemoteDelegateRequestHttpCall MakeRemoteDelegateRequestCall;
	private readonly ILogger<MakeRemoteDelegateRequest> logger;

	public MakeRemoteDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, MakeRemoteDelegateRequestHttpCall sendRemoteDelegateRequestToServer)
		: this(neatooJsonSerializer, sendRemoteDelegateRequestToServer, null)
	{
	}

	public MakeRemoteDelegateRequest(
		INeatooJsonSerializer neatooJsonSerializer,
		MakeRemoteDelegateRequestHttpCall sendRemoteDelegateRequestToServer,
		ILogger<MakeRemoteDelegateRequest>? logger)
	{
		this.NeatooJsonSerializer = neatooJsonSerializer;
		this.MakeRemoteDelegateRequestCall = sendRemoteDelegateRequestToServer;
		this.logger = logger ?? NullLogger<MakeRemoteDelegateRequest>.Instance;
	}

	public async Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters)
	{
		var result = await this.ForDelegateNullable<T>(delegateType, parameters);

		if (result == null)
		{
			throw new InvalidOperationException($"The result of the remote delegate call was null, but a non-nullable type was expected.");
		}

		return result;
	}

	public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters)
	{
		ArgumentNullException.ThrowIfNull(delegateType);

		var correlationId = CorrelationContext.EnsureCorrelationId();
		var delegateTypeName = delegateType.Name;

		logger.RemoteCallStarted(correlationId, delegateTypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			var remoteDelegateRequest = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

			var result = await this.MakeRemoteDelegateRequestCall(remoteDelegateRequest);

			sw.Stop();
			logger.RemoteCallCompleted(correlationId, delegateTypeName, sw.ElapsedMilliseconds);

			if (result == null)
			{
				return default;
			}

			return this.NeatooJsonSerializer.DeserializeRemoteResponse<T>(result);
		}
		catch (Exception ex)
		{
			sw.Stop();
			logger.RemoteCallError(correlationId, delegateTypeName, ex.Message, ex);
			throw;
		}
	}
}
