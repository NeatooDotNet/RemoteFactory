# Register [Execute] Parameter DTOs in DtoConstructorRegistry

## Problem

The source generator registers return types in `DtoConstructorRegistry` for IL trimming support, but does not register plain DTO types used as **parameters** to `[Execute]` methods. When Blazor WASM trimming is active, these parameter types lose their property metadata, causing serialization to produce empty JSON objects on the client. The server then receives default/empty values.

## Example

```csharp
[Factory]
public static partial class AdminCommands
{
    [Remote, Execute]
    private static async Task<AdminCommandResult> _UpdateUser(
        string id,
        AdminUpdateUserRequest request,  // <-- this DTO type is NOT registered
        [Service] IAdminService adminService)
    {
        return await adminService.UpdateUserAsync(id, request);
    }
}
```

Generated code registers `AdminCommandResult` (return type) but not `AdminUpdateUserRequest` (parameter type):

```csharp
// Generated — return types only
DtoConstructorRegistry.Register<AdminCommandResult>(() => new AdminCommandResult());
// Missing: DtoConstructorRegistry.Register<AdminUpdateUserRequest>(() => new AdminUpdateUserRequest());
```

## Impact

- Properties on parameter DTOs are silently stripped by the IL trimmer
- Serialization on the WASM client produces `{}` or default values
- Server-side validation fails with misleading errors (e.g., "First name is required" when the name was clearly visible on screen)
- Only affects Release/published builds with trimming — Debug builds work fine

## Workaround

Consumers must manually add parameter DTO types to a `LinkerConfig.xml`:

```xml
<type fullname="MyApp.DomainModels.Admin.AdminUpdateUserRequest" preserve="all" />
```

## Fix

The source generator should scan `[Execute]` method parameters for non-primitive, non-`[Service]` types and emit `DtoConstructorRegistry.Register<T>()` calls for them, the same way it does for return types.
