using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Neatoo.RemoteFactory.Internal;

internal sealed class MakeLocalSerializedDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<MakeLocalSerializedDelegateRequest> logger;

	public MakeLocalSerializedDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, IServiceProvider serviceProvider)
		: this(neatooJsonSerializer, serviceProvider, null)
	{
	}

	public MakeLocalSerializedDelegateRequest(
		INeatooJsonSerializer neatooJsonSerializer,
		IServiceProvider serviceProvider,
		ILogger<MakeLocalSerializedDelegateRequest>? logger)
	{
		this.NeatooJsonSerializer = neatooJsonSerializer;
		this.serviceProvider = serviceProvider;
		this.logger = logger ?? NullLogger<MakeLocalSerializedDelegateRequest>.Instance;
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

	private const string Result = "Result";

	public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
	{
		var correlationId = CorrelationContext.EnsureCorrelationId();
		var delegateTypeName = delegateType.Name;

		logger.RemoteCallStarted(correlationId, delegateTypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			// Check for cancellation before starting
			cancellationToken.ThrowIfCancellationRequested();

			// Serialize and Deserialize the request so that a different object is returned
			var duplicatedRemoteRequestDto = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

			var duplicatedRemoteRequest = this.NeatooJsonSerializer.DeserializeRemoteDelegateRequest(duplicatedRemoteRequestDto);

			var method = (Delegate)this.serviceProvider.GetRequiredService(delegateType);

			// Append CancellationToken to parameters - it's excluded from serialization
			// but delegates expect it as the last parameter
			var paramsWithToken = duplicatedRemoteRequest.Parameters?.ToList() ?? new List<object?>();
			paramsWithToken.Add(cancellationToken);
			var result = method.DynamicInvoke(paramsWithToken.ToArray());

			if (result is Task task)
			{
				await task;
				result = task.GetType()!.GetProperty(Result)!.GetValue(task);
			}

			sw.Stop();
			logger.RemoteCallCompleted(correlationId, delegateTypeName, sw.ElapsedMilliseconds);

			return (T?)result;
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
		var correlationId = CorrelationContext.EnsureCorrelationId();
		var delegateTypeName = delegateType.Name;

		logger.RemoteCallStarted(correlationId, delegateTypeName);
		var sw = Stopwatch.StartNew();

		try
		{
			// Serialize and Deserialize the request so that a different object is returned
			var duplicatedRemoteRequestDto = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

			var duplicatedRemoteRequest = this.NeatooJsonSerializer.DeserializeRemoteDelegateRequest(duplicatedRemoteRequestDto);

			var method = (Delegate)this.serviceProvider.GetRequiredService(delegateType);

			// Append CancellationToken to parameters - it's excluded from serialization
			// but delegates expect it as the last parameter
			var paramsWithToken = duplicatedRemoteRequest.Parameters?.ToList() ?? new List<object?>();
			paramsWithToken.Add(CancellationToken.None);
			var result = method.DynamicInvoke(paramsWithToken.ToArray());

			// For events, we await the task to complete (fire-and-forget is at the caller level)
			if (result is Task task)
			{
				await task;
			}

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
}