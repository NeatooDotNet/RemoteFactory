# Attributes Reference

Complete reference of all RemoteFactory attributes.

## Factory Discovery Attributes

### [Factory]

Marks a class or interface for factory generation.

**Target:** Class, Interface
**Inherited:** Yes

<!-- snippet: attributes-factory -->
<!--
SNIPPET REQUIREMENTS:
- Show [Factory] attribute on Employee class with Id, FirstName, LastName properties
- Show [Factory] on an IEmployee interface with an implementing Employee class
- Include [Create] constructor in both examples
- Domain layer: Employee aggregate root
- Properties should use private set for Id, public set for Name properties
-->
<!-- endSnippet -->

Generates:
- `I{TypeName}Factory` interface
- `{TypeName}Factory` implementation class with static `FactoryServiceRegistrar` method for DI registration

### [SuppressFactory]

Prevents factory generation for a class or interface.

**Target:** Class, Interface
**Inherited:** Yes

<!-- snippet: attributes-suppressfactory -->
<!--
SNIPPET REQUIREMENTS:
- Show base Employee class with [Factory] attribute
- Show derived ManagerEmployee class with [SuppressFactory] attribute
- Base has Id (protected set) and common employee properties
- Derived adds manager-specific properties (DirectReports count, etc.)
- Comment explaining no factory generated for derived class
- Domain layer: Employee hierarchy
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show Employee class with three [Create] patterns:
  1. [Create] on parameterless constructor (generates new Id)
  2. [Create] on instance method Initialize(string firstName, string lastName)
  3. [Create] on static factory method CreateEmployee(string firstName, string lastName, Guid departmentId)
- Properties: Id, FirstName, LastName, DepartmentId, HireDate
- Domain layer: Employee aggregate showing multiple creation strategies
-->
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Create | Read`

### [Fetch]

Marks methods that load data into existing instances.

**Target:** Method, Constructor
**Inherited:** No

<!-- snippet: attributes-fetch -->
<!--
SNIPPET REQUIREMENTS:
- Show Employee class with multiple [Fetch] methods:
  1. Fetch(Guid employeeId) - loads by primary key
  2. FetchByEmail(string email) - loads by unique email address
- Populate employee properties from fetch parameters
- Include [Create] constructor for completeness
- Domain layer: Employee aggregate with fetch operations
- Return Task (async pattern)
-->
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Fetch | Read`

### [Insert]

Marks methods that persist new entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-insert -->
<!--
SNIPPET REQUIREMENTS:
- Show Employee class implementing IFactorySaveMeta
- Properties: Id, FirstName, LastName, Email, IsNew, IsDeleted
- [Create] constructor that sets IsNew = true
- [Insert] method with [Service] IEmployeeRepository parameter
- Insert sets IsNew = false after persistence
- Domain layer: Employee aggregate with insert operation
-->
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Insert | Write`

### [Update]

Marks methods that persist changes to existing entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-update -->
<!--
SNIPPET REQUIREMENTS:
- Show Employee class implementing IFactorySaveMeta
- Properties: Id, FirstName, LastName, Email, Salary, IsNew, IsDeleted
- [Create] constructor
- [Update] method with [Service] IEmployeeRepository parameter
- Comment: called by Save when IsNew = false
- Domain layer: Employee aggregate with update operation
-->
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Update | Write`

### [Delete]

Marks methods that remove entities.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-delete -->
<!--
SNIPPET REQUIREMENTS:
- Show Employee class implementing IFactorySaveMeta
- Properties: Id, FirstName, LastName, IsNew, IsDeleted
- [Create] constructor
- [Delete] method with [Service] IEmployeeRepository parameter
- Comment: called by Save when IsDeleted = true
- Domain layer: Employee aggregate with delete operation
-->
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Delete | Write`

### [Execute]

Marks methods for business operations.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-execute -->
<!--
SNIPPET REQUIREMENTS:
- Show static partial class TransferEmployeeCommand
- Two methods demonstrating [Execute]:
  1. Local execute: _TransferEmployee(Guid employeeId, Guid newDepartmentId, [Service] IEmployeeRepository)
  2. Remote execute: [Remote, Execute] _TransferEmployeeRemote(...) with same signature
- Return Task<TransferResult> where TransferResult is a simple record
- Comment explaining [Execute] must be in static partial class
- Application layer: Command pattern for employee transfer
-->
<!-- endSnippet -->

Operation flags: `AuthorizeFactoryOperation.Execute | Read`

### [Event]

Marks methods for fire-and-forget domain events.

**Target:** Method
**Inherited:** No

<!-- snippet: attributes-event -->
<!--
SNIPPET REQUIREMENTS:
- Show static partial class EmployeeEventHandlers (events should be static partial)
- [Event] method: SendWelcomeEmail(Guid employeeId, string email, [Service] IEmailService, CancellationToken ct)
- Must show CancellationToken as final parameter (required for events)
- Comment explaining fire-and-forget behavior
- Application layer: Domain event handlers for employee onboarding
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show Employee class with contrast between remote and local fetch:
  1. [Remote, Fetch] FetchFromDatabase(Guid id, [Service] IEmployeeRepository) - server execution
  2. [Fetch] FetchFromCache(Guid id) - local execution only
- Properties: Id, FirstName, LastName, Email
- Comments explaining: [Remote] = serialized HTTP call, no [Remote] = local execution
- Domain layer: Employee aggregate demonstrating execution location control
-->
<!-- endSnippet -->

Without `[Remote]`, methods execute locally (no serialization, no HTTP).

### [Service]

Marks parameters for dependency injection.

**Target:** Parameter
**Inherited:** No

<!-- snippet: attributes-service -->
<!--
SNIPPET REQUIREMENTS:
- Show Employee class with [Fetch] method demonstrating parameter types:
  - Guid employeeId - value parameter passed by caller
  - [Service] IEmployeeRepository repository - injected from DI
  - [Service] ICurrentUserContext userContext - injected from DI
- Include inline comments distinguishing value vs service parameters
- Domain layer: Employee aggregate showing DI injection pattern
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show IEmployeeAuthorization interface with:
  - [AuthorizeFactory(AuthorizeFactoryOperation.Read)] bool CanRead()
  - [AuthorizeFactory(AuthorizeFactoryOperation.Write)] bool CanWrite()
- Show EmployeeAuthorization implementation:
  - Constructor takes ICurrentUserContext
  - CanRead() returns userContext.IsAuthenticated
  - CanWrite() returns userContext.IsInRole("HRManager")
- Show Employee class with [Factory] and [AuthorizeFactory<IEmployeeAuthorization>]
- Include [Remote, Fetch] method to demonstrate protected operation
- Application layer: Authorization interface and implementation
-->
<!-- endSnippet -->

The type parameter must be an interface with authorization methods decorated with `[AuthorizeFactory]`.

### [AuthorizeFactory]

Marks methods in authorization interfaces or applies to specific factory methods.

**Target:** Method
**Inherited:** No

**On authorization interface:**

<!-- snippet: attributes-authorizefactory-interface -->
<!--
SNIPPET REQUIREMENTS:
- Show IDepartmentAuthorization interface with operation-specific methods:
  - [AuthorizeFactory(AuthorizeFactoryOperation.Create)] bool CanCreate()
  - [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)] bool CanFetch(Guid departmentId)
  - [AuthorizeFactory(AuthorizeFactoryOperation.Update)] bool CanUpdate(Guid departmentId)
  - [AuthorizeFactory(AuthorizeFactoryOperation.Delete)] bool CanDelete(Guid departmentId)
- Show DepartmentAuthorization implementation:
  - CanCreate/CanFetch require IsAuthenticated
  - CanUpdate requires "DepartmentManager" role
  - CanDelete requires "Administrator" role
- Application layer: Fine-grained authorization per operation
-->
<!-- endSnippet -->

**On factory method (additional check):**

<!-- snippet: attributes-authorizefactory-method -->
<!--
SNIPPET REQUIREMENTS:
- Show Department class with class-level [AuthorizeFactory<IDepartmentAuthorization>]
- Standard [Remote, Fetch] method (uses class-level auth)
- [Remote, Fetch] FetchWithSalaryData with [AspAuthorize(Roles = "HRManager")]
- Comment: method-level [AspAuthorize] adds additional authorization check
- Domain layer: Department aggregate with mixed authorization strategies
-->
<!-- endSnippet -->

**Parameters:**
- `operation` (AuthorizeFactoryOperation): Flags indicating which operations require this authorization

### [AspAuthorize]

Applies ASP.NET Core authorization policies to endpoints.

**Target:** Method
**Inherited:** No
**Multiple:** Yes

<!-- snippet: attributes-aspauthorize -->
<!--
SNIPPET REQUIREMENTS:
- Show Employee class with three [AspAuthorize] variations on fetch methods:
  1. [AspAuthorize("RequireAuthenticated")] - policy-based
  2. [AspAuthorize(Roles = "HRManager,Administrator")] - role-based
  3. [AspAuthorize(AuthenticationSchemes = "Bearer")] - scheme-based
- Show static partial class PayrollCommands with:
  - [Remote, Execute] [AspAuthorize(Roles = "Payroll")] _ProcessPayroll method
- Domain layer (Employee) and Application layer (PayrollCommands)
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show assembly-level attribute usage (comment form, not actual attribute):
  - [assembly: FactoryMode(FactoryMode.Full)] - server assembly
  - [assembly: FactoryMode(FactoryMode.RemoteOnly)] - client assembly
- Comments explaining:
  - Full: generates local and remote execution paths (default, for server)
  - RemoteOnly: generates HTTP stubs only (for Blazor/client assemblies)
- Infrastructure/configuration context
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show assembly-level attribute usage (comment form):
  - [assembly: FactoryHintNameLength(100)]
- Comments explaining:
  - Limits generated file hint name length
  - Use when hitting Windows 260-character path limits
  - Value is maximum characters for hint name
- Infrastructure/configuration context
-->
<!-- endSnippet -->

**Parameters:**
- `maxHintNameLength` (int): Maximum hint name length

Use when hitting Windows path length limits (260 characters).

## Attribute Combinations

### Multiple Operations on One Method

<!-- snippet: attributes-multiple-operations -->
<!--
SNIPPET REQUIREMENTS:
- Show Department class implementing IFactorySaveMeta
- Properties: Id, Name, Budget, IsNew, IsDeleted
- [Create] constructor
- [Insert, Update] combined on single Save method (upsert pattern)
  - Comment: called by Save for both new and existing entities
- [Delete] as separate method
- Domain layer: Department aggregate with combined persistence operations
-->
<!-- endSnippet -->

Generated factory methods:
```csharp
Task Insert(IPerson person);
Task Update(IPerson person);
```

Both route to the same method.

### Remote + Operation

<!-- snippet: attributes-remote-operation -->
<!--
SNIPPET REQUIREMENTS:
- Show Employee class with [Remote, Fetch] combination:
  - FetchFromDatabase(Guid id, [Service] IEmployeeRepository)
- Show static partial class PromoteEmployeeCommand with [Remote, Execute]:
  - _Promote(Guid employeeId, string newTitle, decimal salaryIncrease, [Service] IEmployeeRepository)
- Comments explaining: [Remote] + operation = server-side execution with serialization
- Domain layer (Employee) and Application layer (PromoteEmployeeCommand)
-->
<!-- endSnippet -->

Executes on server (serialized call).

### Authorization + Operation

<!-- snippet: attributes-authorization-operation -->
<!--
SNIPPET REQUIREMENTS:
- Show IEmployeeOperationAuth interface with combined operation flags:
  - [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)] bool CanCreateAndRead()
  - [AuthorizeFactory(AuthorizeFactoryOperation.Delete)] bool CanDelete()
- Show EmployeeOperationAuth implementation:
  - CanCreateAndRead() requires IsAuthenticated
  - CanDelete() requires "Administrator" role
- Comments explaining: combined flags apply same check to multiple operations
- Application layer: Authorization with bitwise OR operation flags
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show base Employee class with [Factory]:
  - Properties: Id (protected set), FirstName, LastName
  - [Create] constructor
  - [Remote, Fetch] virtual Fetch(Guid id)
- Show Manager : Employee (inherits [Factory]):
  - Adds DirectReportCount property
  - [Remote, Fetch] override Fetch with additional data loading
- Show Contractor : Employee with [SuppressFactory]:
  - Comment: no factory generated, use EmployeeFactory
- Domain layer: Employee hierarchy demonstrating attribute inheritance
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show complete Employee CRUD entity implementing IFactorySaveMeta
- Properties: Id, FirstName, LastName, Email, DepartmentId, HireDate, IsNew, IsDeleted
- [Create] constructor with IsNew = true
- [Remote, Fetch] with [Service] IEmployeeRepository - sets IsNew = false
- [Remote, Insert] with [Service] IEmployeeRepository - sets IsNew = false
- [Remote, Update] with [Service] IEmployeeRepository
- [Remote, Delete] with [Service] IEmployeeRepository
- Domain layer: Complete Employee aggregate with all CRUD operations
-->
<!-- endSnippet -->

### Read-Only Entity

<!-- snippet: attributes-pattern-readonly -->
<!--
SNIPPET REQUIREMENTS:
- Show read-only EmployeeSnapshot class (Create and Fetch only, no persistence)
- Properties: Id, FirstName, LastName, DepartmentName, SnapshotDate (all private set)
- [Create] constructor sets Id and SnapshotDate = DateTime.UtcNow
- [Remote, Fetch] with [Service] IEmployeeRepository - loads data
- Comment: No Insert, Update, or Delete - read-only after creation
- Domain layer: Read-only view/snapshot pattern
-->
<!-- endSnippet -->

### Command Handler

<!-- snippet: attributes-pattern-command -->
<!--
SNIPPET REQUIREMENTS:
- Show static partial class TerminateEmployeeCommand
- Define result record: TerminationResult(Guid EmployeeId, bool Success, DateTime EffectiveDate, string Message)
- [Remote, Execute] _Execute method:
  - Parameters: Guid employeeId, DateTime terminationDate, string reason, [Service] IEmployeeRepository
  - Returns Task<TerminationResult>
- Comment explaining command pattern with [Execute]
- Application layer: Command pattern for employee termination
-->
<!-- endSnippet -->

### Event Publisher

<!-- snippet: attributes-pattern-event -->
<!--
SNIPPET REQUIREMENTS:
- Show static partial class EmployeeLifecycleEvents
- [Event] _OnEmployeeHired:
  - Parameters: Guid employeeId, string email, [Service] IEmailService, CancellationToken ct
  - Sends welcome email
- [Event] _OnEmployeePromoted:
  - Parameters: Guid employeeId, string newTitle, decimal newSalary, [Service] IEmailService, CancellationToken ct
  - Sends congratulations email
- Comment explaining: Event handlers are fire-and-forget, must end with CancellationToken
- Application layer: Domain event handlers pattern
-->
<!-- endSnippet -->

## Next Steps

- [Interfaces Reference](interfaces-reference.md) - All RemoteFactory interfaces
- [Factory Operations](factory-operations.md) - Operation details
- [Authorization](authorization.md) - Authorization attribute usage
