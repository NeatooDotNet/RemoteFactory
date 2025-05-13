using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.Internal;

internal sealed class MakeLocalSerializedDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly IServiceProvider serviceProvider;

   public MakeLocalSerializedDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, IServiceProvider serviceProvider)
	{
		this.NeatooJsonSerializer = neatooJsonSerializer;
		this.serviceProvider = serviceProvider;
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

	private const string Result = "Result";

	public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters)
	{
		// Serialize and Deserialize the request so that a different object is returned

		var duplicatedRemoteRequestDto = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

		var duplicatedRemoteRequest = this.NeatooJsonSerializer.DeserializeRemoteDelegateRequest(duplicatedRemoteRequestDto);

		var method = (Delegate) this.serviceProvider.GetRequiredService(delegateType);

		var result = method.DynamicInvoke(duplicatedRemoteRequest.Parameters?.ToArray());

		if (result is Task task)
		{
			await task;
			result = task.GetType()!.GetProperty(Result)!.GetValue(task);
		}

		return (T?)result;
	}
}