using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory;

public class RemoteResponseDto
{
	[JsonConstructor]
	public RemoteResponseDto(string? json, IReadOnlyList<RelayedFactoryEvent>? relayedEvents = null)
	{
		this.Json = json;
		this.RelayedEvents = relayedEvents;
	}

	public string? Json { get; private set; }
	public IReadOnlyList<RelayedFactoryEvent>? RelayedEvents { get; private set; }
}
