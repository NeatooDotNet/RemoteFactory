using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory.Internal;

public class RemoteResponseDto
{
	[JsonConstructor]
	public RemoteResponseDto(string? json)
	{
		this.Json = json;
	}

	public string? Json { get; private set; }
}
