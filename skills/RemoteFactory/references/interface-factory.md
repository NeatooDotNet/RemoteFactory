# Interface Factory Pattern

Use for remote services where the client calls through a generated proxy. The implementation lives only on the server.

## Complete Example

<!-- snippet: skill-interface-factory-complete -->
<a id='snippet-skill-interface-factory-complete'></a>
```cs
[Factory]
public interface IEmployeeQueryService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<int> CountAsync();
}

// Server implementation (no [Factory] attribute)
public class EmployeeQueryService : IEmployeeQueryService
{
    private readonly List<EmployeeDto> _employees = new()
    {
        new EmployeeDto { Id = 1, Name = "John Doe", Department = "Engineering" },
        new EmployeeDto { Id = 2, Name = "Jane Smith", Department = "Marketing" }
    };

    public Task<IReadOnlyList<EmployeeDto>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<EmployeeDto>>(_employees);
    }

    public Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(employee);
    }

    public Task<int> CountAsync()
    {
        return Task.FromResult(_employees.Count);
    }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/InterfaceFactorySamples.cs#L5-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-interface-factory-complete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**: Proxy implementation that serializes calls to server.

---

## Critical Rules

### Interface methods do NOT need operation attributes

```csharp
// WRONG - emits NF0106 (factory-operation attribute on interface factory method)
[Factory]
public interface IMyRepository
{
    [Fetch]  // DON'T DO THIS — NF0106 error
    Task<Item> GetByIdAsync(int id);
}

// RIGHT - no attributes needed
[Factory]
public interface IMyRepository
{
    Task<Item> GetByIdAsync(int id);
}
```

The interface IS the remote boundary. Every method crosses it automatically. Enforced by **NF0106** — factory-operation attributes (`[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]`/`[Execute]`) on any `[Factory]` interface method are a compile-time error. Applies with or without `[AuthorizeFactory<T>]` on the interface.

### Server implementation does NOT have [Factory]

```csharp
// WRONG - causes duplicate registration
[Factory]
public class EmployeeRepository : IEmployeeRepository { }

// RIGHT - no [Factory] on implementation
public class EmployeeRepository : IEmployeeRepository { }
```

The interface already has `[Factory]`; the implementation is just a service.

---

## Server Registration

Register the implementation in DI:

```csharp
// Program.cs
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
```

Or use convention-based registration:
```csharp
builder.Services.RegisterMatchingName<IEmployeeRepository>();  // Auto-finds EmployeeRepository
```

---

## When to Use Interface Factory

- **Remote services without entity identity** - Query services, report generators
- **Clean separation** - Interface defines contract, server provides implementation
- **Multiple implementations** - Can swap implementations via DI
- **Third-party integrations** - Wrap external APIs behind interface

---

## Authorization with `[AuthorizeFactory<T>]`

Interface factories support `[AuthorizeFactory<T>]` for server-side authorization of remote calls — but the scope model is different from class factories.

### Scope model — use `Execute` / `Read`, not CRUD

Interface factories have no Create/Fetch/Insert/Update/Delete operations (every interface method is just a remote call), so CRUD scopes silently never match. Use:

- **`AuthorizeFactoryOperation.Execute`** — fires on every interface method call. The broad-scope default for interface factories.
- **`AuthorizeFactoryOperation.Read`** — also fires on every interface method call. Use for "read-like" semantics on the auth side (identical runtime effect to `Execute` on an interface factory).

Using `Create` / `Fetch` / `Insert` / `Update` / `Delete` scopes on an interface-factory auth class is a silent no-op — the auth method is never invoked. The generator does not warn about this today.

### Per-method authorization via parameter matching

Fine-grained per-method auth comes from **parameter matching by type**, not per-operation scopes. An auth method declaring parameters typed to match an interface-method's parameters receives the forwarded values from the call site. Auth methods can return `bool`, `Task<bool>`, `string?`, or `Task<string?>`. String returns surface denial messages in `NotAuthorizedException.Message`.

### Complete example

**Auth class:**

<!-- snippet: skill-interface-factory-auth-authclass -->
<a id='snippet-skill-interface-factory-auth-authclass'></a>
```cs
// Auth class — scopes are Execute or Read on interface factories. CRUD scopes
// (Create/Fetch/Insert/Update/Delete) silently never match interface methods.
public interface IEmployeeQueryAuth
{
    // Parameterless — fires on every interface method call.
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool HasAccess();

    // Parameterized — Guid matched by TYPE. Fires on every interface method
    // whose signature includes a Guid parameter; the generator forwards the
    // value from the call site. Per-entity authorization without needing
    // per-operation attributes.
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanAccessEmployee(Guid id);

    // String-returning: null/empty = authorized, non-empty = denial message.
    // The string surfaces in NotAuthorizedException.Message.
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    string? CheckReadAccess();
}

public class EmployeeQueryAuth : IEmployeeQueryAuth
{
    private readonly IUserContext _userContext;

    public EmployeeQueryAuth(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool HasAccess() => _userContext.IsAuthenticated;

    public bool CanAccessEmployee(Guid id) =>
        _userContext.IsInRole("HRManager") || _userContext.IsInRole("Admin");

    public string? CheckReadAccess() =>
        _userContext.IsInRole("ReadOnly") ? "Tenant is read-only" : null;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/InterfaceFactoryAuthSamples.cs#L14-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-interface-factory-auth-authclass' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Interface factory (bare contract — no method attributes):**

<!-- snippet: skill-interface-factory-auth-factory -->
<a id='snippet-skill-interface-factory-auth-factory'></a>
```cs
// Interface factory — bare interface, NO attributes on methods.
// Placing [Create]/[Fetch]/[Insert]/[Update]/[Delete]/[Execute] on any method
// here is a compile error (NF0106). Fine-grained auth comes from parameter
// matching on the auth class, not operation attributes on the contract.
[Factory]
[AuthorizeFactory<IEmployeeQueryAuth>]
public interface IAuthorizedEmployeeQuery
{
    Task<EmployeeRecord?> GetEmployee(Guid id);
    Task<EmployeeRecord> UpdateEmployee(Guid id, string name);
    Task<IReadOnlyList<EmployeeRecord>> ListByDepartment(Guid id);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/InterfaceFactoryAuthSamples.cs#L55-L68' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-interface-factory-auth-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Server-side impl (plain class — no `[Factory]`, no op attributes):**

<!-- snippet: skill-interface-factory-auth-impl -->
<a id='snippet-skill-interface-factory-auth-impl'></a>
```cs
// Server-side implementation — plain class, NO [Factory], NO operation
// attributes. Registered on the server only (not the client).
public class AuthorizedEmployeeQuery : IAuthorizedEmployeeQuery
{
    public Task<EmployeeRecord?> GetEmployee(Guid id) =>
        Task.FromResult<EmployeeRecord?>(new EmployeeRecord { Id = id, Name = "Alice", Department = "HR" });

    public Task<EmployeeRecord> UpdateEmployee(Guid id, string name) =>
        Task.FromResult(new EmployeeRecord { Id = id, Name = name, Department = "HR" });

    public Task<IReadOnlyList<EmployeeRecord>> ListByDepartment(Guid id) =>
        Task.FromResult<IReadOnlyList<EmployeeRecord>>([]);
}

// DI registration (server only):
//   builder.Services.AddScoped<IAuthorizedEmployeeQuery, AuthorizedEmployeeQuery>();
//
// The auth class (EmployeeQueryAuth) is auto-registered by the generator via
// services.TryAddTransient<IEmployeeQueryAuth, EmployeeQueryAuth>() in the
// generated FactoryServiceRegistrar — no manual registration needed.
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/InterfaceFactoryAuthSamples.cs#L70-L91' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-interface-factory-auth-impl' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The auth class is auto-registered by the generator via `services.TryAddTransient<IEmployeeQueryAuth, EmployeeQueryAuth>()` in the generated `FactoryServiceRegistrar` — no manual registration needed. Register the impl on the server only (not the client).

### Generated surface

For each interface method, the generator emits on `IAuthorizedRepositoryFactory`:

- **`{Method}(params)`** — normal forwarding method. `Local{Method}` on the server invokes all applicable auth methods (parameterless + parameter-matched by signature) and throws `NotAuthorizedException` on any denial before calling the impl.
- **`Can{Method}(matching-params, CancellationToken = default)`** — non-throwing helper returning `Authorized`. Runs the same scoped+parameter-matched auth checks. Useful for UI disable-states.

Example: `IAuthorizedRepository.GetItem(Guid id)` generates `CanGetItem(Guid id, CancellationToken ct = default)`.

`Can{Method}` parameters include only those matched by auth methods — e.g., `UpdateItem(Guid id, string name)` → `CanUpdateItem(Guid id)` (the `string name` is dropped because no auth method takes a string parameter).

### Failure behavior

- **Method path:** any denied auth check throws `NotAuthorizedException`. For string-returning auth methods, the denial message appears in `Exception.Message`.
- **`Can{Method}` path:** returns `Authorized` with `HasAccess=false` (no throw).

Unlike class-factory auth, interface factories have no "null on Read failure" behavior — denied calls always throw. There is no `TrySave` equivalent for interface factories (no Save semantics).

### Contrast with class factory auth

| Aspect | Class Factory | Interface Factory |
|--------|--------------|-------------------|
| Auth scopes used | `Create`/`Fetch`/`Insert`/`Update`/`Delete`/`Read`/`Write` | `Execute`/`Read` only |
| Per-method scoping | Via operation attribute on factory method | Via parameter matching by type |
| Generated `Can*` methods | `CanCreate`/`CanFetch`/`CanSave`/`CanDelete` | `Can{Method}` per interface method |
| Failure on Read | Returns `null` | Throws `NotAuthorizedException` |
| `TrySave` available | Yes (with `IFactorySaveMeta`) | No |
| Op attributes on contract | `[Create]`/etc. on class factory methods | **Forbidden** (NF0106) |

See `references/advanced-patterns.md` for class-factory auth (CRUD scopes, `CanSave` aggregation, target parameters).
