# Architect -- Generator-Emitted DTO Constructor Lambdas

Last updated: 2026-03-25
Current step: Step 2 -- Comprehension Check

## Key Context

- v0.23.2 shipped an `Activator.CreateInstance` fallback in `NeatooJsonTypeInfoResolver` -- it does NOT work under IL trimming because `Activator.CreateInstance` needs the same constructor metadata the trimmer strips
- Reproduced: publish Design.Server in Release/net10.0, navigate to Blazor page, click "Get All Items" -> `DeserializeNoConstructor` for `ExampleDto`
- The correct fix is generator-emitted `() => new Dto()` lambdas -- static references the trimmer can trace
- Developer review of the original (pre-v0.23.2) plan correctly identified that DTO discovery must happen in `FactoryModelBuilder` where `ITypeSymbol` is available via `TypeFactoryMethodInfo.ReturnType`, not in renderers where return types are already strings
- The `NeatooJsonTypeInfoResolver` already has the pattern: `CreateObject` is set for DI types; the `else if` branch with `Activator.CreateInstance` can be replaced with a registry lookup
- `FactoryGenerationUnit` is the output of `FactoryModelBuilder.Build()` -- it's a sealed record that flows through the incremental pipeline, so any new data (DTO types list) must be added there with proper equatability
- The existing `FactoryServiceRegistrar` pattern in renderers shows how generated code registers services -- the DTO constructor lambdas can follow a similar pattern (generated static method called at startup)

## Mistakes to Avoid

- Do NOT rely on `Activator.CreateInstance` -- it fails under trimming (same metadata issue)
- Do NOT rely on `[DynamicallyAccessedMembers]` -- .NET team called it "false sense of hope"
- Do NOT try to discover DTOs in renderers -- return types are strings there, not ITypeSymbols
- Records with parameterized constructors must be excluded -- they're handled by `RecordBypassConverterFactory`

## User Corrections

- User chose Option 3 (explicit `CreateObject` lambdas) over JsonSerializerContext or enhanced IJsonTypeInfoResolver in the prior attempt
- User confirmed Activator.CreateInstance approach failed and we need the generator-emitted lambda approach from the original plan
