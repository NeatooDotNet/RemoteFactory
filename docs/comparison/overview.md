---
layout: default
title: "Framework Comparison Overview"
description: "Comparing RemoteFactory to CSLA and manual DTO approaches"
parent: Comparison
nav_order: 1
---

# Framework Comparison Overview

When architecting a 3-tier .NET application, you have several options for handling the communication between your UI and server. This section helps you make an informed decision by comparing RemoteFactory with other approaches.

## Why Compare?

Choosing the right architecture for your data access layer impacts:

- **Development speed**: How quickly can you build features?
- **Maintenance burden**: How much code do you need to maintain?
- **Flexibility**: Can you adapt to changing requirements?
- **Team learning curve**: How long before developers are productive?
- **Performance**: What's the runtime overhead?

## Comparison Dimensions

We evaluate each approach across several dimensions:

### Boilerplate Reduction

How much repetitive code does the approach eliminate?

| Approach | DTOs | Controllers | Factories | Mappers | Total Files |
|----------|------|-------------|-----------|---------|-------------|
| Manual DTOs | Write | Write | Write | Write | Many |
| CSLA | Optional | Minimal | Base class | Manual | Moderate |
| RemoteFactory | None | None | Generated | Generated | Minimal |

### Learning Curve

How long does it take to become productive?

| Approach | Initial Setup | Basic Usage | Advanced Features |
|----------|--------------|-------------|-------------------|
| Manual DTOs | Easy | Easy | N/A |
| CSLA | Moderate | Moderate | Steep |
| RemoteFactory | Easy | Easy | Moderate |

### Flexibility

How well does it adapt to different scenarios?

| Scenario | Manual DTOs | CSLA | RemoteFactory |
|----------|-------------|------|---------------|
| Custom serialization | Full control | Configurable | Customizable |
| API versioning | Easy | Moderate | Different pattern |
| External APIs | Native | Possible | Via DTOs |
| Complex business rules | Manual | Built-in | External |

### Ecosystem & Maturity

What's the community and support landscape?

| Aspect | Manual DTOs | CSLA | RemoteFactory |
|--------|-------------|------|---------------|
| Age | N/A | 20+ years | New |
| Documentation | General patterns | Extensive | Growing |
| Community | General .NET | Established | Emerging |
| Commercial support | N/A | Available | Not yet |

## Quick Comparison Matrix

| Feature | RemoteFactory | CSLA | Manual DTOs |
|---------|--------------|------|-------------|
| Code generation | Roslyn Source Generator | Runtime + optional codegen | None |
| Base class required | No | Yes | No |
| DTOs required | No | No (built-in serialization) | Yes |
| Controllers required | No (single endpoint) | No (data portal) | Yes |
| Authorization | Attribute-based | Rule-based | Manual |
| Business rules | External | Built-in engine | Manual |
| Validation | DataAnnotations | Built-in rules | Manual/DataAnnotations |
| Data binding | INotifyPropertyChanged | Full MVVM support | Manual |
| Compile-time safety | Yes | Partial | No special support |

## When to Consider Each Approach

### Choose RemoteFactory When

- Starting a **new project** with .NET 8+
- Building **Blazor WebAssembly** or **WPF** clients
- Wanting **minimal boilerplate** and fast development
- Comfortable with **source generators**
- Authorization needs are **straightforward to moderate**
- Team prefers **attribute-based** configuration

### Choose CSLA When

- Existing **CSLA investment** in codebase or team
- Complex **business rules** requiring a rules engine
- Need for **comprehensive documentation**
- Require **commercial support** options
- Building **large enterprise** applications
- Need full **MVVM infrastructure** out of the box

### Choose Manual DTOs When

- Need **API versioning** for external consumers
- Building a **public API** with documentation
- Integration with **existing infrastructure**
- Team is most comfortable with **traditional patterns**
- Requirements are **simple and won't grow**

## Migration Considerations

### From Manual DTOs to RemoteFactory

- Can adopt **incrementally** - new features with RemoteFactory
- Existing DTOs can **coexist** with RemoteFactory models
- Controllers can forward to RemoteFactory for specific operations

### From CSLA to RemoteFactory

- **Significant refactoring** required
- Different paradigm for authorization
- Consider for **new modules** rather than wholesale migration

## Detailed Comparisons

For in-depth analysis, see:

- **[RemoteFactory vs CSLA](vs-csla.md)**: Detailed comparison with CSLA framework
- **[RemoteFactory vs Manual DTOs](vs-dtos.md)**: Comparison with traditional DTO patterns
- **[Decision Guide](decision-guide.md)**: Flowchart and checklist for choosing

## Summary

| If You Value... | Best Choice |
|-----------------|-------------|
| Minimal code | RemoteFactory |
| Established ecosystem | CSLA |
| Full control | Manual DTOs |
| Fast development | RemoteFactory |
| Complex business rules | CSLA |
| API versioning | Manual DTOs |
| Source generators | RemoteFactory |
| Commercial support | CSLA |
