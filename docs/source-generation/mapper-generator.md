---
layout: default
title: "Mapper Generator"
description: "Understanding how RemoteFactory generates MapTo/MapFrom methods"
parent: Source Generation
nav_order: 3
---

# Mapper Generator

The Mapper Generator creates property mapping code for partial `MapTo` and `MapFrom` methods. This eliminates manual mapping between domain models and entities while maintaining compile-time safety.

## Enabling Mapper Generation

### 1. Mark Your Class as Partial

```csharp
[Factory]
public partial class PersonModel  // Must be partial
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}
```

### 2. Declare Partial Mapper Methods

```csharp
[Factory]
public partial class PersonModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }

    // Declare partial methods - generator fills in the implementation
    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);
}
```

### 3. The Generator Creates Implementations

```csharp
// Generated file: PersonModelMapper.g.cs
public partial class PersonModel
{
    public partial void MapFrom(PersonEntity entity)
    {
        this.FirstName = entity.FirstName;
        this.LastName = entity.LastName;
        this.Email = entity.Email;
    }

    public partial void MapTo(PersonEntity entity)
    {
        entity.FirstName = this.FirstName;
        entity.LastName = this.LastName;
        entity.Email = this.Email;
    }
}
```

## Property Matching Rules

### Same Name Matching

Properties are matched by name (case-sensitive):

```csharp
// Domain Model
public class PersonModel
{
    public string? FirstName { get; set; }  // Matches
    public string? LastName { get; set; }   // Matches
    public string? Email { get; set; }      // Matches
}

// Entity
public class PersonEntity
{
    public string FirstName { get; set; }   // Matches
    public string LastName { get; set; }    // Matches
    public string? Email { get; set; }      // Matches
    public int Id { get; set; }             // No match in model - ignored
}
```

### Properties That Are Ignored

The generator ignores properties that:
- Don't have a matching name in the target type
- Are marked with `[MapperIgnore]`
- Are read-only in the target (for `MapTo`)

## The [MapperIgnore] Attribute

Exclude properties from mapping:

```csharp
[Factory]
public partial class PersonModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [MapperIgnore]  // Not included in mapping
    public string FullName => $"{FirstName} {LastName}";

    [MapperIgnore]  // Not included in mapping
    public bool IsDirty { get; set; }

    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);
}
```

**Generated (FullName and IsDirty excluded):**

```csharp
public partial void MapFrom(PersonEntity entity)
{
    this.FirstName = entity.FirstName;
    this.LastName = entity.LastName;
    // FullName and IsDirty are not mapped
}
```

## Nullable Type Handling

### Nullable to Non-Nullable

When mapping from nullable to non-nullable, the generator throws if null:

```csharp
// Model has nullable
public class PersonModel
{
    public string? FirstName { get; set; }  // Nullable
}

// Entity requires non-nullable
public class PersonEntity
{
    public string FirstName { get; set; }   // Non-nullable
}

// Generated MapTo throws if null
public partial void MapTo(PersonEntity entity)
{
    entity.FirstName = this.FirstName
        ?? throw new NullReferenceException("PersonModel.FirstName");
}
```

### Non-Nullable to Nullable

This works directly:

```csharp
// Entity has non-nullable
public string FirstName { get; set; }

// Model has nullable
public string? FirstName { get; set; }

// Generated MapFrom - direct assignment
public partial void MapFrom(PersonEntity entity)
{
    this.FirstName = entity.FirstName;  // string -> string?
}
```

## Type Compatibility

### Compatible Types

The generator handles compatible types:

```csharp
public class PersonModel
{
    public int Age { get; set; }
    public DateTime Created { get; set; }
    public decimal Balance { get; set; }
}

public class PersonEntity
{
    public int Age { get; set; }           // Same type
    public DateTime Created { get; set; }  // Same type
    public decimal Balance { get; set; }   // Same type
}
```

### Enum Mapping

Enums with the same values map directly:

```csharp
// Model enum
public enum Status { Active, Inactive, Pending }

public class OrderModel
{
    public Status Status { get; set; }
}

// Entity enum (same values)
public enum OrderStatus { Active, Inactive, Pending }

public class OrderEntity
{
    public OrderStatus Status { get; set; }
}

// Generated - cast between enums
public partial void MapTo(OrderEntity entity)
{
    entity.Status = (OrderStatus)this.Status;
}
```

### Collection Mapping

Collections with matching element types:

```csharp
public class OrderModel
{
    public List<int> ItemIds { get; set; }
}

public class OrderEntity
{
    public List<int> ItemIds { get; set; }
}

// Generated - direct assignment (reference copy)
public partial void MapTo(OrderEntity entity)
{
    entity.ItemIds = this.ItemIds;
}
```

**Note:** This is a reference copy, not a deep clone. For deep copying, implement custom mapping.

## Multiple Mapper Methods

You can have multiple mapper methods for different types:

```csharp
[Factory]
public partial class PersonModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public AddressModel? Address { get; set; }

    // Map to/from different entity types
    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);

    public partial void MapFrom(PersonDto dto);
    public partial void MapTo(PersonDto dto);

    // Map specific subsets
    public partial void MapFrom(PersonSummary summary);
}
```

## Nested Object Mapping

For nested objects, you need to handle them manually or create separate mappers:

```csharp
[Factory]
public partial class OrderModel
{
    public int Id { get; set; }
    public CustomerModel? Customer { get; set; }  // Nested object
    public List<LineItemModel> Items { get; set; }  // Collection

    public partial void MapFrom(OrderEntity entity);
}

// You need to handle Customer and Items manually
[Fetch]
public async Task<bool> Fetch(int id, [Service] IOrderContext context)
{
    var entity = await context.Orders
        .Include(o => o.Customer)
        .Include(o => o.Items)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (entity == null) return false;

    // Use generated mapper for simple properties
    MapFrom(entity);

    // Handle nested objects manually
    if (entity.Customer != null)
    {
        Customer = new CustomerModel();
        Customer.MapFrom(entity.Customer);
    }

    // Handle collections
    Items = entity.Items.Select(i =>
    {
        var item = new LineItemModel();
        item.MapFrom(i);
        return item;
    }).ToList();

    return true;
}
```

## Complete Example

```csharp
// Entity
public class PersonEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string FirstName { get; set; } = null!;

    [Required]
    public string LastName { get; set; } = null!;

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

// Domain Model
[Factory]
public partial class PersonModel : IPersonModel, IFactorySaveMeta
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [MapperIgnore]
    public string FullName => $"{FirstName} {LastName}";

    [MapperIgnore]
    public bool IsValid => !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName);

    // Mapper declarations
    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);

    [Create]
    public PersonModel()
    {
        Created = DateTime.Now;
        Modified = DateTime.Now;
    }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(id);
        if (entity == null) return false;

        MapFrom(entity);  // Generated method
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
            entity = await context.Persons.FindAsync(Id)
                ?? throw new InvalidOperationException("Person not found");
        }

        Modified = DateTime.Now;
        MapTo(entity);  // Generated method
        await context.SaveChangesAsync();

        Id = entity.Id;
        IsNew = false;
    }
}
```

**Generated Mapper:**

```csharp
// PersonModelMapper.g.cs
public partial class PersonModel
{
    public partial void MapFrom(PersonEntity entity)
    {
        this.Id = entity.Id;
        this.FirstName = entity.FirstName;
        this.LastName = entity.LastName;
        this.Email = entity.Email;
        this.Phone = entity.Phone;
        this.Created = entity.Created;
        this.Modified = entity.Modified;
    }

    public partial void MapTo(PersonEntity entity)
    {
        entity.FirstName = this.FirstName
            ?? throw new NullReferenceException("PersonModel.FirstName");
        entity.LastName = this.LastName
            ?? throw new NullReferenceException("PersonModel.LastName");
        entity.Email = this.Email;
        entity.Phone = this.Phone;
        entity.Created = this.Created;
        entity.Modified = this.Modified;
        // Note: Id is not mapped because PersonEntity.Id has [Key] and is typically auto-generated
    }
}
```

## Viewing Generated Mappers

### Location

Generated mapper files appear in:
- `Dependencies/Analyzers/Neatoo.RemoteFactory.FactoryGenerator/Neatoo.RemoteFactory.FactoryGenerator.MapperGenerator/`

### File Naming

Pattern: `{Namespace}.{ClassName}Mapper.g.cs`

Example: `MyApp.DomainModel.PersonModelMapper.g.cs`

## Troubleshooting

### Mapper Not Generated

**Symptoms:** `MapFrom` or `MapTo` shows "not implemented"

**Causes:**
1. Class not marked as `partial`
2. Method not declared as `partial`
3. Target type not accessible

**Solution:**
```csharp
public partial class PersonModel  // Must be partial
{
    public partial void MapFrom(PersonEntity entity);  // Must be partial
}
```

### Property Not Mapped

**Symptoms:** A property isn't being mapped

**Causes:**
1. Names don't match exactly (case-sensitive)
2. Property is marked with `[MapperIgnore]`
3. Property is read-only in target

**Debug:** Check the generated file to see what's being mapped.

### NullReferenceException on MapTo

**Cause:** Mapping nullable source to non-nullable target with null value

**Solution:** Either ensure value is set, or change target to nullable

```csharp
// Before mapping
if (FirstName == null)
{
    FirstName = "";  // Provide default
}
MapTo(entity);
```

## Next Steps

- **[Factory Generator](factory-generator.md)**: Understanding factory generation
- **[Appendix: Internals](appendix-internals.md)**: Technical deep dive
- **[Attributes Reference](../reference/attributes.md)**: MapperIgnore documentation
