# Design Source of Truth

This directory contains the **authoritative design reference** for RemoteFactory. These projects are specifically designed to help understand the RemoteFactory API through working code with extensive comments.

## Purpose

Design decisions are easily lost or contradicted when spread across documentation, samples, and implementation. These projects solve that by being:

1. **The single source of truth** - Updated first, everything else flows from it
2. **Heavily commented** - Not just "what" but "why" and "what we didn't do"
3. **Fully functional** - Compiles and tests pass, ensuring accuracy
4. **AI-optimized** - Structured for Claude Code to understand the API

## Project Structure

```
src/Design/
├── Design.sln                    # Solution file
├── Design.Domain/                # Domain model demonstrating all patterns
│   ├── FactoryPatterns/          # All three factory patterns side-by-side
│   │   └── AllPatterns.cs        # Class, Interface, Static factories
│   ├── Aggregates/               # DDD aggregate root example
│   │   └── Order.cs              # Full lifecycle with IFactorySaveMeta
│   ├── Entities/                 # Child entity example
│   │   └── OrderLine.cs          # No [Remote] - server-side only
│   └── ValueObjects/             # Value object example
│       └── Money.cs              # Record type, no [Factory]
├── Design.Tests/                 # Comprehensive test suite
│   ├── TestInfrastructure/       # Client/server container simulation
│   └── FactoryTests/             # Tests for each pattern
├── Design.Server/                # ASP.NET Core server
│   └── Program.cs                # AddNeatooAspNetCore() setup
└── Design.Client.Blazor/         # Blazor WASM client
    └── Program.cs                # AddNeatooRemoteFactory() setup
```

## The Three Factory Patterns

### 1. Class Factory
Apply `[Factory]` to a class to generate a factory interface for entity lifecycle.

```csharp
[Factory]
public partial class Order
{
    [Remote, Create]
    public void Create(string customerName, [Service] IOrderLineListFactory lineListFactory) { }

    [Remote, Fetch]
    public void Fetch(int id, [Service] IOrderLineListFactory lineListFactory) { }

    [Remote, Insert]
    public Task Insert([Service] IOrderRepository repository) { }
}
```

**Generated**: `IOrderFactory` with `Create()`, `Fetch()`, `Save()` methods.

### 2. Interface Factory
Apply `[Factory]` to an interface to create a remote proxy.

```csharp
[Factory]
public interface IExampleRepository
{
    Task<IReadOnlyList<ExampleDto>> GetAllAsync();
    Task<ExampleDto?> GetByIdAsync(int id);
}
```

**Generated**: Proxy that serializes calls to server where implementation runs.

### 3. Static Factory
Apply `[Factory]` to a static class for commands and events.

```csharp
[Factory]
public static partial class ExampleCommands
{
    [Remote, Execute]
    private static Task<bool> _SendNotification(
        string recipient,
        string message,
        [Service] INotificationService service) => service.SendAsync(recipient, message);
}
```

**Generated**: `ExampleCommands.SendNotification` delegate type.

## Key Design Decisions

### [Remote] Marks Client Entry Points
- Only aggregate root operations need `[Remote]`
- Child entity operations are server-side only (no `[Remote]`)
- Once on server, execution stays there

### Service Injection
- **Constructor injection** (`[Service]` on constructor): Available on both client and server
- **Method injection** (`[Service]` on method parameters): Server-only (common case)

### IFactorySaveMeta Controls Routing
```csharp
public interface IFactorySaveMeta
{
    bool IsNew { get; set; }
    bool IsDeleted { get; set; }
}
```
- `IsNew=true, IsDeleted=false` → Insert
- `IsNew=false, IsDeleted=true` → Delete
- Otherwise → Update

### Serialization Requirements
- Properties need **public setters** for JSON serialization
- Value objects (records) work well with init-only properties
- Factory references in collections are NOT preserved across serialization

## Running the Projects

### Run Tests
```bash
cd src/Design
dotnet test Design.sln
```

### Run Server
```bash
cd src/Design/Design.Server
dotnet run
```
Server runs at `http://localhost:5000` with RemoteFactory endpoint at `/remotefactory`.

### Run Blazor Client
```bash
cd src/Design/Design.Client.Blazor
dotnet run
```
Opens interactive sample demonstrating all three patterns.

## Comment Patterns

The code uses specific comment patterns to document decisions:

| Pattern | Purpose |
|---------|---------|
| `DESIGN DECISION:` | Explains why something is done this way |
| `DID NOT DO THIS:` | Documents rejected alternatives and why |
| `GENERATOR BEHAVIOR:` | Explains what the source generator creates |
| `COMMON MISTAKE:` | Warns about frequent errors |

## Maintenance

When making design changes to RemoteFactory:

1. **Update Design.Domain first** - Add/modify the pattern demonstration
2. **Add tests** - Verify the behavior in Design.Tests
3. **Update comments** - Document the decision and alternatives
4. **Then update main codebase** - Implementation follows design

This ensures design decisions are captured before they're implemented.
