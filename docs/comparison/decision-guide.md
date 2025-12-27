---
layout: default
title: "Decision Guide"
description: "Flowchart and checklist for choosing RemoteFactory"
parent: Comparison
nav_order: 4
---

# Decision Guide

This guide helps you determine if RemoteFactory is the right choice for your project through a decision flowchart and project characteristics checklist.

## Decision Flowchart

```
                    START
                      │
                      ▼
            ┌─────────────────┐
            │ Is this a new   │
            │ .NET 8+ project?│
            └────────┬────────┘
                     │
           ┌─────────┴─────────┐
           │                   │
          YES                  NO
           │                   │
           ▼                   ▼
    ┌──────────────┐   ┌──────────────────┐
    │ Do you need  │   │ Are you migrating│
    │ a public API │   │ from CSLA?       │
    │ for external │   └────────┬─────────┘
    │ consumers?   │            │
    └──────┬───────┘   ┌────────┴────────┐
           │           │                 │
    ┌──────┴──────┐   YES               NO
    │             │    │                 │
   YES           NO    ▼                 ▼
    │             │   Keep CSLA      Consider
    ▼             ▼   (gradual       RemoteFactory
Manual DTOs     ┌─────────────────┐  for new
                │ Building Blazor │  modules
                │ WASM or WPF?    │
                └────────┬────────┘
                         │
                ┌────────┴────────┐
                │                 │
               YES               NO
                │                 │
                ▼                 ▼
         ┌────────────┐   ┌────────────────┐
         │ Team OK    │   │ Web API for    │
         │ with source│   │ mobile/SPA?    │
         │ generators?│   └───────┬────────┘
         └─────┬──────┘           │
               │           ┌──────┴──────┐
        ┌──────┴──────┐    │             │
        │             │   YES            NO
       YES           NO    │             │
        │             │    ▼             ▼
        ▼             ▼   Manual      Evaluate
   RemoteFactory   Evaluate           needs
   RECOMMENDED     alternatives

```

## Project Characteristics Checklist

Score your project against these criteria. The more boxes you check, the better fit RemoteFactory is.

### Strong Indicators for RemoteFactory

- [ ] **New project starting with .NET 8 or later**
- [ ] **Blazor WebAssembly client**
- [ ] **WPF client with server backend**
- [ ] **Single client platform** (not multiple API consumers)
- [ ] **Team comfortable with source generators**
- [ ] **Want minimal boilerplate code**
- [ ] **Standard CRUD operations** dominate the application
- [ ] **Authorization is role-based or straightforward**
- [ ] **Using Entity Framework Core** for data access
- [ ] **Internal business application** (not public API)

### Neutral Factors

- [ ] Blazor Server (works, but less clear benefit)
- [ ] Mixed client types (web + mobile)
- [ ] Moderate business rule complexity
- [ ] Some API versioning needs

### Indicators Against RemoteFactory

- [ ] **Public REST API** for external consumers
- [ ] **Existing CSLA codebase** with significant investment
- [ ] **Complex business rules** requiring rules engine
- [ ] **API versioning requirements** for backward compatibility
- [ ] **Multiple client platforms** with different data needs
- [ ] **Team unfamiliar with modern .NET** patterns

## Scoring Guide

**8-10 checks in "Strong Indicators"**: RemoteFactory is an excellent fit

**5-7 checks in "Strong Indicators"**: RemoteFactory is likely a good fit, evaluate neutral factors

**2-4 checks in "Strong Indicators"**: Consider carefully, may work for specific modules

**Any checks in "Indicators Against"**: Evaluate those concerns specifically

## Scenario Analysis

### Scenario 1: New Blazor Enterprise App

**Project:** Internal HR management system
**Stack:** .NET 8, Blazor WASM, SQL Server, Entity Framework Core
**Team:** 4 senior C# developers, familiar with source generators

| Factor | Assessment |
|--------|------------|
| New .NET 8+ | Yes |
| Blazor WASM | Yes |
| Internal app | Yes |
| Standard CRUD | Yes |
| Team capability | Good |

**Recommendation:** Strong fit for RemoteFactory

---

### Scenario 2: Public API Platform

**Project:** E-commerce API for mobile apps and third-party integrations
**Stack:** .NET 8, Web API, PostgreSQL
**Requirements:** API versioning, OpenAPI documentation, rate limiting

| Factor | Assessment |
|--------|------------|
| Public API | Yes (indicator against) |
| API versioning | Required |
| Multiple clients | Yes |
| External consumers | Yes |

**Recommendation:** Manual DTOs with standard Web API patterns

---

### Scenario 3: Legacy Modernization

**Project:** Migrating .NET Framework WinForms app to .NET 8 WPF
**Existing:** CSLA-based business layer
**Team:** Experienced with CSLA

| Factor | Assessment |
|--------|------------|
| CSLA investment | Significant |
| Team knowledge | CSLA experts |
| Migration scope | Full application |

**Recommendation:** Continue with CSLA, or consider RemoteFactory for new modules only

---

### Scenario 4: Microservices Communication

**Project:** Internal service-to-service communication
**Stack:** .NET 8, Docker, Kubernetes
**Requirements:** gRPC or minimal APIs, high performance

| Factor | Assessment |
|--------|------------|
| Internal services | Yes |
| Performance critical | Yes |
| No UI layer | True |

**Recommendation:** gRPC or minimal APIs directly - RemoteFactory is designed for UI-to-server

---

### Scenario 5: Small Team, Rapid Development

**Project:** Startup MVP for inventory management
**Stack:** .NET 8, Blazor WASM, SQLite
**Team:** 2 developers, need to move fast

| Factor | Assessment |
|--------|------------|
| Speed priority | High |
| Small team | Yes |
| Minimal infrastructure | Preferred |
| Standard CRUD | Yes |

**Recommendation:** RemoteFactory is ideal for rapid development

## Questions to Ask

### About Your Project

1. **Who consumes your API?**
   - Only your own clients → RemoteFactory friendly
   - External developers → Consider manual DTOs

2. **How complex are your business rules?**
   - Simple validation → RemoteFactory + DataAnnotations
   - Complex interdependent rules → Consider CSLA

3. **How often will you add entities?**
   - Frequently → RemoteFactory's code generation pays off
   - Rarely → Less benefit from generation

### About Your Team

1. **Is the team comfortable with source generators?**
   - Yes → RemoteFactory is natural
   - No → May need ramp-up time

2. **What's the team's background?**
   - Modern .NET → RemoteFactory fits well
   - Heavy CSLA experience → May prefer familiar patterns

3. **How important is documentation?**
   - Critical → CSLA has more resources
   - Self-reliant team → RemoteFactory is learnable

### About Your Timeline

1. **Is this a greenfield or brownfield project?**
   - Greenfield → RemoteFactory is easy to adopt
   - Brownfield → Consider incremental adoption

2. **What's the project lifespan?**
   - Long-term → Weigh ecosystem maturity
   - Short-term/MVP → Speed of development matters more

## Adoption Strategies

### Full Adoption

For new projects where RemoteFactory fits well:

```
Day 1: Install packages
Day 1: Create first domain model with [Factory]
Day 2-N: Build features using RemoteFactory patterns
```

### Incremental Adoption

For existing projects or mixed requirements:

```
Phase 1: Add RemoteFactory packages
Phase 2: Use RemoteFactory for new features
Phase 3: Keep existing DTOs for public APIs
Phase 4: Optionally migrate internal APIs over time
```

### Evaluation Period

Not sure? Try it on a small feature:

```
1. Create one domain model with RemoteFactory
2. Build a complete CRUD feature
3. Evaluate developer experience
4. Decide on broader adoption
```

## Final Recommendation Matrix

| Your Situation | Recommendation |
|----------------|----------------|
| New Blazor/WPF app, internal use | **RemoteFactory** |
| New public API | Manual DTOs |
| Existing CSLA project | Keep CSLA |
| Complex business rules | CSLA or manual |
| MVP/Prototype | **RemoteFactory** |
| Microservices | gRPC/minimal APIs |
| Mixed internal/external | DTOs for external, RemoteFactory for internal |

## Next Steps

If RemoteFactory seems right for your project:

1. **[Installation Guide](../getting-started/installation.md)** - Get started
2. **[Quick Start](../getting-started/quick-start.md)** - Build your first feature
3. **[Examples](../examples/blazor-app.md)** - See complete implementations
