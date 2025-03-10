using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.Internal;

	public delegate Task<RemoteResponseDto> MakeRemoteDelegateRequestHttpCall(RemoteDelegateRequestDto request);



public static class MakeRemoteDelegateRequestHttpCallImplementation
{
	public static MakeRemoteDelegateRequestHttpCall Create(HttpClient httpClient)
	{
		return async (RemoteDelegateRequestDto request) =>
		{
			var uri = new Uri(httpClient.BaseAddress!, "api/remotefactory");

			var response = await httpClient.PostAsync(uri, JsonContent.Create(request, typeof(RemoteDelegateRequestDto)));

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
