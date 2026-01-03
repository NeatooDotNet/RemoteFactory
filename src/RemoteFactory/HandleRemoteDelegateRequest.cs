using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

public delegate Task<RemoteResponseDto> HandleRemoteDelegateRequest(RemoteRequestDto portalRequest);


public static class LocalServer
{
	private const string Result = "Result";

	public static HandleRemoteDelegateRequest HandlePortalRequest(IServiceProvider serviceProvider)
	{
		return HandlePortalRequest(serviceProvider, null);
	}

	public static HandleRemoteDelegateRequest HandlePortalRequest(IServiceProvider serviceProvider, ILogger? logger)
	{
		var log = logger ?? NullLoggerFactory.Instance.CreateLogger(NeatooLoggerCategories.Server);

		return async (portalRequest) =>
		{
			var correlationId = CorrelationContext.CorrelationId ?? CorrelationContext.EnsureCorrelationId();
			var delegateTypeName = portalRequest.DelegateAssemblyType ?? "unknown";

			log.HandlingRemoteRequest(correlationId, delegateTypeName);
			var sw = Stopwatch.StartNew();

			var serializer = serviceProvider.GetRequiredService<INeatooJsonSerializer>();

			try
			{
				var remoteRequest = serializer.DeserializeRemoteDelegateRequest(portalRequest);
				var parameterCount = remoteRequest.Parameters?.Count ?? 0;
				log.RemoteRequestDeserialized(correlationId, parameterCount);

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

				sw.Stop();
				log.RemoteRequestCompleted(correlationId, delegateTypeName, sw.ElapsedMilliseconds);

				return portalResponse;
			}
			catch (AspForbidException)
			{
				sw.Stop();
				log.RemoteRequestForbidden(correlationId, delegateTypeName);
				// context.Forbid() have already been called
				return new RemoteResponseDto(string.Empty);
			}
			catch (Exception ex) when (ex is not AspForbidException)
			{
				sw.Stop();
				log.RemoteRequestFailed(correlationId, delegateTypeName, ex.Message, ex);
				throw;
			}
		};
	}
}


[Serializable]
public class AspForbidException : Exception
{
	public AspForbidException() { }
	public AspForbidException(string message) : base(message) { }
	public AspForbidException(string message, Exception inner) : base(message, inner) { }
}