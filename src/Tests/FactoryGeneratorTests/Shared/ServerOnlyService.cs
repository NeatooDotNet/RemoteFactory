using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

public interface IServerOnlyService { }

// Name doesn't match so it doest't get automagic registered
class ServerOnly : IServerOnlyService
{
}
