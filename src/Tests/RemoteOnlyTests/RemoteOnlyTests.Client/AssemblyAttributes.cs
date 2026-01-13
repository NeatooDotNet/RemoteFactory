using Neatoo.RemoteFactory;
using System.Runtime.CompilerServices;

[assembly: FactoryMode(FactoryMode.RemoteOnly)]
[assembly: InternalsVisibleTo("RemoteOnlyTests.Integration")]
