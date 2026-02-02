using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory.AspNetCore;

public static class WebApplicationExtensions
{
	/// <summary>
	/// Configures the Neatoo RemoteFactory endpoint for handling remote delegate requests.
	/// Adds the X-Neatoo-Format response header to communicate the serialization format.
	/// Automatically extracts correlation ID from request headers.
	/// Supports cancellation via client disconnect and server shutdown.
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

		// Get the application lifetime for graceful shutdown support
		var hostLifetime = app.Services.GetService<IHostApplicationLifetime>();

		// Add middleware for correlation ID extraction
		app.Use(async (context, next) =>
		{
			// Get the scoped correlation context for this request
			var correlationContext = context.RequestServices.GetRequiredService<ICorrelationContext>();

			// Extract correlation ID from request headers or generate a new one
			if (context.Request.Headers.TryGetValue(CorrelationContextImpl.HeaderName, out var headerValue)
				&& !string.IsNullOrEmpty(headerValue.FirstOrDefault()))
			{
				correlationContext.CorrelationId = headerValue.FirstOrDefault();
			}
			else
			{
				correlationContext.CorrelationId = CorrelationContextImpl.GenerateCorrelationId();
			}

			// Add correlation ID to response headers for client-side tracing
			context.Response.Headers[CorrelationContextImpl.HeaderName] = correlationContext.CorrelationId;
			await next();
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

			// Create a linked cancellation token that fires on:
			// 1. Client disconnect (HttpContext.RequestAborted)
			// 2. Server graceful shutdown (ApplicationStopping)
			CancellationToken cancellationToken;
			CancellationTokenSource? linkedCts = null;

			if (hostLifetime != null)
			{
				linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
					httpContext.RequestAborted,
					hostLifetime.ApplicationStopping);
				cancellationToken = linkedCts.Token;
			}
			else
			{
				cancellationToken = httpContext.RequestAborted;
			}

			try
			{
				return await handleRemoteDelegateRequest(request, cancellationToken);
			}
			finally
			{
				linkedCts?.Dispose();
			}
		});

		return app;
	}
}
