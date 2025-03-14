using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using System.Reflection;


namespace Neatoo.RemoteFactory;

public delegate Task<RemoteResponseDto> HandleRemoteDelegateRequest(RemoteRequestDto portalRequest);


public static class LocalServer
{
   private const string Result = "Result";

   public static HandleRemoteDelegateRequest HandlePortalRequest(IServiceProvider serviceProvider)
	{
		return async (portalRequest) =>
		{
			var serializer = serviceProvider.GetRequiredService<INeatooJsonSerializer>();
			var remoteRequest = serializer.DeserializeRemoteDelegateRequest(portalRequest);

			ArgumentNullException.ThrowIfNull(remoteRequest.DelegateType, nameof(remoteRequest.DelegateType));

			var method = (Delegate)serviceProvider.GetRequiredService(remoteRequest.DelegateType);

			var result = method.DynamicInvoke(remoteRequest.Parameters?.ToArray());

			if (result is Task task)
			{
				await task;
				result = task.GetType()!.GetProperty(Result)!.GetValue(task);
			}

			// This is the return type the client is looking for
			// If it is an Interface - match the interface
			// Was having issues with the FactoryGenerator when this was returning the concrete type 
			// instead of the interface with the concrete type specific in the JSON

			var returnType = method.GetMethodInfo().ReturnType;

			if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				returnType = returnType.GetGenericArguments()[0];
			}

			var portalResponse = new RemoteResponseDto(serializer.Serialize(result, returnType));

			return portalResponse;
		};
	}
}