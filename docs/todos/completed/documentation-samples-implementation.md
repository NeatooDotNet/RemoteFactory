# Documentation Samples Implementation

**Status:** Complete
**Created:** 2025-01-25
**Updated:** 2026-02-01
**Priority:** High

## Overview

Create a production-quality Employee Management reference application that demonstrates all RemoteFactory capabilities. This is a REAL application with proper layered architecture, NOT a collection of isolated sample files. Documentation snippets will be identified and marked AFTER the application is built.

## Requirements

### Critical Requirements

1. **Working application** - All code must compile and run correctly
2. **Production quality** - Code follows best practices and clean architecture
3. **Employee Management domain** - Standardized domain model demonstrating RemoteFactory features
4. **Blazor WebAssembly client** - Full client-server demonstration
5. **NO two-container testing pattern** - Tests exercise factories as they would be used in production

### Reference Application Architecture

```
src/docs/reference-app/
+-- EmployeeManagement.sln
+-- Domain/                              # Core domain model (no dependencies)
|   +-- Aggregates/
|   |   +-- Employee.cs                  # Employee aggregate root
|   |   +-- Department.cs                # Department aggregate root
|   +-- ValueObjects/
|   |   +-- EmailAddress.cs
|   |   +-- PhoneNumber.cs
|   |   +-- Money.cs
|   +-- Events/
|   |   +-- EmployeeCreatedEvent.cs
|   |   +-- EmployeePromotedEvent.cs
|   +-- Interfaces/
|       +-- IEmployeeRepository.cs
|       +-- IDepartmentRepository.cs
+-- Application/                         # Application services, commands, DTOs
|   +-- Services/
|   |   +-- EmployeeService.cs
|   |   +-- DepartmentService.cs
|   +-- Commands/
|   |   +-- CreateEmployeeCommand.cs
|   |   +-- PromoteEmployeeCommand.cs
|   +-- Queries/
|       +-- GetEmployeeQuery.cs
+-- Infrastructure/                      # Data access, external services
|   +-- Repositories/
|   |   +-- EmployeeRepository.cs
|   |   +-- DepartmentRepository.cs
|   +-- Services/
|       +-- EmailService.cs
|       +-- AuditLogService.cs
+-- Server.WebApi/                       # ASP.NET Core Web API
|   +-- Program.cs
|   +-- Configuration/
+-- Client.Blazor/                       # Blazor WebAssembly client
|   +-- Program.cs
|   +-- Pages/
|   +-- Components/
+-- Tests/                               # Integration tests
    +-- EmployeeTests.cs
    +-- DepartmentTests.cs
```

## Features to Demonstrate

### Domain Layer

- Employee aggregate with full CRUD operations
- Department aggregate with parent-child relationships
- Value objects (EmailAddress, PhoneNumber, Money)
- Domain events (EmployeeCreated, EmployeePromoted)
- `IFactorySaveMeta` for save operation support
- Lifecycle interfaces (`IFactoryOnStart`, `IFactoryOnComplete`, `IFactoryOnCancelled`)
- `IOrdinalSerializable` for custom serialization

### Application Layer

- Application services coordinating domain operations
- Command objects for write operations
- Query objects for read operations
- EventTracker usage for event handling

### Infrastructure Layer

- Repository implementations
- External service implementations (Email, AuditLog)
- Custom serialization converters

### Server Layer (WebApi)

- ASP.NET Core integration with `AddNeatooRemoteFactory`
- Factory mode configuration (Full, Server)
- Serialization configuration
- Authorization policy configuration
- CORS configuration
- Logging configuration

### Client Layer (Blazor)

- Blazor WebAssembly setup with RemoteOnly mode
- Factory injection and usage in components
- Authorization checks in UI
- Client-server workflow demonstration

### Testing

- Integration tests using standard DI patterns
- Logical mode for single-tier testing
- Event testing with EventTracker
- Authorization testing

## Acceptance Criteria

- [x] Reference application compiles successfully
- [x] Blazor client can connect to WebApi server
- [x] All factory operations work (Create, Fetch, Insert, Update, Delete)
- [x] Save operation works with state tracking
- [x] Events fire and can be handled
- [x] Authorization checks work
- [x] Serialization works across client-server boundary
- [x] Integration tests pass using standard DI (no two-container)

## Dependencies

- RemoteFactory core library
- RemoteFactory.AspNetCore package
- Microsoft.AspNetCore.Components.WebAssembly
- Microsoft.Extensions.DependencyInjection
- xUnit (for integration tests)

## Next Steps

After the reference application is built and working:
1. Identify code sections suitable for documentation snippets
2. Add `#region snippet-name` markers to those sections
3. Configure MarkdownSnippets to extract snippets
4. Update documentation with snippet placeholders

## Related Documents

- Plan: `docs/plans/documentation-samples-plan.md`
