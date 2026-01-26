---
title: ASP.NET Core Integration
nav_order: 12
---

# ASP.NET Core Integration

RemoteFactory.AspNetCore provides server-side integration for ASP.NET Core applications. It registers the `/api/neatoo` endpoint, handles remote delegate requests, and manages serialization, authorization, and lifecycle hooks.

## Installation

Install the ASP.NET Core integration package:

```bash
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

This package depends on `Neatoo.RemoteFactory` and includes all core functionality.

## Basic Setup

Configure services and middleware in `Program.cs`:

<!-- snippet: aspnetcore-basic-setup -->
<!--
SNIPPET REQUIREMENTS:
- Show minimal Program.cs setup for RemoteFactory ASP.NET Core integration
- Call AddNeatooAspNetCore with the domain assembly (Employee Management domain)
- Call UseNeatoo to map the /api/neatoo endpoint
- Context: production Server.WebApi layer
- Keep it minimal - just the two essential calls in a working Program.cs
-->
<!-- endSnippet -->

The setup follows this pattern:

1. **AddNeatooAspNetCore** - Registers factories, serialization, and authorization services
2. **UseNeatoo** - Adds the `/api/neatoo` endpoint middleware

## AddNeatooAspNetCore

Registers RemoteFactory services with default serialization options (Ordinal format).

```csharp
public static IServiceCollection AddNeatooAspNetCore(
    this IServiceCollection services,
    params Assembly[] entityLibraries)
```

**Parameters:**
- `entityLibraries` - Assemblies containing domain models with `[Factory]` attributes

**What it registers:**
- Generated factories (scoped)
- Serialization services (`NeatooJsonSerializer`, `NeatooSerializationOptions`)
- Authorization (`IAspAuthorize`, default implementation)
- Remote delegate handler (`HandleRemoteDelegateRequest`)
- Event tracker hosted service for graceful shutdown

Basic registration with domain model assembly:

<!-- snippet: aspnetcore-addneatoo -->
<!--
SNIPPET REQUIREMENTS:
- Show AddNeatooAspNetCore registration with single assembly (Employee entity assembly)
- Include commented example of multi-assembly registration (Employee + Department assemblies)
- Context: production Server.WebApi layer, services configuration
- Domain: use Employee Management theme (reference Employee.cs or Department.cs types)
-->
<!-- endSnippet -->

### Custom Serialization Options

Override default serialization with custom options:

```csharp
public static IServiceCollection AddNeatooAspNetCore(
    this IServiceCollection services,
    NeatooSerializationOptions serializationOptions,
    params Assembly[] entityLibraries)
```

Use Named format for debugging:

<!-- snippet: aspnetcore-custom-serialization -->
<!--
SNIPPET REQUIREMENTS:
- Show NeatooSerializationOptions configuration with Named format
- Pass options to AddNeatooAspNetCore with the domain assembly
- Include comment explaining Named format is larger but more readable (for debugging)
- Context: production Server.WebApi layer
- Domain: use Employee Management domain assembly reference
-->
<!-- endSnippet -->

See [Serialization](serialization.md) for format details.

## UseNeatoo

Configures the `/api/neatoo` endpoint for remote delegate requests.

```csharp
public static WebApplication UseNeatoo(this WebApplication app)
```

**What it does:**
- Maps POST endpoint at `/api/neatoo`
- Adds correlation ID middleware
- Extracts `X-Correlation-Id` from request headers
- Includes `X-Neatoo-Format` header in response
- Supports cancellation via client disconnect and server shutdown
- Configures ambient logging with application's logger factory

Middleware order matters:

<!-- snippet: aspnetcore-middleware-order -->
<!--
SNIPPET REQUIREMENTS:
- Show correct middleware ordering in WebApplication pipeline
- Include numbered comments: 1. CORS, 2. Authentication/Authorization, 3. UseNeatoo, 4. Other endpoints
- Call UseCors(), UseAuthentication(), UseAuthorization(), then UseNeatoo()
- Include commented MapControllers() for completeness
- Context: production Server.WebApi layer, app configuration
-->
<!-- endSnippet -->

## Endpoint Details

### Request Format

The client sends a `POST` to `/api/neatoo` with this payload:

```csharp
public class RemoteRequestDto
{
    public string Target { get; set; }      // Factory operation identifier
    public string? Args { get; set; }       // JSON-serialized arguments
}
```

**You do not create this manually.** Generated factories construct it automatically.

### Response Format

The server returns a `RemoteResponse`:

```csharp
public class RemoteResponse
{
    public object? Result { get; set; }
    public bool Authorized { get; set; }
    public string? Error { get; set; }
}
```

**Headers:**
- `X-Neatoo-Format`: `ordinal` or `named` (indicates serialization format)
- `X-Correlation-Id`: Correlation ID for distributed tracing

### Cancellation Support

The endpoint supports cancellation from multiple sources:

- **Client disconnect** - Detects when the HTTP request is aborted
- **Server shutdown** - Responds to `IHostApplicationLifetime.ApplicationStopping`
- **Explicit cancellation** - Honors `CancellationToken` passed to factory methods

Cancellation flow:

<!-- snippet: aspnetcore-cancellation -->
<!--
SNIPPET REQUIREMENTS:
- Show [Factory] partial class Employee with CancellationToken in a [Remote, Fetch] method
- Include CancellationToken as last parameter in the Fetch method signature
- Show comments explaining cancellation sources: 1. Client disconnect, 2. Server shutdown
- Call cancellationToken.ThrowIfCancellationRequested() before async work
- Pass cancellationToken to repository async method (IEmployeeRepository)
- Context: production Domain layer entity with server-side remote method
- Domain: Employee entity with Id (Guid) and relevant properties
-->
<!-- endSnippet -->

## Correlation IDs

RemoteFactory automatically manages correlation IDs for distributed tracing.

**Client-side:**
- Generates a unique correlation ID for each request
- Includes it in the `X-Correlation-Id` header

**Server-side:**
- Extracts correlation ID from request headers
- Makes it available via `CorrelationContext.CorrelationId`
- Includes it in response headers
- Adds it to structured logs

Access the correlation ID in factory methods:

<!-- snippet: aspnetcore-correlation-id -->
<!--
SNIPPET REQUIREMENTS:
- Show [Factory] partial class Employee accessing CorrelationContext.CorrelationId
- Include [Remote, Fetch] method that uses correlation ID for audit logging
- Inject IAuditLogService via [Service] parameter
- Show reading CorrelationContext.CorrelationId with comment explaining it's auto-populated from X-Correlation-Id header
- Demonstrate passing correlation ID to audit log for distributed tracing
- Context: production Domain layer entity
- Domain: Employee entity with Id (Guid), show audit trail use case
-->
<!-- endSnippet -->

## Logging

RemoteFactory integrates with `ILoggerFactory` for structured logging.

**Registered loggers:**
- `Neatoo.RemoteFactory.Server` - Server-side request handling
- `Neatoo.RemoteFactory.Client` - Client-side HTTP calls
- `Neatoo.RemoteFactory.Serialization` - Serialization diagnostics

Configure log levels in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Neatoo.RemoteFactory": "Information"
    }
  }
}
```

**Log categories:**
- `NeatooLoggerCategories.Server` - Remote delegate execution
- `NeatooLoggerCategories.Client` - HTTP client requests
- `NeatooLoggerCategories.Serialization` - JSON serialization/deserialization

Log structured data in factory methods:

<!-- snippet: aspnetcore-logging -->
<!--
SNIPPET REQUIREMENTS:
- Show logging configuration with AddLogging builder pattern
- Add console logging provider
- Set minimum level to Information
- Include comments listing Neatoo log categories: Server, Client, Serialization
- Add filter for "Neatoo.RemoteFactory" category at Debug level
- Call AddNeatooAspNetCore after logging setup
- Context: production Server.WebApi layer, services configuration
- Domain: reference Employee Management domain assembly
-->
<!-- endSnippet -->

## Event Tracking

RemoteFactory uses `IEventTracker` for fire-and-forget domain events.

**EventTrackerHostedService** is registered automatically by `AddNeatooAspNetCore()`. It:
- Tracks pending event handlers across all scopes
- Waits for completion during graceful shutdown
- Logs warnings for events that exceed shutdown timeout

**Shutdown behavior:**
- `IHostApplicationLifetime.ApplicationStopping` triggers shutdown
- Event handlers receive cancellation signal via `CancellationToken`
- Server waits up to 30 seconds for event completion
- Logs incomplete events after timeout

See [Events](events.md) for details on domain events.

## Authorization

ASP.NET Core integration includes authorization support via `IAspAuthorize`.

**Default implementation (`AspAuthorize`):**
- Registered automatically by `AddNeatooAspNetCore()`
- Integrates with ASP.NET Core's authorization system
- Uses `[AspAuthorize]` attributes on factory methods
- Supports policies, roles, and authentication schemes

**Interface:**
```csharp
public interface IAspAuthorize
{
    Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false);
}
```

The `AspAuthorize` implementation coordinates with ASP.NET Core's `IAuthorizationPolicyProvider` and `IPolicyEvaluator` to evaluate authorization requirements. Custom implementations can override this behavior by registering a different `IAspAuthorize` implementation.

See [Authorization](authorization.md) for complete authorization patterns, including custom implementations.

## Service Registration

Register your domain services alongside RemoteFactory:

<!-- snippet: aspnetcore-service-registration -->
<!--
SNIPPET REQUIREMENTS:
- Show complete service registration pattern for RemoteFactory
- Register Neatoo first with AddNeatooAspNetCore
- Register domain services with AddScoped: IEmployeeRepository, IDepartmentRepository
- Register infrastructure services: IUserContext, IEmailService, IAuditLogService
- Show RegisterMatchingName for auto-registration of IName/Name pattern
- Include comment explaining services are available via [Service] parameters
- Context: production Server.WebApi layer, services configuration
- Domain: Employee Management (IEmployeeRepository, IDepartmentRepository)
-->
<!-- endSnippet -->

**RegisterMatchingName** is a convenience method that registers interfaces and their implementations as transient when they follow the `IName` → `Name` pattern.

Services are injected into factory methods via `[Service]` parameters.

## Multi-Assembly Support

Register factories from multiple assemblies:

<!-- snippet: aspnetcore-multi-assembly -->
<!--
SNIPPET REQUIREMENTS:
- Show AddNeatooAspNetCore with multiple assembly registration
- Pass Employee.Domain assembly as primary
- Include commented examples for additional assemblies (HR.Domain, Payroll.Domain)
- Show RegisterMatchingName with same multiple assemblies
- Include comments explaining each assembly contains [Factory] types
- Context: production Server.WebApi layer, services configuration
- Domain: Employee Management as primary, hypothetical HR/Payroll as additional
-->
<!-- endSnippet -->

Each assembly can contain domain models with `[Factory]` attributes. The generator processes all of them.

## Development vs Production

### Development

Use Named serialization format for easier debugging:

<!-- snippet: aspnetcore-development -->
<!--
SNIPPET REQUIREMENTS:
- Show development-optimized configuration with environment check
- Use SerializationFormat.Named for readable JSON in development
- Fall back to SerializationFormat.Ordinal in production (ternary expression)
- Enable detailed logging in development (Debug minimum level, Trace for Neatoo)
- Pass serialization options to AddNeatooAspNetCore
- Include comments: "Readable JSON" for Named, "Compact arrays" for Ordinal
- Context: production Server.WebApi layer with isDevelopment parameter
- Domain: reference Employee Management domain assembly
-->
<!-- endSnippet -->

### Production

Use Ordinal format (default) for compact payloads:

<!-- snippet: aspnetcore-production -->
<!--
SNIPPET REQUIREMENTS:
- Show production-optimized configuration
- Use SerializationFormat.Ordinal explicitly (comment: minimal payload size, default)
- Configure production logging: Warning minimum level, Information for Neatoo
- Pass serialization options to AddNeatooAspNetCore
- Include comment: "Production logging - less verbose"
- Context: production Server.WebApi layer
- Domain: reference Employee Management domain assembly
-->
<!-- endSnippet -->

Ordinal format reduces payload size by 40-50% compared to Named format.

## Error Handling

The `/api/neatoo` endpoint returns structured errors:

**Authorization failures:**
```json
{
  "Authorized": false,
  "Result": null,
  "Error": null
}
```

**Exception during execution:**
```json
{
  "Authorized": true,
  "Result": null,
  "Error": "Exception message"
}
```

Handle errors on the client:

<!-- snippet: aspnetcore-error-handling -->
<!--
SNIPPET REQUIREMENTS:
- Show client-side error handling for RemoteFactory exceptions
- Demonstrate catching NotAuthorizedException when authorization fails
- Demonstrate catching ValidationException when server-side validation fails
- Use try/catch blocks with appropriate exception types
- Show getting factory from DI and calling operations that may fail
- Include comments explaining each error scenario
- Context: Client layer code (Blazor or console app consuming the API)
- Domain: Employee entity with authorization and validation scenarios
- NOTE: This is CLIENT-SIDE code showing how to handle errors returned from server
-->
<!-- endSnippet -->

## CORS Configuration

Configure CORS for Blazor WASM clients:

<!-- snippet: aspnetcore-cors -->
<!--
SNIPPET REQUIREMENTS:
- Show complete CORS configuration for Blazor WASM client
- Configure default policy with WithOrigins (localhost:5001 for dev, production domain)
- Include AllowAnyHeader, AllowAnyMethod, AllowCredentials (comment: required for auth cookies)
- Show named policy "NeatooApi" with specific headers (Content-Type, X-Correlation-Id) and POST method
- Call AddNeatooAspNetCore after CORS setup
- Show app configuration with UseCors() before UseNeatoo()
- Context: production Server.WebApi layer, complete CORS setup
- Domain: reference Employee Management domain assembly
-->
<!-- endSnippet -->

Place CORS middleware before `UseNeatoo()` to allow cross-origin requests.

## Minimal API Integration

RemoteFactory integrates seamlessly with minimal APIs:

<!-- snippet: aspnetcore-minimal-api -->
<!--
SNIPPET REQUIREMENTS:
- Show UseNeatoo coexisting with minimal API endpoints
- Include UseNeatoo() with comment: POST /api/neatoo
- Add /health endpoint returning "OK" string
- Add /api/info endpoint returning anonymous object with Version and Framework
- Include commented MapControllers() for MVC fallback
- Show that Neatoo endpoint coexists with other endpoints
- Context: production Server.WebApi layer, app configuration
-->
<!-- endSnippet -->

The `/api/neatoo` endpoint coexists with your other minimal API endpoints.

## Performance Considerations

**Scoped services:**
- Factories and serializers are scoped per-request
- Avoid singleton services that hold request-specific state

**Serialization:**
- Ordinal format reduces payload size by 40-50%
- Use Named format only for debugging

**Event handlers:**
- Run in isolated scopes (new `IServiceScope`)
- Do not block HTTP response (fire-and-forget)
- Respect cancellation tokens for graceful shutdown

## Testing

Test server-side factories using the two-container pattern:

<!-- snippet: aspnetcore-testing -->
<!--
SNIPPET REQUIREMENTS:
- Show two-container test pattern for client/server simulation
- Create test class with [Fact] methods demonstrating the pattern
- First test: ClientServerRoundTrip showing client vs local factory comparison
  - Get scopes from test container helper
  - Get IEmployeeFactory from client container (simulates Blazor WASM)
  - Get IEmployeeFactory from local container (no serialization)
  - Create entities from both and compare results
- Second test: TestFullWorkflow showing Create, Save, Fetch, Update, Delete cycle
  - Use client factory throughout
  - Assert state changes (IsNew, persisted values)
- Include Employee entity with [Factory], IFactorySaveMeta, [Remote] methods
- Show [Create], [Remote, Fetch], [Remote, Insert], [Remote, Update], [Remote, Delete]
- Context: Tests layer, integration tests
- Domain: Employee entity with Id (Guid), Name (string), IsNew, IsDeleted
-->
<!-- endSnippet -->

This simulates client/server communication without HTTP.

See the `RemoteFactory.IntegrationTests` project for comprehensive examples.

## Summary

| Method | Purpose |
|--------|---------|
| `AddNeatooAspNetCore` | Register factories, serialization, and authorization services |
| `UseNeatoo` | Add `/api/neatoo` endpoint middleware |

**Key features:**
- Automatic endpoint generation at `/api/neatoo`
- Correlation ID propagation for distributed tracing
- Integrated logging with `ILoggerFactory`
- Graceful shutdown support for event handlers
- Client disconnect and server shutdown cancellation
- ASP.NET Core authorization integration

## Next Steps

- [Getting Started](getting-started.md) - First working example
- [Authorization](authorization.md) - Authorization patterns
- [Events](events.md) - Fire-and-forget domain events
- [Serialization](serialization.md) - Ordinal vs Named formats
