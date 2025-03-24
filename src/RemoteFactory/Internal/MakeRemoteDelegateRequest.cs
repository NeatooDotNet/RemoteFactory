using System;
using System.Collections.Generic;
using System.Text;

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

	public MakeRemoteDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, MakeRemoteDelegateRequestHttpCall sendRemoteDelegateRequestToServer)
	{
	  this.NeatooJsonSerializer = neatooJsonSerializer;
	  this.MakeRemoteDelegateRequestCall = sendRemoteDelegateRequestToServer;
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
		var remoteDelegateRequest = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

		var result = await this.MakeRemoteDelegateRequestCall(remoteDelegateRequest);

		if (result == null)
		{
			return default;
		}

		return this.NeatooJsonSerializer.DeserializeRemoteResponse<T>(result);
	}
}
