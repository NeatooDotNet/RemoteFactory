using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

public class RemoteRequestDto
{
	public string DelegateAssemblyType { get; set; } = null!;
	public IReadOnlyCollection<ObjectJson?>? Parameters { get; set; }
	public ObjectJson? Target { get; set; }
}

public class RemoteRequest
{
	public Type DelegateType { get; set; } = null!;
	public IReadOnlyCollection<object?>? Parameters { get; set; }
	public object? Target { get; set; }
}
