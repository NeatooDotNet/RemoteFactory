using Neatoo.RemoteFactory;

// This assembly is the CLIENT version of the domain layer.
// It generates factories with remote HTTP stubs only - no local implementations.
// Entity methods that call [Service] parameters are not generated.
[assembly: FactoryMode(FactoryMode.RemoteOnly)]
