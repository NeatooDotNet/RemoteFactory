# RemoteFactory - .NET Runtime Debugger Memory

## Trimming Configuration
- `src/RemoteFactory/RemoteFactory.csproj` has `<IsTrimmable>true</IsTrimmable>` and `<EnableTrimAnalyzer>false</EnableTrimAnalyzer>`
- Transport DTOs (`RemoteRequestDto`, `RemoteResponseDto`, `ObjectJson`) cross the HTTP boundary using System.Text.Json
- Source-generated `NeatooTransportJsonContext` added in `src/RemoteFactory/Internal/NeatooTransportJsonContext.cs` to make transport DTO serialization trimming-safe
- `MakeRemoteDelegateRequestHttpCall.cs` uses the source-generated context for `JsonContent.Create()` and `ReadFromJsonAsync()`
- The NeatooJsonSerializer (custom serializer) uses `DefaultJsonTypeInfoResolver` subclass -- separate concern from transport DTOs

## Key Serialization Paths
- **Client-to-server (HTTP)**: `JsonContent.Create(request, NeatooTransportJsonContext.Default.RemoteRequestDto)` -- trimming-safe
- **Server-to-client (HTTP)**: `ReadFromJsonAsync(NeatooTransportJsonContext.Default.RemoteResponseDto)` -- trimming-safe
- **Domain object serialization**: `NeatooJsonSerializer` with custom `NeatooJsonTypeInfoResolver` and `NeatooOrdinalConverterFactory` -- uses reflection, separate trimming concern
- **Test containers**: `MakeSerializedServerStandinDelegateRequest` uses default `JsonSerializer.Serialize/Deserialize` -- not trimmed in test context

## Known Trimming Risk Areas
- `NeatooOrdinalConverterFactory.CreateConverter()` uses reflection to find `IOrdinalConverterProvider<T>` methods
- `NeatooOrdinalConverter<T>` static constructor uses reflection to read `IOrdinalSerializationMetadata` properties
- `HandleRemoteDelegateRequest` uses `DynamicInvoke` and `GetProperty("Result")` on Task
- `AddRemoteFactoryServices.RegisterFactories()` uses `Assembly.GetTypes()` reflection
- `NeatooJsonTypeInfoResolver.GetTypeInfo()` uses `ServiceProviderIsService.IsService()` -- runtime DI check
