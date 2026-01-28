// Assembly-level attributes are configured in AssemblyAttributes.cs
// These samples demonstrate the usage patterns.

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-factorymode
// Assembly-level factory mode configuration examples:
//
// Server assembly (default mode):
// [assembly: FactoryMode(FactoryMode.Full)]
// - Generates local and remote execution paths
// - Use in server/API projects
//
// Client assembly (remote-only mode):
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]
// - Generates HTTP stubs only, no local execution
// - Use in Blazor WebAssembly and other client projects
#endregion

#region attributes-factoryhintnamelength
// Assembly-level hint name length configuration:
//
// [assembly: FactoryHintNameLength(100)]
// - Limits generated file hint name length to 100 characters
// - Use when hitting Windows 260-character path limits
// - Value is maximum characters for the generated file hint name
//
// Default behavior uses full type names which can be long
// for deeply nested namespaces or generic types.
#endregion
