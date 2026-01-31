// Factory Mode Assembly Attributes Samples
// These demonstrate assembly-level configuration

/*
#region attributes-factorymode

// Full mode (default): Generate both local methods and remote stubs
// Use in shared domain assemblies that can run on both client and server
[assembly: FactoryMode(FactoryModeOption.Full)]

// RemoteOnly mode: Generate HTTP stubs only
// Use in client-only assemblies (e.g., Blazor WASM)
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]

#endregion

#region modes-full-config
// Full mode configuration in Program.cs
// Generates both local implementation and remote HTTP stubs
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Full,  // Full mode
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
#endregion

#region modes-logical-config
// Logical mode configuration for testing
// All methods execute locally without serialization
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Logical,  // Logical mode - no HTTP, no serialization
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
#endregion

#region modes-remote-config
// Remote mode configuration for Blazor WASM clients
// Generates HTTP stubs that call server
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,  // Remote mode - HTTP client stubs
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Required: Register HttpClient for remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri("https://api.example.com/") });
#endregion

#region modes-remoteonly-config
// RemoteOnly mode assembly attribute
// Client assemblies that only generate HTTP stubs
// Use when you have separate client and server projects
[assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
#endregion

#region modes-server-config
// Server mode configuration with ASP.NET Core integration
// Handles incoming remote requests from clients
builder.Services.AddNeatooAspNetCore(
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

var app = builder.Build();
app.UseNeatoo();  // Maps /api/neatoo endpoint
#endregion
*/

namespace EmployeeManagement.Domain.Samples.Modes;

// This file contains static configuration examples that would appear in Program.cs
// The examples above are wrapped in comments because they are not valid C# without context
// They demonstrate the configuration patterns for documentation snippets
