# Attributes Reference

Complete reference of all RemoteFactory attributes.

## Factory Discovery Attributes

### [Factory]

Marks a class or interface for factory generation.

**Target:** Class, Interface
**Inherited:** Yes

<!-- snippet: attributes-factory -->
<a id='snippet-attributes-factory'></a>
```cs
/// <summary>
/// [Factory] marks a class for factory generation.
/// Generates IEmployeeFactory interface and EmployeeFactory implementation.
/// </summary>
[Factory]
public partial class SimpleEmployee
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public SimpleEmployee()
    {
        Id = Guid.NewGuid();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L7-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generates:
- `I{TypeName}Factory` interface
- `{TypeName}Factory` implementation class with static `FactoryServiceRegistrar` method for DI registration

### [SuppressFactory]

Prevents factory generation for a class or interface.

**Target:** Class, Interface
**Inherited:** Yes

<!-- snippet: attributes-suppressfactory -->
<a id='snippet-attributes-suppressfactory'></a>
```cs
/// <summary>
/// Base class with factory generation.
/// </summary>
[Factory]
public partial class BaseEmployeeEntity
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = "";

    [Create]
    public BaseEmployeeEntity()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// [SuppressFactory] prevents factory generation for derived class.
/// Use when base class has [Factory] but derived should not.
/// </summary>
[SuppressFactory]
public partial class InternalEmployeeEntity : BaseEmployeeEntity
{
    public string InternalCode { get; set; } = "";

    // No factory generated for this class
    // Must be created via base factory or manually
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L26-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-suppressfactory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use when:
- Base class has `[Factory]` but derived class shouldn't
- Type should not have a factory despite matching generation criteria

## Operation Attributes

### [Create]

Marks constructors or methods that create new instances.

**Target:** Constructor, Method, Class
**Inherited:** No

<!-- snippet: attributes-create -->
<a id='snippet-attributes-create'></a>
```cs
/// <summary>
/// [Create] marks constructors and static methods for instance creation.
/// </summary>
[Factory]
public partial class EmployeeWithCreate
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public decimal InitialSalary { get; private set; }

    private EmployeeWithCreate() { }

    /// <summary>
    /// Parameterless constructor [Create].
    /// </summary>
    [Create]
    public EmployeeWithCreate(string firstName)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
    }

    /// <summary>
    /// Static factory method [Create] for parameterized creation.
    /// </summary>
    [Create]
    public static EmployeeWithCreate Create(
        string employeeNumber,
        string firstName,
        decimal initialSalary)
    {
        return new EmployeeWithCreate
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            FirstName = firstName,
            InitialSalary = initialSalary
        };
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L57-L99' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-create' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Create | Read`

### [Fetch]

Marks methods that load data into existing instances.

**Target:** Method, Constructor
**Inherited:** No

<!-- snippet: attributes-fetch -->
<a id='snippet-attributes-fetch'></a>
```cs
/// <summary>
/// [Fetch] marks methods that load data into existing instances.
/// </summary>
[Factory]
public partial class EmployeeWithFetch : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithFetch() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Fetch] method loads employee by ID.
    /// Returns bool - false means not found (factory returns null).
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L101-L135' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-fetch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Fetch | Read`

### [Insert]

Marks methods that persist new entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-insert -->
<a id='snippet-attributes-insert'></a>
```cs
/// <summary>
/// [Insert] marks methods that persist new entities.
/// </summary>
[Factory]
public partial class EmployeeWithInsert : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithInsert() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Insert] method persists a new entity.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L137-L172' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Insert | Write`

### [Update]

Marks methods that persist changes to existing entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-update -->
<a id='snippet-attributes-update'></a>
```cs
/// <summary>
/// [Update] marks methods that persist changes to existing entities.
/// </summary>
[Factory]
public partial class EmployeeWithUpdate : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithUpdate() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// [Update] method persists changes to an existing entity.
    /// </summary>
    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L174-L219' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Update | Write`

### [Delete]

Marks methods that remove entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-delete -->
<a id='snippet-attributes-delete'></a>
```cs
/// <summary>
/// [Delete] marks methods that remove entities.
/// </summary>
[Factory]
public partial class EmployeeWithDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithDelete() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// [Delete] method removes the entity from persistence.
    /// </summary>
    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L221-L259' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Delete | Write`

### [Execute]

Marks methods for business operations.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-execute -->
<a id='snippet-attributes-execute'></a>
```cs
/// <summary>
/// [Execute] marks methods for business operations (commands).
/// </summary>
[Factory]
public static partial class EmployeePromotion
{
    /// <summary>
    /// [Execute] method performs a business operation.
    /// Underscore prefix is removed in generated delegate name.
    /// </summary>
    [Remote, Execute]
    private static async Task<bool> _PromoteEmployee(
        Guid employeeId,
        string newPosition,
        decimal salaryIncrease,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null) return false;

        var oldPosition = employee.Position;
        employee.Position = newPosition;
        employee.SalaryAmount += salaryIncrease;

        await repository.UpdateAsync(employee, ct);
        await repository.SaveChangesAsync(ct);

        await auditLog.LogAsync("Promotion", employeeId, "Employee",
            $"Promoted from {oldPosition} to {newPosition}", ct);

        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L261-L297' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Execute | Read`

### [Event]

Marks methods for fire-and-forget domain events.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-event -->
<a id='snippet-attributes-event'></a>
```cs
/// <summary>
/// [Event] marks methods for fire-and-forget domain events.
/// CancellationToken is required as the last parameter.
/// </summary>
[Factory]
public partial class EmployeeEvents
{
    /// <summary>
    /// [Event] method runs fire-and-forget.
    /// CancellationToken must be the last parameter.
    /// </summary>
    [Event]
    public async Task NotifyManager(
        Guid employeeId,
        string message,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "manager@company.com",
            "Employee Update",
            $"Employee {employeeId}: {message}",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L299-L325' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Requirements:
- Must have `CancellationToken` as last parameter
- Returns `void` or `Task`

Operation flags: `AuthorizeFactoryOperation.Event`

## Execution Control Attributes

### [Remote]

Marks methods that execute on the server.

**Target:** Method
**Inherited:** Yes

<!-- snippet: attributes-remote -->
<a id='snippet-attributes-remote'></a>
```cs
/// <summary>
/// [Remote] marks methods that execute on the server.
/// </summary>
[Factory]
public partial class EmployeeRemoteExecution : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Local execution - no [Remote].
    /// Runs on client without network call.
    /// </summary>
    [Create]
    public EmployeeRemoteExecution()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// [Remote, Fetch] - executes on server.
    /// Request serialized, sent via HTTP, response deserialized.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L327-L367' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Without `[Remote]`, methods execute locally (no serialization, no HTTP).

### [Service]

Marks parameters for dependency injection.

**Target:** Parameter
**Inherited:** No

<!-- snippet: attributes-service -->
<a id='snippet-attributes-service'></a>
```cs
/// <summary>
/// [Service] marks parameters for dependency injection.
/// </summary>
[Factory]
public partial class EmployeeServiceParams : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeServiceParams() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Mix of value parameters (serialized) and [Service] parameters (injected).
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,                          // Value: serialized to server
        [Service] IEmployeeRepository repository, // Service: resolved from server DI
        [Service] IAuditLogService auditLog,      // Service: resolved from server DI
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;

        await auditLog.LogAsync("Fetch", employeeId, "Employee", "Loaded", ct);
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L369-L405' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-service' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Service parameters:
- Resolved from DI container
- Not serialized
- Must be registered in the appropriate container (server for remote methods)

## Authorization Attributes

### [AuthorizeFactory\<T\>]

Applies custom authorization class to the factory.

**Target:** Class, Interface
**Inherited:** No

<!-- snippet: attributes-authorizefactory-generic -->
<a id='snippet-attributes-authorizefactory-generic'></a>
```cs
/// <summary>
/// [AuthorizeFactory<T>] applies custom authorization to the factory.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class AuthorizedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedEmployee() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L407-L434' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-generic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The type parameter must be an interface with authorization methods decorated with `[AuthorizeFactory]`.

### [AuthorizeFactory]

Marks methods in authorization interfaces or applies to specific factory methods.

**Target:** Method
**Inherited:** No

**On authorization interface:**

<!-- snippet: attributes-authorizefactory-interface -->
<a id='snippet-attributes-authorizefactory-interface'></a>
```cs
/// <summary>
/// [AuthorizeFactory] on interface methods defines authorization checks.
/// </summary>
public interface IDocumentAuthorization
{
    /// <summary>
    /// Check for Create operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    /// <summary>
    /// Check for Read operations (Fetch).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    /// <summary>
    /// Check for Write operations (Insert, Update, Delete).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L436-L460' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**On factory method (additional check):**

<!-- snippet: attributes-authorizefactory-method -->
<a id='snippet-attributes-authorizefactory-method'></a>
```cs
/// <summary>
/// Method-level [AspAuthorize] adds ADDITIONAL authorization on top of class-level auth.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]  // Class-level: runs first
public partial class EmployeeWithMethodAuth2 : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithMethodAuth2() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Delete requires BOTH:
    /// 1. Class-level [AuthorizeFactory<IEmployeeAuthorization>] CanWrite check
    /// 2. Method-level [AspAuthorize] HRManager role check
    /// Both must pass for operation to succeed.
    /// </summary>
    [Remote, Delete]
    [AspAuthorize(Roles = "HRManager")]  // Method-level: runs after class-level
    public async Task Delete(
        [Service] IEmployeeRepository repo,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync("Terminate", Id, "Employee", "Deleted", ct);
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L462-L507' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-method' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Parameters:**
- `operation` (AuthorizeFactoryOperation): Flags indicating which operations require this authorization

### [AspAuthorize]

Applies ASP.NET Core authorization policies to endpoints.

**Target:** Method
**Inherited:** No
**Multiple:** Yes

<!-- snippet: attributes-aspauthorize -->
<a id='snippet-attributes-aspauthorize'></a>
```cs
/// <summary>
/// [AspAuthorize] applies ASP.NET Core authorization policies.
/// </summary>
[Factory]
public partial class PolicyProtectedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PolicyProtectedEmployee() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Policy-based authorization via constructor parameter.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireEmployee")]  // Policy name via constructor
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Role-based authorization via Roles property.
    /// </summary>
    [Remote, Insert]
    [AspAuthorize(Roles = "HR,Manager")]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L509-L558' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-aspauthorize' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Properties:**
- `Policy` (string?): Authorization policy name
- `Roles` (string?): Comma-delimited role list
- `AuthenticationSchemes` (string?): Comma-delimited authentication schemes

Applied to the generated `/api/neatoo` endpoint for the method.

## Assembly-Level Attributes

### [assembly: FactoryMode]

Specifies factory generation mode for the assembly.

**Target:** Assembly
**Inherited:** No

<!-- snippet: attributes-factorymode -->
<a id='snippet-attributes-factorymode'></a>
```cs
// Full mode (default): Generate both local methods and remote stubs
// Use in shared domain assemblies that can run on both client and server
[assembly: FactoryMode(FactoryModeOption.Full)]

// RemoteOnly mode: Generate HTTP stubs only
// Use in client-only assemblies (e.g., Blazor WASM)
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L5-L15' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factorymode' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Parameters:**
- `mode` (FactoryMode): Full or RemoteOnly

Modes:
- **Full**: Generate local and remote code (default)
- **RemoteOnly**: Generate HTTP stubs only (client assemblies)

### [assembly: FactoryHintNameLength]

Limits generated file hint name length for long paths.

**Target:** Assembly
**Inherited:** No

<!-- snippet: attributes-factoryhintnamelength -->
<a id='snippet-attributes-factoryhintnamelength'></a>
```cs
// Increase hint name length to accommodate long namespace/type names
// Use when hitting Windows path length limits (260 characters)
[assembly: FactoryHintNameLength(100)]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/AssemblyAttributes.cs#L3-L7' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Parameters:**
- `maxHintNameLength` (int): Maximum hint name length

Use when hitting Windows path length limits (260 characters).

## Attribute Combinations

### Multiple Operations on One Method

<!-- snippet: attributes-multiple-operations -->
<a id='snippet-attributes-multiple-operations'></a>
```cs
/// <summary>
/// Multiple operation attributes on one method (upsert pattern).
/// </summary>
[Factory]
public partial class SettingSample : IFactorySaveMeta
{
    public string Key { get; private set; } = "";
    public string Value { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SettingSample(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Both [Insert] and [Update] point to same method.
    /// Generated factory has both Insert() and Update() methods.
    /// </summary>
    [Remote, Insert, Update]
    public Task Upsert(CancellationToken ct)
    {
        // Handle both insert and update cases
        IsNew = false;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L560-L590' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-multiple-operations' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory methods:
```csharp
Task Insert(IPerson person);
Task Update(IPerson person);
```

Both route to the same method.

### Remote + Operation

<!-- snippet: attributes-remote-operation -->
<a id='snippet-attributes-remote-operation'></a>
```cs
/// <summary>
/// [Remote] combined with operation attributes.
/// </summary>
[Factory]
public partial class EmployeeRemoteOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeRemoteOps() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Remote, Fetch] - server-side data loading.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// [Remote, Insert] - server-side persistence.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L592-L639' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Executes on server (serialized call).

### Authorization + Operation

<!-- snippet: attributes-authorization-operation -->
<a id='snippet-attributes-authorization-operation'></a>
```cs
/// <summary>
/// Authorization combined with operation attributes.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class EmployeeAuthOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeAuthOps() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Authorization checked before Fetch executes.
    /// IEmployeeAuthorization.CanRead() must return true.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Authorization checked before Insert executes.
    /// IEmployeeAuthorization.CanWrite() must return true.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L641-L691' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorization-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Authorization checked before execution.

## Attribute Inheritance

| Attribute | Inherited |
|-----------|-----------|
| `[Factory]` | Yes |
| `[SuppressFactory]` | Yes |
| `[Create]`, `[Fetch]`, etc. | No |
| `[Remote]` | Yes |
| `[Service]` | No |
| `[AuthorizeFactory<T>]` | No |
| `[AuthorizeFactory]` | No |
| `[AspAuthorize]` | No |

Example:

<!-- snippet: attributes-inheritance -->
<a id='snippet-attributes-inheritance'></a>
```cs
/// <summary>
/// Demonstrates attribute inheritance behavior.
/// </summary>
[Factory]   // Inherited: Yes
public partial class BaseEntityWithFactory
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = "";

    [Create]    // Inherited: No - must be redeclared
    public BaseEntityWithFactory()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]  // [Remote] Inherited: Yes
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        return true;
    }
}

/// <summary>
/// Derived class inherits [Factory] and [Remote] but not [Create].
/// </summary>
public partial class DerivedWithInheritedFactory : BaseEntityWithFactory
{
    public string DerivedProperty { get; set; } = "";

    // Inherits: [Factory] from base
    // Inherits: [Remote] from base.Fetch()
    // Does NOT inherit: [Create] - must redeclare if needed

    [Create]  // Must redeclare for this class to have a Create
    public DerivedWithInheritedFactory() : base()
    {
        DerivedProperty = "Default";
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L693-L737' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`DerivedWithInheritedFactory` inherits:
- `[Factory]` from `BaseEntityWithFactory`
- `[Remote]` from `BaseEntityWithFactory.Fetch`

`DerivedWithInheritedFactory` does NOT inherit:
- `[Create]` from `BaseEntityWithFactory` constructor
- `[AuthorizeFactory<T>]` (if it were applied to `BaseEntityWithFactory`)

## Common Patterns

### CRUD Entity

<!-- snippet: attributes-pattern-crud -->
<a id='snippet-attributes-pattern-crud'></a>
```cs
/// <summary>
/// Complete CRUD entity pattern with all operations.
/// </summary>
[Factory]
public partial class CrudEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Position { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public CrudEmployee()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        Position = entity.Position;
        Salary = entity.SalaryAmount;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = Email, DepartmentId = Guid.Empty, Position = Position,
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = Email, DepartmentId = Guid.Empty, Position = Position,
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L739-L811' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-crud' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Read-Only Entity

<!-- snippet: attributes-pattern-readonly -->
<a id='snippet-attributes-pattern-readonly'></a>
```cs
/// <summary>
/// Read-only entity pattern with only Create and Fetch.
/// </summary>
[Factory]
public partial class EmployeeReport
{
    public Guid Id { get; private set; }
    public string EmployeeName { get; private set; } = "";
    public string Department { get; private set; } = "";
    public decimal TotalSalary { get; private set; }

    [Create]
    public EmployeeReport()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Read-only: Only Fetch operation defined.
    /// No Insert, Update, or Delete.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        EmployeeName = $"{entity.FirstName} {entity.LastName}";
        Department = entity.DepartmentId.ToString();
        TotalSalary = entity.SalaryAmount;
        return true;
    }

    // No Insert, Update, Delete - this is a read-only projection
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L813-L853' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-readonly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Command Handler

<!-- snippet: attributes-pattern-command -->
<a id='snippet-attributes-pattern-command'></a>
```cs
/// <summary>
/// Command handler pattern using static class with [Execute].
/// </summary>
[Factory]
public static partial class TransferEmployeeCmd
{
    /// <summary>
    /// Command pattern: static class with [Execute] method.
    /// Underscore prefix removed in generated delegate.
    /// </summary>
    [Remote, Execute]
    private static async Task<CommandResult> _Execute(
        Guid employeeId,
        Guid newDepartmentId,
        string reason,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository deptRepo,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId, ct);
        if (employee == null)
            return new CommandResult(false, "Employee not found");

        var newDept = await deptRepo.GetByIdAsync(newDepartmentId, ct);
        if (newDept == null)
            return new CommandResult(false, "Department not found");

        var oldDeptId = employee.DepartmentId;
        employee.DepartmentId = newDepartmentId;

        await employeeRepo.UpdateAsync(employee, ct);
        await employeeRepo.SaveChangesAsync(ct);

        await auditLog.LogAsync("Transfer", employeeId, "Employee",
            $"Transferred from {oldDeptId} to {newDepartmentId}. Reason: {reason}", ct);

        return new CommandResult(true, $"Transferred to {newDept.Name}");
    }
}

public record CommandResult(bool Success, string Message);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AttributesSamples.cs#L855-L898' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Event Publisher

<!-- snippet: attributes-pattern-event -->
<a id='snippet-attributes-pattern-event'></a>
```cs
/// <summary>
/// Event handlers for Employee domain events.
/// </summary>
[Factory]
public partial class EmployeeEventHandlers
{
    /// <summary>
    /// Notifies HR when a new employee is created.
    /// CancellationToken is required as the last parameter.
    /// </summary>
    [Event]
    public async Task NotifyHROfNewEmployee(
        Guid employeeId,
        string employeeName,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "hr@company.com",
            $"New Employee: {employeeName}",
            $"Employee {employeeName} (ID: {employeeId}) has been added to the system.",
            ct);
    }

    /// <summary>
    /// Domain event for employee promotion.
    /// </summary>
    [Event]
    public async Task NotifyManagerOfPromotion(
        Guid employeeId,
        string employeeName,
        string oldPosition,
        string newPosition,
        [Service] IEmailService emailService,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        var departmentId = employee?.DepartmentId ?? Guid.Empty;

        await emailService.SendAsync(
            "manager@company.com",
            $"Employee Promotion: {employeeName}",
            $"{employeeName} has been promoted from {oldPosition} to {newPosition}. Department: {departmentId}",
            ct);
    }

    /// <summary>
    /// Audit logging event for employee departure.
    /// </summary>
    [Event]
    public async Task LogEmployeeDeparture(
        Guid employeeId,
        string reason,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync(
            "Departure",
            employeeId,
            "Employee",
            $"Employee departed. Reason: {reason}",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs#L6-L78' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Interfaces Reference](interfaces-reference.md) - All RemoteFactory interfaces
- [Factory Operations](factory-operations.md) - Operation details
- [Authorization](authorization.md) - Authorization attribute usage
