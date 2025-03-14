using System.Net.Http.Json;

namespace Neatoo.RemoteFactory.Internal;

public delegate Task<RemoteResponseDto> MakeRemoteDelegateRequestHttpCall(RemoteRequestDto request);

public static class MakeRemoteDelegateRequestHttpCallImplementation
{
	public static MakeRemoteDelegateRequestHttpCall Create(HttpClient httpClient)
	{
		return async (RemoteRequestDto request) =>
		{
			var uri = new Uri(httpClient.BaseAddress!, "api/neatoo");

			var response = await httpClient.PostAsync(uri, JsonContent.Create(request));

			if (!response.IsSuccessStatusCode)
			{
				var issue = await response.Content.ReadAsStringAsync();
				throw new HttpRequestException($"Failed to call remotefactory. Status code: {response.StatusCode} {issue}");
			}

			var result = await response.Content.ReadFromJsonAsync<RemoteResponseDto>() ?? throw new HttpRequestException($"Successful Code but empty response."); ;

			return result;
		};
	}
}
