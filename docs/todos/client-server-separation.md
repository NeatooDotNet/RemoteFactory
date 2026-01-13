# Client/Server Code Separation

## Goal

Enable minimal client assemblies that contain:
- Entity properties (for serialization/binding)
- Validation rules (for real-time feedback)
- Remote factory stubs (HTTP calls to server)

**Exclude from client:**
- Factory method implementations (Fetch/Insert/Update/Delete/Execute bodies)
- Database types and infrastructure
- Child factories

**Benefits:**
- Smaller client assembly size
- No business logic to reverse engineer
- No persistence code or schema hints on client

## Architecture

### Project Structure

```
/Domain.Shared/                    # Shared project (.shproj or linked files)
  IPerson.cs                       # Interface
  Person.cs                        # Properties, rules, partial declarations

/Domain.Client/
  Domain.Client.csproj
  [assembly: FactoryMode(FactoryMode.RemoteOnly)]
  # Generator produces: remote stubs for [Remote] partial methods

/Domain.Server/
  Domain.Server.csproj
  Person.Server.cs                 # Partial implementations + server constructor
  # Generator produces: full factory wiring
```

### What Goes Where

| Component | Shared | Client Assembly | Server Assembly |
|-----------|--------|-----------------|-----------------|
| Entity properties | ✓ | ✓ | ✓ |
| Validation rules | ✓ | ✓ | ✓ |
| `[Create]` (simple entities) | ✓ | ✓ | ✓ |
| `[Create]` (aggregates with children) | Declaration | Remote stub | Implementation |
| `[Fetch/Insert/Update/Delete]` | Declaration | Remote stub | Implementation |
| Server constructor | | | ✓ |
| Child factories | | | ✓ |

## Key Design Decisions

### 1. Partial Methods for Remote Operations

Use C# partial methods where:
- **Declaration** (shared): Method signature with `[Remote]` attribute, no body
- **Implementation** (server): Method body with business logic

```csharp
// Shared
[Remote, Fetch]
public partial void Fetch(Guid id);

// Server
public partial void Fetch(Guid id)
{
    var entity = _db.Persons.Find(id);
    // ...
}
```

### 2. Server Constructor for Service Injection

Server-only services (DbContext, repositories) injected via additional constructor:

```csharp
// Shared - base constructor
public Person(IEntityBaseServices<Person> services) : base(services)
{
    // Rules - run on both client and server
}

// Server - additional constructor, chains to base
public Person(IEntityBaseServices<Person> services, IDbContext db) : this(services)
{
    _db = db;
}
```

**Rationale:** Avoids method-level `[Service]` parameters that reference server-only types. Partial method signatures must match exactly.

### 3. Child Factories Only on Server

Child factories don't exist on client. Children are:
- Created by parent's `[Create]` on server
- Fetched as part of aggregate by parent's `[Fetch]` on server
- Serialized to client as part of aggregate

### 4. Client-Side Create for Simple Entities

Entities without children can have local `[Create]`:

```csharp
// No [Remote] needed - runs on client
[Create]
public void Create()
{
    Id = Guid.NewGuid();
}
```

Aggregates with children need `[Remote, Create]` because they require child factories:

```csharp
// Must be remote - needs child factory
[Remote, Create]
public partial void Create();

// Server implementation
public partial void Create()
{
    Id = Guid.NewGuid();
    Lines = _lineListFactory.Create();  // Child factory only exists on server
}
```

### 5. Rules on Both Client and Server

Validation rules run on both sides:
- **Client**: Real-time validation feedback as user types
- **Server**: Validation before persistence (can't trust client)

Rules are defined in the shared project.

## Generator Changes Required

### New Assembly Attribute

```csharp
[assembly: FactoryMode(FactoryMode.RemoteOnly)]

public enum FactoryMode
{
    Full,       // Default - generate everything
    RemoteOnly  // Only generate remote stubs for [Remote] methods
}
```

### Generator Logic Changes

1. **Detect partial methods without bodies:**
   ```csharp
   var isPartialWithoutBody = methodSymbol.IsPartialDefinition
       && methodSymbol.PartialImplementationPart == null;
   ```

2. **In RemoteOnly mode:**
   - Only process methods with `[Remote]` attribute
   - For partial methods without body: generate remote call as implementation
   - Skip methods without `[Remote]`
   - Generate factory interface with remote methods only

3. **Prefer constructor with more parameters:**
   - When multiple constructors exist, factory uses the one with more parameters
   - Server constructor (with services) preferred over base constructor

### Generated Code

**Client (RemoteOnly mode):**
```csharp
// Generated implementation for partial declaration
public partial void Fetch(Guid id)
{
    // Remote call implementation
}
```

**Server (Full mode):**
- Uses source implementation
- Wires factory to implementation

## Tasks

- [ ] Add `FactoryMode` enum and `FactoryModeAttribute`
- [ ] Update generator to check for assembly-level `FactoryMode`
- [ ] Detect partial method declarations without implementations
- [ ] In RemoteOnly mode, generate remote stubs for `[Remote]` partials
- [ ] In RemoteOnly mode, skip non-`[Remote]` methods
- [ ] Update constructor resolution to prefer more parameters
- [ ] Add documentation for shared project pattern
- [ ] Create example solution demonstrating the pattern
- [ ] Test: Simple entity with client-side Create
- [ ] Test: Aggregate with children requiring remote Create
- [ ] Test: Server constructor with injected services
- [ ] Test: Rules executing on both client and server

## Example: Complete Pattern

### Shared/IOrder.cs
```csharp
public partial interface IOrder : IEntityBase
{
    Guid Id { get; }
    string? CustomerName { get; set; }
    IOrderLineList Lines { get; }
}
```

### Shared/Order.cs
```csharp
[Factory]
internal partial class Order : EntityBase<Order>, IOrder
{
    public Order(IEntityBaseServices<Order> services) : base(services)
    {
        RuleManager.AddValidation(
            t => string.IsNullOrEmpty(t.CustomerName) ? "Customer required" : "",
            t => t.CustomerName);
    }

    public partial Guid Id { get; set; }
    public partial string? CustomerName { get; set; }
    public partial IOrderLineList Lines { get; set; }

    // Remote - needs child factory
    [Remote, Create]
    public partial void Create();

    [Remote, Fetch]
    public partial void Fetch(Guid id);

    [Remote, Insert]
    public partial Task Insert();

    [Remote, Update]
    public partial Task Update();
}
```

### Server/Order.Server.cs
```csharp
internal partial class Order
{
    private readonly IAppDbContext _db;
    private readonly IOrderLineListFactory _lineListFactory;

    public Order(
        IEntityBaseServices<Order> services,
        IAppDbContext db,
        IOrderLineListFactory lineListFactory) : this(services)
    {
        _db = db;
        _lineListFactory = lineListFactory;
    }

    public partial void Create()
    {
        Id = Guid.NewGuid();
        Lines = _lineListFactory.Create();
    }

    public partial void Fetch(Guid id)
    {
        var entity = _db.Orders.Include(o => o.Lines).First(o => o.Id == id);
        LoadProperty(o => o.Id, entity.Id);
        LoadProperty(o => o.CustomerName, entity.CustomerName);
        Lines = _lineListFactory.Fetch(entity.Lines);
    }

    public partial Task Insert()
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = new OrderEntity { Id = Id, CustomerName = CustomerName };
        _lineListFactory.Insert(Lines, entity.Lines);
        _db.Orders.Add(entity);
        await _db.SaveChangesAsync();
    }

    public partial Task Update()
    {
        await RunRules();
        if (!IsSavable) return;

        var entity = _db.Orders.Include(o => o.Lines).First(o => o.Id == Id);
        if (this[nameof(CustomerName)].IsModified)
            entity.CustomerName = CustomerName;
        _lineListFactory.Update(Lines, entity.Lines);
        await _db.SaveChangesAsync();
    }
}
```

## Open Questions

1. **Shared project vs linked files?**
   - .shproj is older but well-understood
   - Linked files work but more manual
   - Source generator NuGet package?

2. **How to handle Execute commands?**
   - Same pattern: declaration in shared, implementation in server
   - Static partial classes

3. **NuGet distribution?**
   - Client NuGet: shared + client assembly
   - Server NuGet: shared + server assembly + child factories
