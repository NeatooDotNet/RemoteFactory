using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Neatoo.RemoteFactory.AspNetCore;

public static class WebApplicationExtensions
{
	/// <summary>
	/// Configures the Neatoo RemoteFactory endpoint for handling remote delegate requests.
	/// Adds the X-Neatoo-Format response header to communicate the serialization format.
	/// </summary>
	public static WebApplication UseNeatoo(this WebApplication app)
	{
		app.MapPost("/api/neatoo", async (HttpContext httpContext, RemoteRequestDto request) =>
		{
			var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
			var serializationOptions = httpContext.RequestServices.GetService<NeatooSerializationOptions>();

			// Add the serialization format header to the response
			if (serializationOptions != null)
			{
				httpContext.Response.Headers[NeatooSerializationOptions.FormatHeaderName] = serializationOptions.FormatHeaderValue;
			}

			return await handleRemoteDelegateRequest(request);
		});

		return app;
	}
}
