using System;
using System.Collections.Generic;
using System.Text;

namespace Neatoo.RemoteFactory.Internal;

public class ObjectJson
{
	public string Json { get; }
	public string AssemblyType { get; }

	public ObjectJson(string json, string assemblyType)
	{
		this.Json = json;
		this.AssemblyType = assemblyType;
	}
}
