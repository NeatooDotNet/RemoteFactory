using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

public delegate Task<RemoteResponseDto> HandleRemoteDelegateRequest(RemoteRequestDto portalRequest, CancellationToken cancellationToken);


public static class LocalServer
{
	private const string Result = "Result";

	/// <summary>
	/// Prepares invoke parameters by injecting CancellationToken where expected.
	/// CancellationToken is excluded from serialized parameters but flows through the HTTP layer.
	/// </summary>
	private static object?[] PrepareInvokeParameters(
		IReadOnlyCollection<object?>? serializedParams,
		ParameterInfo[] methodParams,
		CancellationToken cancellationToken)
	{
		var result = new object?[methodParams.Length];
		var paramsList = serializedParams?.ToArray() ?? [];
		var serializedIndex = 0;

		for (int i = 0; i < methodParams.Length; i++)
		{
			if (methodParams[i].ParameterType == typeof(CancellationToken))
			{
				// Inject the CancellationToken from the HTTP layer
				result[i] = cancellationToken;
			}
			else if (serializedIndex < paramsList.Length)
			{
				result[i] = paramsList[serializedIndex++];
			}
			else
			{
				// Use default for missing parameters
				result[i] = methodParams[i].HasDefaultValue ? methodParams[i].DefaultValue : null;
			}
		}

		return result;
	}

	public static HandleRemoteDelegateRequest HandlePortalRequest(IServiceProvider serviceProvider)
	{
		return HandlePortalRequest(serviceProvider, null);
	}

	public static HandleRemoteDelegateRequest HandlePortalRequest(IServiceProvider serviceProvider, ILogger? logger)
	{
		var log = logger ?? NullLoggerFactory.Instance.CreateLogger(NeatooLoggerCategories.Server);

		return async (portalRequest, cancellationToken) =>
		{
			var correlationId = CorrelationContext.CorrelationId ?? CorrelationContext.EnsureCorrelationId();
			var delegateTypeName = portalRequest.DelegateAssemblyType ?? "unknown";

			log.HandlingRemoteRequest(correlationId, delegateTypeName);
			var sw = Stopwatch.StartNew();

			var serializer = serviceProvider.GetRequiredService<INeatooJsonSerializer>();

			try
			{
				// Check for cancellation before processing
				cancellationToken.ThrowIfCancellationRequested();

				var remoteRequest = serializer.DeserializeRemoteDelegateRequest(portalRequest);
				var parameterCount = remoteRequest.Parameters?.Count ?? 0;
				log.RemoteRequestDeserialized(correlationId, parameterCount);

				ArgumentNullException.ThrowIfNull(remoteRequest.DelegateType, nameof(remoteRequest.DelegateType));

				var method = (Delegate)serviceProvider.GetRequiredService(remoteRequest.DelegateType);

				// Prepare parameters, injecting CancellationToken if the method expects it
				var methodParams = method.Method.GetParameters();
				var invokeParams = PrepareInvokeParameters(remoteRequest.Parameters, methodParams, cancellationToken);

				object? result;
				try
				{
					result = method.DynamicInvoke(invokeParams);
				}
				catch (TargetInvocationException tie) when (tie.InnerException != null)
				{
					// Unwrap the TargetInvocationException from DynamicInvoke
					System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
					throw; // Unreachable, but required by compiler
				}

				if (result is Task task)
				{
					await task;
					// Only get result for Task<T>, not for non-generic Task (events, void methods)
					var resultProperty = task.GetType().GetProperty(Result);
					if (resultProperty != null && resultProperty.PropertyType != typeof(void) &&
						resultProperty.PropertyType.Name != "VoidTaskResult")
					{
						result = resultProperty.GetValue(task);
					}
					else
					{
						result = null;
					}
				}

				// Check for cancellation before serializing response
				cancellationToken.ThrowIfCancellationRequested();

				// This is the return type the client is looking for
				// If it is an Interface - match the interface
				// Was having issues with the FactoryGenerator when this was returning the concrete type
				// instead of the interface with the concrete type specific in the JSON

				var returnType = method.GetMethodInfo().ReturnType;

				// Handle Task<T> return types - extract the inner type
				// For non-generic Task (events) or void, serialize null with object type
				if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
				{
					returnType = returnType.GetGenericArguments()[0];
				}
				else if (returnType == typeof(Task) || returnType == typeof(void))
				{
					// For non-generic Task (events) or void methods, serialize null as object
					result = null;
					returnType = typeof(object);
				}

				var portalResponse = new RemoteResponseDto(serializer.Serialize(result, returnType));

				sw.Stop();
				log.RemoteRequestCompleted(correlationId, delegateTypeName, sw.ElapsedMilliseconds);

				return portalResponse;
			}
			catch (OperationCanceledException)
			{
				sw.Stop();
				log.RemoteRequestCancelled(correlationId, delegateTypeName);
				throw;
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