// Assembly-level attributes for RemoteFactory configuration

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-factorymode
// Full mode (default): generates local and remote code
// [assembly: FactoryMode(FactoryModeOption.Full)]

// RemoteOnly mode: generates HTTP stubs only (use in Blazor WASM)
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
#endregion

#region attributes-factoryhintnamelength
// Limits generated file name length for Windows path limits
// [assembly: FactoryHintNameLength(100)]
#endregion
