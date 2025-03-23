using Neatoo.RemoteFactory.FactoryGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryGeneratorSandbox;

[Factory]
 public static class ExecuteFactory
 {
	public delegate int Execute(string message);

	[Execute<Execute>]
	public static int DoExecute(string message)
	{
		Console.WriteLine($"Message: {message}");
		return 1;
	}
}
