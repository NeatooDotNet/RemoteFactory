using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Neatoo.RemoteFactory.AspNetCore;
public static class WebApplicationExtensions
{
	public static WebApplication UseNeatooRemoteFactory(this WebApplication app)
	{
		app.MapPost("/api/neatoo", (HttpContext httpContext, RemoteRequestDto request) =>
		{
			var handleRemoteDelegateRequest = httpContext.RequestServices.GetRequiredService<HandleRemoteDelegateRequest>();
			return handleRemoteDelegateRequest(request);
		}); 
		
		return app;
	}
}
