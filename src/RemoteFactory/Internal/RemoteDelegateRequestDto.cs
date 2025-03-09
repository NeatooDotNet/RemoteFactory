using System;
using System.Collections.Generic;
using System.Text;

namespace Neatoo.RemoteFactory.Internal;

public class RemoteDelegateRequestDto
{
	public string DelegateAssemblyType { get; set; } = null!;
	public IReadOnlyCollection<ObjectJson?>? Parameters { get; set; }
	public ObjectJson? Target { get; set; }
}

public class RemoteDelegateRequest
{
	public Type? DelegateType { get; set; } = null!;
	public IReadOnlyCollection<object?>? Parameters { get; set; }
	public object? Target { get; set; }
}
