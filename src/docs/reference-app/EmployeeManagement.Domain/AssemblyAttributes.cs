using Neatoo.RemoteFactory;

#region attributes-factoryhintnamelength
// Increase hint name length to accommodate long namespace/type names
// Use when hitting Windows path length limits (260 characters)
[assembly: FactoryHintNameLength(100)]
#endregion
