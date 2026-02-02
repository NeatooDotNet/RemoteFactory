// Factory Mode configuration one-liners for documentation
// These show the essential API for each mode

namespace EmployeeManagement.Domain.Samples.Modes;

// Minimal configuration snippets - kept as comments since they're Program.cs patterns

#region modes-full-config
// Full mode (default): no assembly attribute needed
// services.AddNeatooRemoteFactory(NeatooFactory.Server, options, domainAssembly);
#endregion

#region modes-logical-config
// Logical mode: local execution, no HTTP - ideal for testing
// services.AddNeatooRemoteFactory(NeatooFactory.Logical, options, domainAssembly);
#endregion

#region modes-remote-config
// Remote mode: all operations serialize and POST to server
// services.AddNeatooRemoteFactory(NeatooFactory.Remote, options, domainAssembly);
// services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
//     new HttpClient { BaseAddress = new Uri(serverUrl) });
#endregion

#region modes-remoteonly-config
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
// Generates HTTP stubs only - smaller client assemblies
#endregion

#region modes-server-config
// Server mode: handles incoming HTTP + local execution
// services.AddNeatooAspNetCore(options, domainAssembly);
// app.UseNeatoo();  // Maps /api/neatoo endpoint
#endregion
