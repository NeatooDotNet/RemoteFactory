---
layout: default
title: "Design Constraints"
description: "Core assumptions and constraints that shape RemoteFactory's design"
parent: Concepts
nav_order: 0
---

# Design Constraints

This document captures the fundamental design constraints and assumptions that shape RemoteFactory. Understanding these helps explain why certain features are supported or excluded.

## Core Assumption: Client/Server Serialization

**All factory-managed types must be serializable across the network.**

RemoteFactory generates factories for 3-tier architectures where domain objects travel between client and server via JSON serialization. This fundamental requirement drives many design decisions.

## Constraints

### No Struct Support

**RemoteFactory does not support `struct` or `record struct` types.**

Structs are value types that:
- Are copied on assignment and parameter passing
- Cannot be `null` (without `Nullable<T>`)
- Have different serialization semantics than reference types
- Lose identity when boxed for interface usage

These characteristics conflict with RemoteFactory's architecture:

| Concern | Why Structs Don't Fit |
|---------|----------------------|
| **Identity tracking** | Factories track object instances; structs copy on every assignment |
| **Interface serialization** | Factory interfaces (`IPersonModel`) require boxing structs, losing type fidelity |
| **Reference preservation** | JSON serialization with `$ref` doesn't work with value types |
| **Null handling** | Factory operations return `null` for "not found" scenarios |

**Recommendation**: Use `record class` (or just `record`) for Value Objects that need factory support.

```csharp
// Supported - record class (reference type)
[Factory]
public record Address(string Street, string City, string PostalCode);

// NOT supported - record struct (value type)
// [Factory]
// public record struct Money(decimal Amount, string Currency);
```

### No Generic Type Factories

**RemoteFactory does not generate factories for open generic types.**

```csharp
// NOT supported - generic type
// [Factory]
// public class Repository<T> { }

// Supported - closed generic in inheritance
[Factory]
public class PersonRepository : Repository<Person> { }
```

Closed generic types (specific type arguments) are supported through inheritance.

### No Abstract Type Factories

**RemoteFactory does not generate factories for abstract classes or interfaces directly.**

Abstract types cannot be instantiated, so `[Create]` operations have no meaning. Use concrete implementations:

```csharp
// NOT supported directly
// [Factory]
// public abstract class EntityBase { }

// Supported - concrete implementation
[Factory]
public class PersonModel : EntityBase { }
```

### Partial Class Requirement

**All factory-decorated types must be declared as `partial`.**

The source generator adds members to your types. Without `partial`, it cannot extend the class:

```csharp
// Required
[Factory]
public partial class PersonModel { }
```

## Supported Patterns

Given these constraints, RemoteFactory excels with:

- **Domain Entities** - Classes with identity and lifecycle (Create, Fetch, Update, Delete)
- **Aggregate Roots** - Top-level domain objects that own child entities
- **Value Objects** - Immutable `record class` types for domain concepts
- **Static Commands** - `[Execute]` operations in static classes for RPC-style calls

## Related Documentation

- [Architecture Overview](architecture-overview.md) - How RemoteFactory works
- [Factory Operations](factory-operations.md) - Available CRUD operations
- [Attributes Reference](../reference/attributes.md) - Complete attribute documentation
