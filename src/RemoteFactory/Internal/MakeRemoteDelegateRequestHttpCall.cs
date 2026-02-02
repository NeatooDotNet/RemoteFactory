using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Neatoo.RemoteFactory.Internal;

public delegate Task<RemoteResponseDto> MakeRemoteDelegateRequestHttpCall(RemoteRequestDto request, CancellationToken cancellationToken);

public static class MakeRemoteDelegateRequestHttpCallImplementation
{
	public static MakeRemoteDelegateRequestHttpCall Create(HttpClient httpClient, ICorrelationContext correlationContext)
	{
		return Create(httpClient, correlationContext, null);
	}

	public static MakeRemoteDelegateRequestHttpCall Create(HttpClient httpClient, ICorrelationContext correlationContext, ILogger? logger)
	{
		return async (RemoteRequestDto request, CancellationToken cancellationToken) =>
		{
			var uri = new Uri(httpClient.BaseAddress!, "api/neatoo");
			// Correlation ID should already be set by MakeRemoteDelegateRequest.EnsureCorrelationId()
			var correlationId = correlationContext.CorrelationId ?? CorrelationContextImpl.GenerateCorrelationId();
			var delegateTypeName = request.DelegateAssemblyType ?? "unknown";

			// Add correlation ID header for server-side tracing
			using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri)
			{
				Content = JsonContent.Create(request)
			};
			httpRequest.Headers.Add(CorrelationContextImpl.HeaderName, correlationId);

			var response = await httpClient.SendAsync(httpRequest, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				var issue = await response.Content.ReadAsStringAsync(cancellationToken);
				logger?.RemoteCallFailed(correlationId, delegateTypeName, (int)response.StatusCode, null);
				throw new HttpRequestException($"Failed to call remotefactory. Status code: {response.StatusCode} {issue}");
			}

			var result = await response.Content.ReadFromJsonAsync<RemoteResponseDto>(cancellationToken) ?? throw new HttpRequestException($"Successful Code but empty response.");

			return result;
		};
	}
}
