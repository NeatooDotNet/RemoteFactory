using System.Text.Json.Serialization;
using System.Threading;

namespace Neatoo.RemoteFactory.Internal;

public sealed class NeatooReferenceHandler : ReferenceHandler
{
	public AsyncLocal<ReferenceResolver> ReferenceResolver { get; set; } = new();

	public override ReferenceResolver CreateResolver()
	{
		return this.ReferenceResolver.Value!;
	}
}
