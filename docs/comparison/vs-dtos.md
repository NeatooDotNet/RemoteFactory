---
layout: default
title: "RemoteFactory vs Manual DTOs"
description: "Comparison of RemoteFactory with traditional DTO patterns"
parent: Comparison
nav_order: 3
---

# RemoteFactory vs Manual DTOs

The manual DTO (Data Transfer Object) approach is the traditional way of building 3-tier applications. This document compares it with RemoteFactory to help you understand the trade-offs.

## The Traditional DTO Approach

In a typical 3-tier application, you create:

1. **Domain/Entity models** - Database representations
2. **DTOs** - Data transfer objects for the API
3. **Controllers** - API endpoints
4. **Mapping code** - AutoMapper profiles or manual mapping
5. **Service layer** - Business logic and factory methods

```
Client → DTO → Controller → Service → Entity → Database
```

## Code Comparison

### Manual DTO Approach

Here's what you typically write for a Person entity:

```csharp
// 1. Entity (required anyway)
public class PersonEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

// 2. DTOs (one per operation typically)
public class PersonDto
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

public class CreatePersonDto
{
    [Required]
    public string FirstName { get; set; } = null!;
    [Required]
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
}

public class UpdatePersonDto
{
    [Required]
    public string FirstName { get; set; } = null!;
    [Required]
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
}

// 3. AutoMapper Profile
public class PersonProfile : Profile
{
    public PersonProfile()
    {
        CreateMap<PersonEntity, PersonDto>();
        CreateMap<CreatePersonDto, PersonEntity>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Created, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.Modified, opt => opt.MapFrom(_ => DateTime.UtcNow));
        CreateMap<UpdatePersonDto, PersonEntity>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.Created, opt => opt.Ignore())
            .ForMember(d => d.Modified, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}

// 4. Service Interface
public interface IPersonService
{
    Task<PersonDto?> GetByIdAsync(int id);
    Task<PersonDto> CreateAsync(CreatePersonDto dto);
    Task<PersonDto> UpdateAsync(int id, UpdatePersonDto dto);
    Task DeleteAsync(int id);
}

// 5. Service Implementation
public class PersonService : IPersonService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public PersonService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PersonDto?> GetByIdAsync(int id)
    {
        var entity = await _context.Persons.FindAsync(id);
        return entity == null ? null : _mapper.Map<PersonDto>(entity);
    }

    public async Task<PersonDto> CreateAsync(CreatePersonDto dto)
    {
        var entity = _mapper.Map<PersonEntity>(dto);
        _context.Persons.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<PersonDto>(entity);
    }

    public async Task<PersonDto> UpdateAsync(int id, UpdatePersonDto dto)
    {
        var entity = await _context.Persons.FindAsync(id)
            ?? throw new NotFoundException();
        _mapper.Map(dto, entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<PersonDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Persons.FindAsync(id)
            ?? throw new NotFoundException();
        _context.Persons.Remove(entity);
        await _context.SaveChangesAsync();
    }
}

// 6. Controller
[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly IPersonService _service;

    public PersonsController(IPersonService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanReadPersons")]
    public async Task<ActionResult<PersonDto>> GetById(int id)
    {
        var person = await _service.GetByIdAsync(id);
        return person == null ? NotFound() : Ok(person);
    }

    [HttpPost]
    [Authorize(Policy = "CanCreatePersons")]
    public async Task<ActionResult<PersonDto>> Create(CreatePersonDto dto)
    {
        var person = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = person.Id }, person);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanUpdatePersons")]
    public async Task<ActionResult<PersonDto>> Update(int id, UpdatePersonDto dto)
    {
        var person = await _service.UpdateAsync(id, dto);
        return Ok(person);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeletePersons")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

// 7. Client HTTP calls
public class PersonApiClient
{
    private readonly HttpClient _http;

    public async Task<PersonDto?> GetByIdAsync(int id)
    {
        var response = await _http.GetAsync($"api/persons/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PersonDto>();
    }

    public async Task<PersonDto> CreateAsync(CreatePersonDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/persons", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PersonDto>()!;
    }

    // ... more methods
}
```

**Total: ~200+ lines across 7 files**

### RemoteFactory Approach

```csharp
// 1. Entity (required anyway)
public class PersonEntity
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

// 2. Domain Model (this is your "DTO" that works everywhere)
[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public partial class PersonModel : IPersonModel, IFactorySaveMeta
{
    public int Id { get; private set; }

    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    public string? Email { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);

    [Create]
    public PersonModel()
    {
        Created = DateTime.UtcNow;
        Modified = DateTime.UtcNow;
    }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(id);
        if (entity == null) return false;
        MapFrom(entity);
        IsNew = false;
        return true;
    }

    [Remote]
    [Insert]
    [Update]
    public async Task Save([Service] IPersonContext context)
    {
        PersonEntity entity;
        if (IsNew)
        {
            entity = new PersonEntity();
            context.Persons.Add(entity);
        }
        else
        {
            entity = await context.Persons.FindAsync(Id)!;
        }
        Modified = DateTime.UtcNow;
        MapTo(entity);
        await context.SaveChangesAsync();
        Id = entity.Id;
        IsNew = false;
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(Id);
        if (entity != null)
        {
            context.Persons.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}

// 3. Authorization (optional but recommended)
public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();
    // ...
}

// Usage - inject IPersonModelFactory (GENERATED)
var person = await factory.Fetch(123);
person.FirstName = "Updated";
await factory.Save(person);
```

**Total: ~70 lines in 2-3 files (plus generated code)**

## File Count Comparison

| Files Needed | Manual DTOs | RemoteFactory |
|--------------|-------------|---------------|
| Entity | 1 | 1 |
| DTOs | 3+ | 0 |
| Mapper Profile | 1 | 0 (generated) |
| Service Interface | 1 | 0 (generated) |
| Service Implementation | 1 | 0 (generated) |
| Controller | 1 | 0 |
| Client API | 1 | 0 (generated) |
| Domain Model | 0 | 1 |
| Authorization | 0-1 | 0-1 |
| **Total** | **8-10** | **2-3** |

## Detailed Comparison

### Maintenance Burden

**Manual DTOs:**
- Add property to entity? Update 3+ DTOs, mapper, possibly service
- Rename property? Find and update everywhere
- Add validation? Multiple places
- DTOs can drift out of sync with entities

**RemoteFactory:**
- Add property to domain model? Done
- Rename property? One place
- Add validation? DataAnnotations on domain model
- Single source of truth

### Type Safety

**Manual DTOs:**
- Runtime errors from mapping mismatches
- String-based API routes
- Possible null reference issues in mapping

**RemoteFactory:**
- Compile-time generated code
- Type-safe factory methods
- Compiler catches property name changes

### Flexibility

**Manual DTOs:**
- Full control over API shape
- Easy API versioning
- Different shapes for different clients
- Standard REST patterns

**RemoteFactory:**
- Single model serves all purposes
- Different approach to versioning (model versions)
- Same model on client and server
- Single endpoint pattern

### Performance

**Manual DTOs:**
- AutoMapper has runtime overhead
- HTTP call per operation type
- Possible over-fetching/under-fetching

**RemoteFactory:**
- Compile-time generated mappers
- Single endpoint, minimal HTTP overhead
- Exact data transfer

## When Manual DTOs Are Better

### 1. Public APIs

When you're building an API for external consumers:

```csharp
// You want stable, versioned DTOs
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/persons")]
public class PersonsV1Controller : ControllerBase
{
    // V1 DTOs won't change
}

[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/persons")]
public class PersonsV2Controller : ControllerBase
{
    // V2 can have different shape
}
```

### 2. Different Data Shapes

When different clients need different data:

```csharp
// Mobile needs minimal data
public class PersonMobileDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
}

// Admin needs everything
public class PersonAdminDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public AuditInfo Audit { get; set; }
    public List<ActivityLog> Activities { get; set; }
}
```

### 3. Third-Party Integration

When integrating with external systems that expect specific formats:

```csharp
// External system's expected format
public class ExternalPersonDto
{
    [JsonPropertyName("person_id")]
    public string PersonId { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; }
}
```

## When RemoteFactory Is Better

### 1. Internal Applications

Blazor, WPF, or other .NET clients where you control both ends:

```csharp
// Same model everywhere
[Factory]
public partial class PersonModel : IPersonModel
{
    // Used in Blazor UI
    // Used in server processing
    // No translation needed
}
```

### 2. Rapid Development

When speed matters more than API design:

```csharp
// Add a feature in minutes, not hours
[Factory]
public partial class NewFeatureModel
{
    [Create]
    public NewFeatureModel() { }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch([Service] IContext ctx) { ... }
}
// Factory is generated, DI is registered, ready to use
```

### 3. Single Client Type

When you have one client platform:

```csharp
// Blazor WASM only? RemoteFactory is perfect
// No need for REST API design
// Just domain operations
```

## Migration Path

### From DTOs to RemoteFactory

1. **Keep existing DTOs for external APIs**
2. **Add RemoteFactory for new internal features**
3. **Gradually migrate internal endpoints**

```csharp
// Existing public API stays
[Route("api/persons")]
public class PersonsController : ControllerBase { ... }

// New internal features use RemoteFactory
[Factory]
public partial class PersonModel
{
    [Remote]
    [Execute]
    public static async Task<List<PersonSummary>> GetDashboardData([Service] IContext ctx)
    {
        // New feature, no DTO needed
    }
}
```

## Summary

| Aspect | Manual DTOs | RemoteFactory |
|--------|-------------|---------------|
| Code volume | High | Low |
| Files to maintain | Many | Few |
| Type safety | Runtime | Compile-time |
| API versioning | Easy | Different pattern |
| External APIs | Designed for this | Less suitable |
| Internal apps | More work | Ideal |
| Learning curve | Familiar patterns | New concepts |
| Flexibility | Maximum | Moderate |
| Development speed | Slower | Faster |
