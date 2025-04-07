using Neatoo.RemoteFactory.FactoryGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryGeneratorSandbox;

[Factory]
 public static partial class ExecuteFactory
 {

	[Execute]
	internal static int ExecuteMethod(string message)
	{
		Console.WriteLine($"Message: {message}");
		return 1;
	}

}
