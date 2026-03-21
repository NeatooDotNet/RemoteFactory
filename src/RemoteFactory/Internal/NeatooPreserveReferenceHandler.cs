using System;
using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Bridges STJ's <see cref="ReferenceHandler"/> API to the existing
/// <see cref="NeatooReferenceResolver.Current"/> AsyncLocal. This enables
/// STJ's built-in converters (for Dictionary, List, plain classes, etc.)
/// to participate in the same reference tracking used by Neatoo's custom
/// converters, preserving shared-instance identity across the full
/// serialization graph.
/// </summary>
/// <remarks>
/// <para>
/// STJ calls <see cref="CreateResolver"/> once at the start of each
/// top-level serialize/deserialize operation. By returning
/// <see cref="NeatooReferenceResolver.Current"/>, both STJ built-in
/// converters and Neatoo custom converters share the same resolver
/// instance and ID counter.
/// </para>
/// <para>
/// The resolver lifecycle is managed by <see cref="NeatooJsonSerializer"/>:
/// it creates a new <see cref="NeatooReferenceResolver"/>, sets
/// <see cref="NeatooReferenceResolver.Current"/>, calls
/// <c>JsonSerializer.Serialize/Deserialize</c>, then clears Current
/// in a finally block. This handler simply returns whatever Current
/// is at the time STJ calls <see cref="CreateResolver"/>.
/// </para>
/// </remarks>
internal sealed class NeatooPreserveReferenceHandler : ReferenceHandler
{
	public override ReferenceResolver CreateResolver()
	{
		return NeatooReferenceResolver.Current
			?? throw new InvalidOperationException(
				"NeatooReferenceResolver.Current is null. " +
				"A NeatooReferenceResolver must be created and set as Current before serialization. " +
				"This handler should only be used with NeatooJsonSerializer, which manages the resolver lifecycle.");
	}
}
