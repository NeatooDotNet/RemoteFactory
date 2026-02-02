# Decision Guide

Quick answers to common "when should I use...?" questions.

---

## When Do I Need [Remote]?

```
Is this method called directly from client code (UI, Blazor component)?
├── YES → Add [Remote]
└── NO (called from other server-side code)
    └── No [Remote] needed
```

**Rule of thumb**: `[Remote]` marks **entry points** from client to server. Once you're on the server, subsequent calls stay there.

**Examples**:
- `Fetch()` called by Blazor component → `[Remote, Fetch]`
- `LoadChildren()` called by `Fetch()` on the server → just `[Fetch]` (no `[Remote]`)
- `Insert()` called by `Save()` routed from client → `[Remote, Insert]`

---

## Constructor vs Method Injection?

```
Does the client need this service?
├── YES (validation, logging, client-side logic)
│   └── Constructor injection: [Service] on constructor parameter
└── NO (database, secrets, server-only)
    └── Method injection: [Service] on method parameter
```

**Rule of thumb**: Method injection is the common case. Use constructor injection only when clients need the service too.

**Examples**:
- `IEmployeeRepository` → Method injection (server-only, database access)
- `IValidator<Employee>` → Constructor injection (validate on client and server)
- `ILogger` → Constructor injection (log on both sides)
- `DbContext` → Method injection (server-only)

---

## Which Serialization Format?

```
Can you deploy client and server together?
├── YES (same release cycle)
│   └── Ordinal (40-50% smaller payloads)
└── NO (independent deployments, rolling updates)
    └── Named (version tolerant)
```

**Rule of thumb**: Ordinal is the default and works well when client and server are released together. Use Named if you need independent version compatibility.

**Trade-offs**:
| Format | Payload Size | Version Tolerance | Debugging |
|--------|--------------|-------------------|-----------|
| Ordinal | Smaller (40-50%) | Requires matching versions | Harder to read |
| Named | Larger | Tolerates property changes | Easier to read |

---

## Do I Need IFactorySaveMeta?

```
Does your entity support Insert, Update, and Delete via Save()?
├── YES → Implement IFactorySaveMeta
│         (adds IsNew, IsDeleted properties)
└── NO (read-only, or explicit Insert/Update/Delete calls)
    └── Don't implement it
```

**Rule of thumb**: `IFactorySaveMeta` enables the `factory.Save()` routing pattern. If you only need `Fetch()`, skip it.

---

## Full vs RemoteOnly Mode?

```
What type of assembly is this?
├── Server (ASP.NET Core)
│   └── Full mode (default) + NeatooFactory.Server
├── Client (Blazor WASM)
│   └── RemoteOnly mode + NeatooFactory.Remote
└── Shared domain library
    └── Full mode (default) - works in both contexts
```

**Configuration**:
```csharp
// Client assembly - add to AssemblyAttributes.cs
[assembly: FactoryMode(FactoryModeOption.RemoteOnly)]

// Server registration
services.AddNeatooAspNetCore(...);  // Implies Server mode

// Client registration
services.AddNeatooRemoteFactory(NeatooFactory.Remote, ...);
```

---

## When to Use [Execute] vs Entity Methods?

```
Is this operation tied to a specific entity instance?
├── YES → Use entity method with [Insert], [Update], etc.
└── NO (cross-cutting, batch, or stateless operation)
    └── Use static class with [Execute]
```

**Examples**:
- Promote an employee → Entity method: `employee.Promote()` with `[Update]`
- Transfer employee to new department → Static: `TransferEmployeeCommand` with `[Execute]`
- Generate monthly report → Static: `GenerateReportCommand` with `[Execute]`

---

## When to Use [Event]?

```
Should the caller wait for this operation to complete?
├── YES → Use regular method (or [Execute])
└── NO (notifications, logging, side effects)
    └── Use [Event]
```

**Rule of thumb**: Events are fire-and-forget. The caller continues immediately. Use for side effects that shouldn't block the main operation.

**Requirements**:
- `CancellationToken` must be the last parameter
- Returns `void` or `Task`

---

## Custom Authorization vs [AspAuthorize]?

```
What authorization style do you need?
├── Fine-grained, operation-specific logic
│   └── [AuthorizeFactory<T>] with custom interface
├── ASP.NET Core policies/roles
│   └── [AspAuthorize] on methods
└── Both
    └── Combine them (both checks must pass)
```

**Examples**:
```csharp
// Custom authorization - full control
[AuthorizeFactory<IEmployeeAuthorization>]
public class Employee { ... }

// ASP.NET Core policies - simple roles/policies
[Remote, Delete]
[AspAuthorize(Roles = "Admin")]
public Task Delete(...) { ... }

// Combined - both must pass
[AuthorizeFactory<IEmployeeAuthorization>]  // Check 1
public class Employee
{
    [Remote, Delete]
    [AspAuthorize(Roles = "Admin")]  // Check 2
    public Task Delete(...) { ... }
}
```

---

## Next Steps

- [Attributes Reference](attributes-reference.md) - Complete attribute documentation
- [Client-Server Architecture](client-server-architecture.md) - Understanding `[Remote]`
- [Service Injection](service-injection.md) - DI patterns
- [Factory Modes](factory-modes.md) - Full vs RemoteOnly configuration
