# Documentation Samples Implementation Plan

**Related Todo:** `docs/todos/documentation-samples-implementation.md`
**Created:** 2025-01-25
**Updated:** 2025-01-25

## Executive Summary

Build a production-quality Employee Management reference application at `src/docs/reference-app/` that demonstrates all RemoteFactory capabilities. This is a REAL application with proper layered architecture, NOT a collection of isolated sample files. Documentation snippets will be identified and extracted AFTER the application is complete.

## Architecture Overview

### Solution Structure

```
src/docs/reference-app/
+-- EmployeeManagement.sln
|
+-- EmployeeManagement.Domain/           # Core domain (no dependencies)
|   +-- EmployeeManagement.Domain.csproj
|   +-- Aggregates/
|   +-- ValueObjects/
|   +-- Events/
|   +-- Interfaces/
|
+-- EmployeeManagement.Application/      # Application services
|   +-- EmployeeManagement.Application.csproj
|   +-- Services/
|   +-- Commands/
|   +-- Queries/
|
+-- EmployeeManagement.Infrastructure/   # Data access, external services
|   +-- EmployeeManagement.Infrastructure.csproj
|   +-- Repositories/
|   +-- Services/
|
+-- EmployeeManagement.Server.WebApi/    # ASP.NET Core Web API
|   +-- EmployeeManagement.Server.WebApi.csproj
|   +-- Program.cs
|   +-- Configuration/
|
+-- EmployeeManagement.Client.Blazor/    # Blazor WebAssembly
|   +-- EmployeeManagement.Client.Blazor.csproj
|   +-- Program.cs
|   +-- Pages/
|   +-- Components/
|
+-- EmployeeManagement.Tests/            # Integration tests
    +-- EmployeeManagement.Tests.csproj
    +-- EmployeeTests.cs
    +-- DepartmentTests.cs
    +-- TestInfrastructure/
```

### Project Dependencies

```
Domain (no dependencies)
    ^
    |
Application (depends on Domain)
    ^
    |
Infrastructure (depends on Domain, Application)
    ^
    |
+-- Server.WebApi (depends on all)
|
+-- Client.Blazor (depends on Domain, Application)
|
+-- Tests (depends on all)
```

## Domain Model

### Employee Variants Strategy

Different documentation snippets require Employee classes with different properties and features. To avoid partial class conflicts, use **multiple Employee variants** in separate namespaces:

| Variant | Namespace | Purpose |
|---------|-----------|---------|
| `Employee` | `Domain.Aggregates` | Main aggregate with full features |
| `EmployeeBasic` | `Domain.Samples.GettingStarted` | Simple example for getting-started docs |
| `EmployeeWithAuth` | `Domain.Samples.Authorization` | Authorization examples |
| `EmployeeWithEvents` | `Domain.Samples.Events` | Event handling examples |
| `EmployeeWithSave` | `Domain.Samples.Save` | Save operation examples |

Each variant is a complete, compilable class tailored to its documentation context.

### Employee Aggregate (Main)

```csharp
// Domain/Aggregates/Employee.cs
namespace EmployeeManagement.Domain.Aggregates;

[Factory]
public partial class Employee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EmailAddress Email { get; set; } = null!;
    public PhoneNumber? Phone { get; set; }
    public Guid DepartmentId { get; set; }
    public string Position { get; set; } = "";
    public Money Salary { get; set; } = null!;
    public DateTime HireDate { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public Employee()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        MapFromEntity(entity);
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }

    private void MapFromEntity(EmployeeEntity entity)
    {
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = new EmailAddress(entity.Email);
        Phone = entity.Phone != null ? ParsePhone(entity.Phone) : null;
        DepartmentId = entity.DepartmentId;
        Position = entity.Position;
        Salary = new Money(entity.SalaryAmount, entity.SalaryCurrency);
        HireDate = entity.HireDate;
    }

    private EmployeeEntity MapToEntity() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email.Value,
        Phone = Phone?.ToString(),
        DepartmentId = DepartmentId,
        Position = Position,
        SalaryAmount = Salary.Amount,
        SalaryCurrency = Salary.Currency,
        HireDate = HireDate
    };

    private static PhoneNumber ParsePhone(string phone)
    {
        // Simple parsing logic
        var parts = phone.Split(' ', 2);
        return new PhoneNumber(parts[0], parts.Length > 1 ? parts[1] : "");
    }
}
```

### Department Aggregate

```csharp
// Domain/Aggregates/Department.cs
[Factory]
public partial class Department : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public Guid? ManagerId { get; set; }
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    // Factory methods...
}
```

### Value Objects

Value objects use the `[Factory]` attribute to get automatic serialization support. This is the recommended approach - custom serialization via `IOrdinalSerializable` should be avoided.

```csharp
// Domain/ValueObjects/EmailAddress.cs
[Factory]
public partial class EmailAddress
{
    public string Value { get; }

    [Create]
    public EmailAddress(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException("Invalid email format");
        Value = value;
    }

    private static bool IsValid(string value) =>
        !string.IsNullOrEmpty(value) && value.Contains('@');
}

// Domain/ValueObjects/PhoneNumber.cs
[Factory]
public partial class PhoneNumber
{
    public string CountryCode { get; }
    public string Number { get; }

    [Create]
    public PhoneNumber(string countryCode, string number)
    {
        CountryCode = countryCode;
        Number = number;
    }
}

// Domain/ValueObjects/Money.cs
[Factory]
public partial class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    [Create]
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
}
```

### Domain Event Handlers

Event handlers are `[Factory]` classes containing `[Event]` methods. The class name should describe the handler, not the event data.

```csharp
// Domain/Events/EmployeeEventHandlers.cs
[Factory]
public partial class EmployeeEventHandlers
{
    [Event]
    public async Task NotifyHROfNewEmployee(
        Guid employeeId,
        string employeeName,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "hr@example.com",
            $"New Employee: {employeeName}",
            $"Employee {employeeName} (ID: {employeeId}) has been added.",
            ct);
    }

    [Event]
    public async Task NotifyManagerOfPromotion(
        Guid employeeId,
        string newPosition,
        [Service] IEmailService emailService,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        // Event handler implementation
    }
}
```

The source generator creates delegates named after the method (e.g., `NotifyHROfNewEmployeeEvent`).

### Execute Operations (Commands)

Execute operations use `[Factory]` classes with `[Execute]` methods (not static classes with `[SuppressFactory]`).

```csharp
// Domain/Commands/PromoteEmployeeCommand.cs
[Factory]
public partial class PromoteEmployeeCommand
{
    public Guid EmployeeId { get; set; }
    public string NewPosition { get; set; } = "";
    public decimal SalaryIncrease { get; set; }

    [Create]
    public PromoteEmployeeCommand() { }

    [Remote, Execute]
    public async Task<bool> Execute(
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(EmployeeId, ct);
        if (entity == null) return false;

        entity.Position = NewPosition;
        entity.SalaryAmount += SalaryIncrease;

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        await auditLog.LogAsync(
            "Promote",
            EmployeeId,
            "Employee",
            $"Promoted to {NewPosition} with ${SalaryIncrease} raise",
            ct);

        return true;
    }
}
```

## Critical Implementation Rules

### Rule 1: NO Commented Code

All code must be REAL, executable code. No placeholder comments.

**CORRECT:**
```csharp
public async Task SaveEmployee(IEmployeeFactory factory)
{
    var employee = factory.Create();
    employee.FirstName = "Jane";
    employee.LastName = "Smith";
    employee.Email = new EmailAddress("jane.smith@example.com");

    var saved = await factory.Save(employee);
}
```

### Rule 2: Production Quality Code

Code should follow best practices and clean architecture principles:

```csharp
public partial class EmployeeListPage
{
    [Inject] private IEmployeeFactory Factory { get; set; }

    private bool CanAddEmployee => Factory.CanCreate().HasAccess;

    private async Task LoadEmployees()
    {
        if (Factory.CanFetch().HasAccess)
        {
            employees = await Factory.FetchAll();
        }
    }
}
```

### Rule 3: Two-Container Testing Pattern

Tests use the two-container pattern to validate client-server serialization round-trips, following the established pattern in `RemoteFactory.IntegrationTests`.

**CORRECT:**
```csharp
public class EmployeeTests
{
    public static IEnumerable<object[]> ContainerScopes => ClientServerContainers.Scopes();

    [Theory]
    [MemberData(nameof(ContainerScopes))]
    public async Task Employee_Create_And_Save(IServiceScope client, IServiceScope server)
    {
        var factory = client.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "User";

        var saved = await factory.Save(employee);

        Assert.False(saved.IsNew);
    }
}
```

The `ClientServerContainers` class sets up:
- **Client container**: Configured with `NeatooFactory.Remote`, serializes requests
- **Server container**: Configured with `NeatooFactory.Server`, executes operations
- **Local container**: Configured with `NeatooFactory.Logical`, for comparison testing

## Implementation Phases

### Phase 1: Foundation

**Deliverables:**
1. Solution and project structure
2. Domain layer with core aggregates
3. Shared interfaces and value objects

### Phase 2: Domain Operations

**Deliverables:**
1. Complete Employee and Department aggregates
2. All CRUD operations (Create, Fetch, Insert, Update, Delete)
3. Lifecycle hooks (IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled)

### Phase 3: Authorization

**Deliverables:**
1. Authorization interfaces (IAuthorizeFactory)
2. Authorization implementations
3. Authorized domain models

### Phase 4: Events

**Deliverables:**
1. Domain events (EmployeeCreated, EmployeePromoted)
2. Event handlers
3. EventTracker usage

### Phase 5: Save Operation

**Deliverables:**
1. Complete save workflow with IFactorySaveMeta
2. Validation patterns
3. Optimistic concurrency

### Phase 6: Server Configuration

**Deliverables:**
1. Server.WebApi project
2. ASP.NET Core integration
3. Serialization configuration

### Phase 7: Client Configuration

**Deliverables:**
1. Client.Blazor project
2. Remote mode setup
3. Client-server workflow

### Phase 8: Service Injection

**Deliverables:**
1. Infrastructure services
2. Scoped services
3. Service registration

### Phase 9: Testing

**Deliverables:**
1. Two-container test infrastructure (following `RemoteFactory.IntegrationTests` pattern)
2. `ClientServerContainers` class for client/server/local container setup
3. Integration tests using `[Theory]` with `[MemberData]` for container scopes
4. Tests validate serialization round-trips across client-server boundary

**Test Infrastructure:**
```csharp
// Tests/TestInfrastructure/ClientServerContainers.cs
public static class ClientServerContainers
{
    public static IEnumerable<object[]> Scopes()
    {
        // Returns client, server, and local scope combinations
        // for testing different factory modes
    }

    private static IServiceProvider BuildClientProvider() { ... }
    private static IServiceProvider BuildServerProvider() { ... }
    private static IServiceProvider BuildLocalProvider() { ... }
}
```

### Phase 10: Snippet Identification

**Deliverables:**
1. Review working application
2. Identify code suitable for documentation
3. Add `#region snippet-name` markers
4. Configure MarkdownSnippets tool

## Technical Specifications

### Project Files

#### EmployeeManagement.Domain.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\RemoteFactory\RemoteFactory.csproj" />
  </ItemGroup>
</Project>
```

#### EmployeeManagement.Server.WebApi.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\RemoteFactory.AspNetCore\RemoteFactory.AspNetCore.csproj" />
    <ProjectReference Include="..\EmployeeManagement.Domain\EmployeeManagement.Domain.csproj" />
    <ProjectReference Include="..\EmployeeManagement.Application\EmployeeManagement.Application.csproj" />
    <ProjectReference Include="..\EmployeeManagement.Infrastructure\EmployeeManagement.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

#### EmployeeManagement.Client.Blazor.csproj

Blazor WebAssembly targets a single framework (no multi-targeting) due to compatibility constraints.

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\RemoteFactory\RemoteFactory.csproj" />
    <ProjectReference Include="..\EmployeeManagement.Domain\EmployeeManagement.Domain.csproj" />
    <ProjectReference Include="..\EmployeeManagement.Application\EmployeeManagement.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

#### EmployeeManagement.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\EmployeeManagement.Domain\EmployeeManagement.Domain.csproj" />
    <ProjectReference Include="..\EmployeeManagement.Application\EmployeeManagement.Application.csproj" />
    <ProjectReference Include="..\EmployeeManagement.Infrastructure\EmployeeManagement.Infrastructure.csproj" />
    <ProjectReference Include="..\EmployeeManagement.Server.WebApi\EmployeeManagement.Server.WebApi.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

## Success Criteria

1. **Compilation:** `dotnet build EmployeeManagement.sln` succeeds across all target frameworks
2. **Tests:** All integration tests pass
3. **Client-Server:** Blazor client can communicate with WebApi server
4. **Features:** All RemoteFactory features are demonstrated:
   - Factory operations (Create, Fetch, Insert, Update, Delete)
   - Save operation with state tracking
   - Events and EventTracker
   - Authorization
   - Serialization
   - Service injection

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Build failures | Each project builds independently |
| Generator conflicts | Use unique namespaces per domain class |
| Client-server communication | Test locally with Kestrel |
| Framework compatibility | Target net8.0, net9.0, net10.0 |

## Service Interfaces

Repositories work with persistence entities, not domain objects. The domain aggregates handle mapping in their factory methods.

```csharp
// Domain/Interfaces/IEmployeeRepository.cs
public interface IEmployeeRepository
{
    Task<EmployeeEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<EmployeeEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(EmployeeEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeEntity entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

// Infrastructure/Entities/EmployeeEntity.cs
public class EmployeeEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public Guid DepartmentId { get; set; }
    public string Position { get; set; } = "";
    public decimal SalaryAmount { get; set; }
    public string SalaryCurrency { get; set; } = "USD";
    public DateTime HireDate { get; set; }
}

// Infrastructure/Repositories/InMemoryEmployeeRepository.cs
public class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly Dictionary<Guid, EmployeeEntity> _employees = new();

    public Task<EmployeeEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_employees.GetValueOrDefault(id));

    public Task<List<EmployeeEntity>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(_employees.Values.ToList());

    public Task AddAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        _employees[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        _employees[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _employees.Remove(id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask; // No-op for in-memory
}

// Domain/Interfaces/IUserContext.cs
public interface IUserContext
{
    Guid UserId { get; }
    string Username { get; }
    string[] Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

// Domain/Interfaces/IEmailService.cs
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}

// Domain/Interfaces/IAuditLogService.cs
public interface IAuditLogService
{
    Task LogAsync(string action, Guid entityId, string entityType, string details, CancellationToken ct);
}
```

## Post-Implementation: Snippet Extraction

After the application is built and working:

1. **Review the codebase** - Identify code sections that best demonstrate RemoteFactory features
2. **Add region markers** - Mark code sections with `#region snippet-name` / `#endregion`
3. **Configure MarkdownSnippets** - Set up `.mdsnippets` configuration
4. **Update documentation** - Add `<!-- snippet: name -->` placeholders to markdown files
5. **Run MarkdownSnippets** - Extract and embed snippets into documentation
