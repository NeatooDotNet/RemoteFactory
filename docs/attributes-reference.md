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
/// Employee aggregate root with [Factory] attribute on class.
/// </summary>
[Factory]
public partial class EmployeeWithFactory
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeWithFactory()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Employee interface - [Factory] on interface generates factory for implementations.
/// </summary>
public interface IEmployeeContract
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
}

/// <summary>
/// Employee implementation with [Factory] on the class.
/// The factory is generated for this concrete class.
/// </summary>
[Factory]
public partial class EmployeeContract : IEmployeeContract
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeContract()
    {
        Id = Guid.NewGuid();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/FactorySamples.cs#L5-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factory' title='Start of snippet'>anchor</a></sup>
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
/// Base Employee class with [Factory] attribute.
/// </summary>
[Factory]
public partial class EmployeeBase
{
    public Guid Id { get; protected set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    [Create]
    public EmployeeBase()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Manager employee with [SuppressFactory] - no factory generated for this derived class.
/// </summary>
[SuppressFactory]
public partial class ManagerEmployee : EmployeeBase
{
    public int DirectReportCount { get; set; }
    public string Department { get; set; } = "";
    public decimal BonusPercentage { get; set; }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/SuppressFactorySamples.cs#L5-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-suppressfactory' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate demonstrating multiple [Create] patterns.
/// </summary>
[Factory]
public partial class EmployeeWithCreate
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public DateTime HireDate { get; private set; }

    /// <summary>
    /// [Create] on parameterless constructor - generates new Id.
    /// </summary>
    [Create]
    public EmployeeWithCreate()
    {
        Id = Guid.NewGuid();
        HireDate = DateTime.UtcNow;
    }

    /// <summary>
    /// [Create] on instance method - initializes with provided values.
    /// </summary>
    [Create]
    public void Initialize(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>
    /// [Create] on static factory method - full control over creation.
    /// </summary>
    [Create]
    public static EmployeeWithCreate CreateEmployee(
        string firstName,
        string lastName,
        Guid departmentId)
    {
        return new EmployeeWithCreate
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            DepartmentId = departmentId,
            HireDate = DateTime.UtcNow
        };
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/CreateSamples.cs#L5-L57' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-create' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with multiple [Fetch] methods.
/// </summary>
[Factory]
public partial class EmployeeWithFetch
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; private set; } = "";

    [Create]
    public EmployeeWithFetch()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches employee by primary key.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            Email = entity.Email;
        }
    }

    /// <summary>
    /// Fetches employee by unique email address.
    /// </summary>
    [Remote, Fetch]
    public async Task FetchByEmail(
        string email,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employees = await repository.GetAllAsync(ct);
        var entity = employees.FirstOrDefault(e =>
            e.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            Email = entity.Email;
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/FetchSamples.cs#L6-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-fetch' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with [Insert] operation.
/// </summary>
[Factory]
public partial class EmployeeWithInsert : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Employee, marking it as new.
    /// </summary>
    [Create]
    public EmployeeWithInsert()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// Persists a new employee to the repository.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/InsertSamples.cs#L6-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-insert' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with [Update] operation.
/// </summary>
[Factory]
public partial class EmployeeWithUpdate : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithUpdate()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// Updates an existing employee in the repository.
    /// Called by Save when IsNew = false.
    /// </summary>
    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            SalaryAmount = Salary
        };

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/UpdateSamples.cs#L6-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-update' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with [Delete] operation.
/// </summary>
[Factory]
public partial class EmployeeWithDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithDelete()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// Deletes the employee from the repository.
    /// Called by Save when IsDeleted = true.
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/DeleteSamples.cs#L6-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-delete' title='Start of snippet'>anchor</a></sup>
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
/// Result of a transfer operation.
/// </summary>
public record TransferResult(Guid EmployeeId, Guid NewDepartmentId, bool Success);

/// <summary>
/// Command for transferring an employee to a new department.
/// [Execute] must be used in a static partial class.
/// </summary>
[Factory]
public static partial class TransferEmployeeCommand
{
    /// <summary>
    /// Local execute - runs on the calling machine.
    /// </summary>
    [Execute]
    private static Task<TransferResult> _TransferEmployee(
        Guid employeeId,
        Guid newDepartmentId,
        [Service] IEmployeeRepository repository)
    {
        return Task.FromResult(new TransferResult(employeeId, newDepartmentId, true));
    }

    /// <summary>
    /// Remote execute - serializes to server and executes there.
    /// </summary>
    [Remote, Execute]
    private static async Task<TransferResult> _TransferEmployeeRemote(
        Guid employeeId,
        Guid newDepartmentId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null)
        {
            return new TransferResult(employeeId, newDepartmentId, false);
        }

        employee.DepartmentId = newDepartmentId;
        await repository.UpdateAsync(employee, ct);
        await repository.SaveChangesAsync(ct);

        return new TransferResult(employeeId, newDepartmentId, true);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/ExecuteSamples.cs#L6-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute' title='Start of snippet'>anchor</a></sup>
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
/// Event handlers for employee onboarding.
/// Events are fire-and-forget - caller does not wait for completion.
/// </summary>
[Factory]
public partial class EmployeeEventHandlers
{
    /// <summary>
    /// Sends welcome email when an employee is hired.
    /// CancellationToken must be the final parameter for [Event] methods.
    /// </summary>
    [Event]
    public async Task SendWelcomeEmail(
        Guid employeeId,
        string email,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            email,
            "Welcome to the Company!",
            $"Your employee ID is {employeeId}. We're excited to have you!",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/EventSamples.cs#L6-L32' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-event' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate demonstrating [Remote] vs local execution.
/// </summary>
[Factory]
public partial class EmployeeWithRemote
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    [Create]
    public EmployeeWithRemote()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// [Remote, Fetch] - Serialized HTTP call to server.
    /// Method executes on the server with access to server-side services.
    /// </summary>
    [Remote, Fetch]
    public async Task FetchFromDatabase(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            Email = entity.Email;
        }
    }

    /// <summary>
    /// [Fetch] without [Remote] - Local execution only.
    /// No serialization, no HTTP call. Uses only local data.
    /// </summary>
    [Fetch]
    public void FetchFromCache(Guid id)
    {
        // In a real scenario, this would read from a local cache
        Id = id;
        FirstName = "Cached";
        LastName = "Employee";
        Email = "cached@example.com";
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/RemoteSamples.cs#L6-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate demonstrating [Service] parameter injection.
/// </summary>
[Factory]
public partial class EmployeeWithService
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FetchedBy { get; private set; } = "";

    [Create]
    public EmployeeWithService()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Demonstrates both value and service parameters.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid employeeId,                        // Value parameter - passed by caller
        [Service] IEmployeeRepository repository, // Service - injected from DI
        [Service] IUserContext userContext,       // Service - injected from DI
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }

        FetchedBy = userContext.Username;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/ServiceSamples.cs#L6-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-service' title='Start of snippet'>anchor</a></sup>
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
/// Authorization interface for Employee operations.
/// </summary>
public interface IEmployeeAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

/// <summary>
/// Implementation of Employee authorization rules.
/// </summary>
public class EmployeeAuthorizationImpl : IEmployeeAuthorization
{
    private readonly IUserContext _userContext;

    public EmployeeAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanRead()
    {
        return _userContext.IsAuthenticated;
    }

    public bool CanWrite()
    {
        return _userContext.IsInRole("HRManager");
    }
}

/// <summary>
/// Employee aggregate with class-level authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class EmployeeWithAuth
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeWithAuth()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/AuthorizationSamples.cs#L6-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-generic' title='Start of snippet'>anchor</a></sup>
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
/// Authorization interface with operation-specific methods.
/// </summary>
public interface IDepartmentAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch(Guid departmentId);

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool CanUpdate(Guid departmentId);

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete(Guid departmentId);
}

/// <summary>
/// Implementation with fine-grained authorization per operation.
/// </summary>
public class DepartmentAuthorizationImpl : IDepartmentAuthorization
{
    private readonly IUserContext _userContext;

    public DepartmentAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreate()
    {
        return _userContext.IsAuthenticated;
    }

    public bool CanFetch(Guid departmentId)
    {
        return _userContext.IsAuthenticated;
    }

    public bool CanUpdate(Guid departmentId)
    {
        return _userContext.IsInRole("DepartmentManager");
    }

    public bool CanDelete(Guid departmentId)
    {
        return _userContext.IsInRole("Administrator");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/AuthorizationSamples.cs#L76-L127' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**On factory method (additional check):**

<!-- snippet: attributes-authorizefactory-method -->
<a id='snippet-attributes-authorizefactory-method'></a>
```cs
/// <summary>
/// Department aggregate with class-level and method-level authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IDepartmentAuthorizationSample>]
public partial class DepartmentWithMethodAuth
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public decimal Budget { get; set; }

    [Create]
    public DepartmentWithMethodAuth()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Uses class-level authorization only.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IDepartmentRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            Name = entity.Name;
        }
    }

    /// <summary>
    /// Method-level [AspAuthorize] adds additional authorization check.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(Roles = "HRManager")]
    public async Task FetchWithSalaryData(
        Guid id,
        [Service] IDepartmentRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            Name = entity.Name;
            Budget = entity.Budget;
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AuthorizeMethodSamples.cs#L15-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorizefactory-method' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with various [AspAuthorize] patterns.
/// </summary>
[Factory]
public partial class EmployeeWithAspAuth
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }

    [Create]
    public EmployeeWithAspAuth()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Policy-based authorization.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public async Task FetchWithPolicy(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }

    /// <summary>
    /// Role-based authorization with multiple roles.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(Roles = "HRManager,Administrator")]
    public async Task FetchWithRoles(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            Salary = entity.SalaryAmount;
        }
    }

    /// <summary>
    /// Scheme-based authorization.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(AuthenticationSchemes = "Bearer")]
    public async Task FetchWithScheme(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }
}

/// <summary>
/// Payroll commands with [AspAuthorize].
/// </summary>
[Factory]
public static partial class PayrollCommands
{
    [Remote, Execute]
    [AspAuthorize(Roles = "Payroll")]
    private static Task<bool> _ProcessPayroll(
        Guid departmentId,
        DateTime payPeriodEnd,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AspAuthorizeSamples.cs#L6-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-aspauthorize' title='Start of snippet'>anchor</a></sup>
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
// Assembly-level factory mode configuration examples:
//
// Server assembly (default mode):
// [assembly: FactoryMode(FactoryMode.Full)]
// - Generates local and remote execution paths
// - Use in server/API projects
//
// Client assembly (remote-only mode):
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]
// - Generates HTTP stubs only, no local execution
// - Use in Blazor WebAssembly and other client projects
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L6-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factorymode' title='Start of snippet'>anchor</a></sup>
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
// Assembly-level hint name length configuration:
//
// [assembly: FactoryHintNameLength(100)]
// - Limits generated file hint name length to 100 characters
// - Use when hitting Windows 260-character path limits
// - Value is maximum characters for the generated file hint name
//
// Default behavior uses full type names which can be long
// for deeply nested namespaces or generic types.
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs#L20-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryhintnamelength' title='Start of snippet'>anchor</a></sup>
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
/// Department aggregate with combined [Insert, Update] on single method.
/// </summary>
[Factory]
public partial class DepartmentWithCombinedOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public decimal Budget { get; set; }
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Create]
    public DepartmentWithCombinedOps()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// [Insert, Update] combined - upsert pattern.
    /// Called by Save for both new and existing entities.
    /// </summary>
    [Remote, Insert, Update]
    public async Task Save(
        [Service] IDepartmentRepository repository,
        CancellationToken ct)
    {
        var entity = new DepartmentEntity
        {
            Id = Id,
            Name = Name,
            Budget = Budget
        };

        if (IsNew)
        {
            await repository.AddAsync(entity, ct);
            IsNew = false;
        }
        else
        {
            await repository.UpdateAsync(entity, ct);
        }

        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IDepartmentRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/CombinationSamples.cs#L6-L64' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-multiple-operations' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with [Remote, Fetch] combination.
/// </summary>
[Factory]
public partial class EmployeeRemoteFetch
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeRemoteFetch()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// [Remote] + operation = server-side execution with serialization.
    /// </summary>
    [Remote, Fetch]
    public async Task FetchFromDatabase(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }
}

/// <summary>
/// Promote command with [Remote, Execute].
/// </summary>
[Factory]
public static partial class PromoteEmployeeCommand
{
    /// <summary>
    /// [Remote, Execute] - server-side command execution.
    /// </summary>
    [Remote, Execute]
    private static async Task<bool> _Promote(
        Guid employeeId,
        string newTitle,
        decimal salaryIncrease,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null) return false;

        employee.Position = newTitle;
        employee.SalaryAmount += salaryIncrease;

        await repository.UpdateAsync(employee, ct);
        await repository.SaveChangesAsync(ct);

        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/CombinationSamples.cs#L66-L131' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-remote-operation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Executes on server (serialized call).

### Authorization + Operation

<!-- snippet: attributes-authorization-operation -->
<a id='snippet-attributes-authorization-operation'></a>
```cs
/// <summary>
/// Authorization interface with combined operation flags.
/// </summary>
public interface IEmployeeOperationAuth
{
    /// <summary>
    /// Combined flags apply same check to multiple operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
    bool CanCreateAndRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

/// <summary>
/// Implementation with operation-level authorization.
/// </summary>
public class EmployeeOperationAuthImpl : IEmployeeOperationAuth
{
    private readonly IUserContext _userContext;

    public EmployeeOperationAuthImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Create and Fetch require only authentication.
    /// </summary>
    public bool CanCreateAndRead()
    {
        return _userContext.IsAuthenticated;
    }

    /// <summary>
    /// Delete requires Administrator role.
    /// </summary>
    public bool CanDelete()
    {
        return _userContext.IsInRole("Administrator");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/AuthorizationOperationSamples.cs#L6-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-authorization-operation' title='Start of snippet'>anchor</a></sup>
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
/// Base Employee class with [Factory].
/// </summary>
[Factory]
public partial class EmployeeBaseInherited
{
    public Guid Id { get; protected set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeBaseInherited()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// [Remote] is inherited by derived classes.
    /// </summary>
    [Remote, Fetch]
    public virtual async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }
}

/// <summary>
/// Manager inherits [Factory] from base.
/// </summary>
public partial class ManagerInherited : EmployeeBaseInherited
{
    public int DirectReportCount { get; set; }

    /// <summary>
    /// Override with additional data loading.
    /// [Remote] is inherited from base.
    /// </summary>
    [Fetch]
    public override async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await base.Fetch(id, repository, ct);
        // Load additional manager-specific data
        DirectReportCount = 5; // Would be loaded from repository
    }
}

/// <summary>
/// Contractor with [SuppressFactory] - no factory generated, use EmployeeBaseFactory.
/// </summary>
[SuppressFactory]
public partial class ContractorInherited : EmployeeBaseInherited
{
    public DateTime ContractEndDate { get; set; }
    public string AgencyName { get; set; } = "";
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/InheritanceSamples.cs#L6-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-inheritance' title='Start of snippet'>anchor</a></sup>
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
/// Complete Employee CRUD entity pattern.
/// </summary>
[Factory]
public partial class EmployeeCrud : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public DateTime HireDate { get; private set; }
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeCrud()
    {
        Id = Guid.NewGuid();
        HireDate = DateTime.UtcNow;
        IsNew = true;
    }

    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            Email = entity.Email;
            DepartmentId = entity.DepartmentId;
            HireDate = entity.HireDate;
            IsNew = false;
        }
    }

    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }

    private EmployeeEntity MapToEntity() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        DepartmentId = DepartmentId,
        HireDate = HireDate
    };
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/PatternSamples.cs#L6-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-crud' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Read-Only Entity

<!-- snippet: attributes-pattern-readonly -->
<a id='snippet-attributes-pattern-readonly'></a>
```cs
/// <summary>
/// Read-only Employee snapshot - Create and Fetch only, no persistence.
/// </summary>
[Factory]
public partial class EmployeeSnapshot
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string DepartmentName { get; private set; } = "";
    public DateTime SnapshotDate { get; private set; }

    /// <summary>
    /// Creates a new snapshot with current timestamp.
    /// </summary>
    [Create]
    public EmployeeSnapshot()
    {
        Id = Guid.NewGuid();
        SnapshotDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Loads snapshot data from repository.
    /// No Insert, Update, or Delete - read-only after creation.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo,
        CancellationToken ct)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId, ct);
        if (employee != null)
        {
            Id = employee.Id;
            FirstName = employee.FirstName;
            LastName = employee.LastName;

            var department = await departmentRepo.GetByIdAsync(employee.DepartmentId, ct);
            DepartmentName = department?.Name ?? "Unknown";
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/PatternSamples.cs#L91-L137' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-readonly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Command Handler

<!-- snippet: attributes-pattern-command -->
<a id='snippet-attributes-pattern-command'></a>
```cs
/// <summary>
/// Result of a termination operation.
/// </summary>
public record TerminationResult(
    Guid EmployeeId,
    bool Success,
    DateTime EffectiveDate,
    string Message);

/// <summary>
/// Command pattern for employee termination.
/// </summary>
[Factory]
public static partial class TerminateEmployeeCommand
{
    /// <summary>
    /// Executes the termination process on the server.
    /// </summary>
    [Remote, Execute]
    private static async Task<TerminationResult> _Execute(
        Guid employeeId,
        DateTime terminationDate,
        string reason,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null)
        {
            return new TerminationResult(
                employeeId,
                false,
                terminationDate,
                "Employee not found");
        }

        await repository.DeleteAsync(employeeId, ct);
        await repository.SaveChangesAsync(ct);

        return new TerminationResult(
            employeeId,
            true,
            terminationDate,
            $"Terminated for: {reason}");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/PatternSamples.cs#L6-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Event Publisher

<!-- snippet: attributes-pattern-event -->
<a id='snippet-attributes-pattern-event'></a>
```cs
/// <summary>
/// Domain event handlers for employee lifecycle events.
/// Event handlers are fire-and-forget - caller does not wait for completion.
/// </summary>
[Factory]
public partial class EmployeeLifecycleEvents
{
    /// <summary>
    /// Sends welcome email when employee is hired.
    /// CancellationToken must be the final parameter for [Event] methods.
    /// </summary>
    [Event]
    public async Task OnEmployeeHired(
        Guid employeeId,
        string email,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            email,
            "Welcome to the Team!",
            $"Your employee ID is {employeeId}. Welcome aboard!",
            ct);
    }

    /// <summary>
    /// Sends congratulations email when employee is promoted.
    /// </summary>
    [Event]
    public async Task OnEmployeePromoted(
        Guid employeeId,
        string newTitle,
        decimal newSalary,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "hr@company.com",
            "Employee Promotion",
            $"Employee {employeeId} promoted to {newTitle} with salary ${newSalary:N2}",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/PatternSamples.cs#L55-L99' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-pattern-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Interfaces Reference](interfaces-reference.md) - All RemoteFactory interfaces
- [Factory Operations](factory-operations.md) - Operation details
- [Authorization](authorization.md) - Authorization attribute usage
