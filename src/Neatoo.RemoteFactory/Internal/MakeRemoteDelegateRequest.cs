using System;
using System.Collections.Generic;
using System.Text;

namespace Neatoo.RemoteFactory.Internal;

public delegate Task<RemoteResponseDto> MakeRemoteDelegateRequestCall(RemoteDelegateRequestDto request);

public interface IMakeRemoteDelegateRequest
{
	Task<T?> ForDelegate<T>(Type delegateType, object[]? parameters);
}

public class MakeRemoteDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly MakeRemoteDelegateRequestCall MakeRemoteDelegateRequestCall;

	public MakeRemoteDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, MakeRemoteDelegateRequestCall sendRemoteDelegateRequestToServer)
	{
	  this.NeatooJsonSerializer = neatooJsonSerializer;
	  this.MakeRemoteDelegateRequestCall = sendRemoteDelegateRequestToServer;
	}

	public async Task<T?> ForDelegate<T>(Type delegateType, object[]? parameters)
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
