---
layout: default
title: "Ordinal Serialization Design"
description: "Internal design document for ordinal JSON serialization"
nav_exclude: true
---

# Ordinal Serialization Design

This document describes the internal design and implementation of ordinal JSON serialization in RemoteFactory.

## Implementation Status

**Status:** COMPLETED - Implementation Notes

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1: Property Order Infrastructure | Completed | Alphabetical ordering, base class first |
| Phase 2: Custom JsonConverter | Completed | NeatooOrdinalConverterFactory |
| Phase 3: Source Generator Changes | Completed | IOrdinalSerializable, IOrdinalSerializationMetadata |
| Phase 4: Interface Serialization | Completed | Uses `$type`/`$value` (reads both) |
| Phase 5: Reference Handling | Completed | Works with `$id`/`$ref` |
| Phase 6: ObjectJson Updates | DEFERRED | Property names remain `Json`/`AssemblyType` |
| Phase 7: Backward Compatibility | Completed | Server detects format, X-Neatoo-Format header |

**Key Implementation Details:**
- Options class: `NeatooSerializationOptions`
- Format enum: `SerializationFormat.Ordinal` / `SerializationFormat.Named`
- Phase 4: Write uses `$type`/`$value`, Read accepts both `$type`/`$value` AND `$t`/`$v`
- Phase 6: Deferred - ObjectJson still uses full property names for stability

---

## Overview

Ordinal serialization eliminates property names from serialized JSON by using positional arrays instead of named objects. Since both client and server have the compiled type definitions, property names are redundant.

## Format Comparison

| Named Format | Ordinal Format | Savings |
|--------------|----------------|---------|
| `{"Name":"John","Age":42,"Active":true}` | `["John",42,true]` | ~45% |
| `{"FirstName":"Alice","LastName":"Smith","Email":"a@b.com"}` | `["Alice","Smith","a@b.com"]` | ~50% |

For typical objects with 5-10 properties, payload reduction is **40-50%**.

## Property Ordering

Properties are ordered:
1. **Alphabetically by name** - deterministic across compilations
2. **Base class properties first** - then derived class properties

Example for `Employee : Person`:
```
Base (Person): Active, Age, Name
Derived (Employee): Department
Final order: [Active, Age, Name, Department]
```

## Interface Serialization

When serializing interface-typed properties:

```json
{
  "$type": "Namespace.ConcretePerson",
  "$value": ["John", 42, true]
}
```

Reading accepts both:
- `$type`/`$value` (current implementation)
- `$t`/`$v` (for future/compact format)

## Edge Cases

### Nullable Properties

Nulls are explicit in the positional array:
```json
["John", null, 42]
```

### Collections

Collections serialize as nested arrays:
```json
["Engineering", ["Alice", "Bob", "Charlie"]]
```

### Nested Objects

Nested objects use ordinal format recursively:
```json
[["123 Main", "Boston", "MA"], "ORD-001"]
```

## Format Negotiation

The server communicates its format preference via HTTP header:

```
X-Neatoo-Format: ordinal
```

Flow:
1. Client makes request (uses ordinal by default)
2. Server includes `X-Neatoo-Format` header in response
3. Client adapts to server's preferred format
4. Server always accepts both formats (detects by first character)

## Configuration

```csharp
// Server configuration
services.AddNeatooRemoteFactory(
    NeatooFactory.Server,
    new NeatooSerializationOptions { Format = SerializationFormat.Named },
    typeof(MyModel).Assembly);
```

## Key Files

| File | Purpose |
|------|---------|
| `NeatooOrdinalConverterFactory.cs` | JsonConverter for ordinal format |
| `NeatooInterfaceJsonTypeConverter.cs` | Interface `$type`/`$value` wrapper |
| `IOrdinalSerializable.cs` | Interface for ToOrdinalArray() |
| `IOrdinalSerializationMetadata.cs` | Static abstract FromOrdinalArray() |
| `NeatooSerializationOptions.cs` | Format configuration |
| `SerializationFormat.cs` | Ordinal/Named enum |
