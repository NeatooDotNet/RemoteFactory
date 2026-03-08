using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Source-generated JSON serializer context for transport DTOs.
/// Ensures trimming-safe serialization of RemoteRequestDto and RemoteResponseDto
/// (and their nested types like ObjectJson) by generating all type metadata at compile time.
/// This eliminates the need for runtime reflection to read constructor parameter names,
/// which the IL trimmer would otherwise strip from assemblies marked IsTrimmable.
/// </summary>
[JsonSourceGenerationOptions(System.Text.Json.JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(RemoteRequestDto))]
[JsonSerializable(typeof(RemoteResponseDto))]
[JsonSerializable(typeof(ObjectJson))]
internal partial class NeatooTransportJsonContext : JsonSerializerContext
{
}
