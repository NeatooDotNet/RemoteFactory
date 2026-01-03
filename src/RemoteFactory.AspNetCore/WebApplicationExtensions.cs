using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory.AspNetCore;

public static class WebApplicationExtensions
{
	/// <summary>
	/// Configures the Neatoo RemoteFactory endpoint for handling remote delegate requests.
	/// Adds the X-Neatoo-Format response header to communicate the serialization format.
	/// Automatically extracts correlation ID from request headers.
	/// </summary>
	public static WebApplication UseNeatoo(this WebApplication app)
	{
		ArgumentNullException.ThrowIfNull(app);

		// Configure ambient logging with the application's logger factory
		var loggerFactory = app.Services.GetService<ILoggerFactory>();
		if (loggerFactory != null)
		{
			NeatooLogging.SetLoggerFactory(loggerFactory);
		}

		// Add middleware for correlation ID extraction
		app.Use(async (context, next) =>
		{
			// Extract correlation ID from request headers or generate a new one
			string? correlationId = null;
			if (context.Request.Headers.TryGetValue(CorrelationContext.HeaderName, out var headerValue))
			{
				correlationId = headerValue.FirstOrDefault();
			}

			using (CorrelationContext.BeginScope(correlationId))
			{
				// Add correlation ID to response headers for client-side tracing
				context.Response.Headers[CorrelationContext.HeaderName] = CorrelationContext.CorrelationId;
				await next();
			}
		});

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
