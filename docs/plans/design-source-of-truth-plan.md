# Design Source of Truth - Implementation Plan

**Date:** 2026-01-30
**Related Todo:** [Create Design Source of Truth Projects](../todos/design-source-of-truth.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-01-30
**Reviewed By:** remotefactory-architect (2026-01-30)

---

## Overview

Create a set of C# projects in `src/Design/` that serve as the authoritative reference for RemoteFactory's API design. These projects are specifically designed for Claude Code to understand, reason about, and extend the API.

---

## Approach

Build four interconnected projects that mirror the real-world usage patterns of RemoteFactory across all supported platforms:

1. **Design.Domain** - Class library showing domain object patterns
2. **Design.Tests** - Unit tests demonstrating testing approaches and API contracts
3. **Design.Server** - ASP.NET Core project showing server integration
4. **Design.Client.Blazor** - Blazor WASM project showing client-side usage

Each project will be heavily commented with four types of annotations:
- **API documentation** - What this code demonstrates
- **Design rationale** - Why this approach was chosen
- **Rejected alternatives** - What was NOT done and why (often with commented-out code)
- **Generator behavior** - What code the source generator produces

The projects must demonstrate all **three factory patterns**:
1. **Class Factory** - `[Factory]` on a class (e.g., `Order.cs`)
2. **Static Factory** - `[Factory]` on a static class with `[Execute]` or `[Event]` methods
3. **Interface Factory** - `[Factory]` on an interface for remote service proxies

---

## Design

### Directory Structure

```
src/Design/
├── Design.sln
├── README.md                    # Explains purpose for humans
├── CLAUDE-DESIGN.md            # Detailed guidance for Claude Code
├── Design.Domain/
│   ├── Design.Domain.csproj
│   ├── FactoryPatterns/
│   │   └── AllPatterns.cs      # Side-by-side comparison of all 3 patterns
│   ├── Aggregates/
│   │   └── Order.cs            # CLASS FACTORY - aggregate root
│   ├── Entities/
│   │   └── OrderLine.cs        # CLASS FACTORY - child entity (no [Remote])
│   ├── ValueObjects/
│   │   └── Money.cs            # Value object example
│   ├── Services/
│   │   ├── IOrderRepository.cs # INTERFACE FACTORY - remote service proxy
│   │   └── OrderCommands.cs    # STATIC FACTORY - [Execute] methods
│   └── Events/
│       └── OrderEvents.cs      # STATIC FACTORY - [Event] methods
├── Design.Tests/
│   ├── Design.Tests.csproj
│   ├── FactoryTests/
│   │   ├── ClassFactoryTests.cs
│   │   ├── StaticFactoryTests.cs
│   │   ├── InterfaceFactoryTests.cs
│   │   └── SaveTests.cs
│   └── SerializationTests/
│       └── RoundTripTests.cs
├── Design.Server/
│   ├── Design.Server.csproj
│   ├── Program.cs
│   ├── OrderRepository.cs      # Server-only implementation of IOrderRepository
│   └── appsettings.json
└── Design.Client.Blazor/
    ├── Design.Client.Blazor.csproj
    ├── Program.cs
    └── Pages/
        └── Orders.razor
```

### Comment Standards

#### API Documentation Comments

```csharp
/// <summary>
/// Demonstrates: [RemoteCreate] attribute on aggregate root.
///
/// Key points:
/// - Constructor injection ([Service]) = available on both client and server
/// - Method injection ([Service] on parameters) = server-only, the common case
/// - [Remote] marks client-to-server entry points, not internal server calls
/// </summary>
```

#### Design Rationale Comments

```csharp
// DESIGN DECISION: We use [Remote] only on aggregate roots, not child entities.
// Once execution crosses to the server via [RemoteCreate] or [RemoteFetch],
// subsequent method calls stay server-side. Child entity factories are called
// from within aggregate operations, so they don't need [Remote].
//
// See: src/RemoteFactory/Attributes/RemoteAttribute.cs for implementation
```

#### Rejected Alternative Comments

```csharp
// DID NOT DO THIS: Have [Remote] automatically propagate to child factories
//
// Reasons:
// 1. Violates single-responsibility - each method should declare its own boundary
// 2. Makes it unclear where the client/server boundary actually is
// 3. Would require complex static analysis in the generator
//
// The explicit approach is more verbose but clearer:
//
// // REJECTED PATTERN:
// // [Remote]  <-- This would auto-propagate
// // public class Order { ... }
// //
// // ACTUAL PATTERN:
// // [Remote]  <-- Explicit on Order
// // public class Order {
// //     // No [Remote] on OrderLine factory calls - they happen server-side
// //     private OrderLine CreateLine(...) { ... }
// // }
```

#### Generator Behavior Comments

```csharp
// GENERATOR BEHAVIOR: For this [Remote, Create] method, the generator produces:
//
// 1. Delegate type:
//    delegate Task RemoteCreate_Order_Create(IOrderLineListFactory lineListFactory);
//
// 2. Factory method in IOrderFactory:
//    IOrder Create();
//
// 3. DI registration:
//    services.AddScoped<RemoteCreate_Order_Create>(...);
//
// 4. Remote stub (client-side):
//    Serializes call to server, deserializes response
```

#### Common Mistake Comments

```csharp
// COMMON MISTAKE: Marking child entity methods as [Remote]
//
// WRONG:
// [Factory]
// public class OrderLine {
//     [Remote, Fetch]  // <-- WRONG: Child entities don't need [Remote]
//     public Task Fetch(...) { }
// }
//
// RIGHT:
// [Factory]
// public class OrderLine {
//     [Fetch]  // <-- No [Remote] - called from server-side Order operations
//     public Task Fetch(...) { }
// }
//
// Why: Once execution reaches the server via the aggregate root's [Remote]
// method, all subsequent calls stay server-side.
```

### API Coverage Checklist

The design projects must demonstrate ALL of these:

**Factory Patterns (the three types):**
- [ ] Class Factory - `[Factory]` on a class (Order aggregate root)
- [ ] Static Factory - `[Factory]` on static class with `[Execute]`/`[Event]` methods
- [ ] Interface Factory - `[Factory]` on interface for remote service proxies

**Factory Operations:**
- [ ] `[Remote, Create]` - Creating new aggregate roots
- [ ] `[Remote, Fetch]` - Fetching existing aggregate roots
- [ ] `[Remote, Save]` - Persisting aggregate changes (via `IFactorySaveMeta`)
- [ ] `[Remote, Execute]` - Stateless command execution
- [ ] `[Event]` - Fire-and-forget operations with isolated scope

**Assembly-Level Attributes:**
- [ ] `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` - Client-only generation
- [ ] `[assembly: FactoryMode(FactoryMode.Full)]` - Server generation (default)
- [ ] `[assembly: FactoryHintNameLength(N)]` - Hint name length control

**NeatooFactory Modes:**
- [ ] `NeatooFactory.Server` - Full local execution + handles remote requests
- [ ] `NeatooFactory.Remote` - Remote stubs only, calls server via HTTP
- [ ] `NeatooFactory.Logical` - Full local execution, no remote stubs (single-tier)

**Service Injection:**
- [ ] Constructor injection with `[Service]` - available on both client AND server
- [ ] Method parameter injection with `[Service]` - server-only (common case)
- [ ] The difference demonstrated side-by-side with "DESIGN DECISION" comment

**Domain Patterns:**
- [ ] Aggregate root with `[Remote]` entry points
- [ ] Child entities WITHOUT `[Remote]` (server-side only)
- [ ] Value objects and their serialization
- [ ] Entity collections within aggregates

**Lifecycle Hooks:**
- [ ] `IFactoryOnStart` / `IFactoryOnStartAsync` - Pre-operation hooks
- [ ] `IFactoryOnComplete` / `IFactoryOnCompleteAsync` - Post-operation hooks
- [ ] `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` - Cancellation hooks
- [ ] `IFactorySaveMeta` - Save routing based on `IsNew`/`IsDeleted`

**Authorization:**
- [ ] `[AuthorizeFactory<T>]` attribute on class/interface
- [ ] `[AuthorizeFactory(AuthorizeFactoryOperation)]` on authorization methods
- [ ] `AuthorizeFactoryOperation` flags: `Read`, `Write`, `Execute`

**Serialization:**
- [ ] `NeatooJsonSerializer` usage
- [ ] `IOrdinalSerializable` interface (auto-generated)
- [ ] `SerializationFormat.Ordinal` vs `SerializationFormat.Json`
- [ ] Circular reference handling
- [ ] Private member serialization
- [ ] Record type serialization

**ASP.NET Core Integration:**
- [ ] `AddRemoteFactoryServer()` configuration
- [ ] `MapRemoteFactory()` endpoint mapping
- [ ] Authorization configuration

**Blazor Client:**
- [ ] `AddRemoteFactoryClient()` configuration
- [ ] `UseRemoteFactory()` HTTP configuration
- [ ] Calling [Remote] methods from components

**Testing Patterns:**
- [ ] ClientServerContainers pattern for round-trip tests
- [ ] Client container, Server container, Local container
- [ ] Verifying serialization fidelity
- [ ] "DESIGN DECISION" comment explaining why this approach vs HTTP/mocking

**Conventions:**
- [ ] `RegisterMatchingName` convention (`IOrderRepository` → `OrderRepository`)

### Evolution Strategy

When the API changes:

1. **Update Design.* projects first** - This is the source of truth
2. **Add "was/now" comments** for changed behavior:
   ```csharp
   // CHANGED in v11.0: Previously required explicit [Serialize] attribute.
   // Now all public properties are serialized by default.
   //
   // OLD (v10.x):
   // [Serialize]
   // public string Name { get; set; }
   //
   // NEW (v11.0+):
   // public string Name { get; set; }  // Serialized by default
   ```
3. **Update main codebase** to implement the change
4. **Update samples/examples** to reflect new patterns
5. **Update user documentation** last

### Who Updates Design Projects

| Who | When |
|-----|------|
| **Architect Agent** | When designing new features - updates Design.* first |
| **Developer Agent** | When implementing - ensures Design.* matches implementation |
| **Before any PR that changes public API** | Design.* must be updated |

### Validation Requirements

- Tests in Design.Tests must pass
- All `DESIGN DECISION` comments must remain accurate
- No commented-out code should be stale (outdated rejected patterns)
- `GENERATOR BEHAVIOR` comments must match actual generator output

---

## Implementation Steps

### Phase 1: Foundation

1. Create `src/Design/Design.sln` solution
2. Create `Design.Domain` project with basic structure
3. Create `Design.Tests` project that references Domain
4. Add project references to RemoteFactory source projects
5. Verify the solution builds

### Phase 2: Pattern Documentation

6. Create `FactoryPatterns/AllPatterns.cs` showing all three patterns side-by-side
7. Add extensive comments explaining when to use each pattern
8. Document what the generator produces for each pattern

### Phase 3: Class Factory Pattern

9. Implement `Order` aggregate root (Class Factory)
10. Implement `OrderLine` child entity (no `[Remote]`)
11. Implement `Money` value object
12. Add lifecycle hooks (`IFactoryOnStart`, `IFactorySaveMeta`, etc.)
13. Add comprehensive comments including "COMMON MISTAKE" for child [Remote]

### Phase 4: Interface Factory Pattern

14. Create `IOrderRepository` interface (Interface Factory)
15. Document the `RegisterMatchingName` convention
16. Show server-only implementation pattern

### Phase 5: Static Factory Pattern

17. Create `OrderCommands.cs` with `[Execute]` methods
18. Create `OrderEvents.cs` with `[Event]` methods
19. Document fire-and-forget pattern and isolated scope

### Phase 6: Testing Patterns

20. Implement ClientServerContainers-based tests
21. Add "DESIGN DECISION" comment explaining why this approach
22. Test all three factory patterns
23. Implement serialization round-trip tests

### Phase 7: Server/Client Integration

24. Create `Design.Server` ASP.NET Core project
25. Implement `OrderRepository` (server-only)
26. Configure `AddRemoteFactoryServer()` and `MapRemoteFactory()`
27. Create `Design.Client.Blazor` project
28. Configure `AddRemoteFactoryClient()` and `UseRemoteFactory()`
29. Create sample page demonstrating all patterns

### Phase 8: Documentation

30. Create `README.md` explaining the purpose
31. Create `CLAUDE-DESIGN.md` with Claude-specific guidance
32. Update main `CLAUDE.md` to reference design projects as source of truth

---

## Acceptance Criteria

- [ ] All four projects compile without errors
- [ ] All tests pass
- [ ] All three factory patterns demonstrated (Class, Static, Interface)
- [ ] Every public API element from checklist is demonstrated with comments
- [ ] At least 10 "DID NOT DO THIS BECAUSE" comments documenting rejected alternatives
- [ ] At least 5 "DESIGN DECISION" comments explaining key choices
- [ ] At least 3 "GENERATOR BEHAVIOR" comments showing generated output
- [ ] At least 3 "COMMON MISTAKE" comments showing incorrect patterns
- [ ] CLAUDE.md updated to reference src/Design as source of truth
- [ ] Can be used to answer "How does X work in RemoteFactory?" questions

---

## Out of Scope

**Explicitly NOT included in Design projects:**

- EF Core integration (use mock/in-memory repositories instead)
- Complex UI implementation (minimal Blazor, just enough to show patterns)
- Performance optimization examples
- Multi-tenancy patterns
- Complex validation scenarios beyond basic demonstration
- Real database access
- Production-ready error handling (keep examples focused)

**Differentiation from Examples/Samples:**

| Aspect | Examples/Samples | Design.* |
|--------|------------------|----------|
| Purpose | User learning | AI comprehension |
| Complexity | Real-world scenarios | Minimal viable demonstrations |
| Comments | Minimal, clean | Extensive, including rejected alternatives |
| Rejected alternatives | None shown | Multiple per file |
| Target audience | Developers evaluating/learning | Claude Code understanding API |

---

## Dependencies

- RemoteFactory source projects (via project references to stay synchronized)
- .NET 9.0 SDK (or latest supported)
- No external packages beyond what RemoteFactory already requires

---

## Risks / Considerations

1. **Maintenance burden** - These projects must be updated whenever the API changes. Mitigated by making it the first step in the design workflow.

2. **Scope creep** - Temptation to add too much. Keep focused on API demonstration, not comprehensive examples.

3. **Duplication with samples** - Some overlap is intentional. Design projects optimize for AI comprehension; samples optimize for user learning.

4. **Comment rot** - Old comments becoming inaccurate. Mitigated by making this the source of truth that flows to everything else.

---

## Decisions

1. **Project references** - Design projects reference RemoteFactory via project references (not NuGet). This keeps everything synchronized during development.

2. **Internal details** - Include what helps Claude Code make design decisions. Start with public API, add internals as needed.

3. **Rejected alternatives** - Use comments only (not separate files). Keeps them in context where they're most useful.

---

## Architectural Verification

**Three Patterns Analysis:**
- Standalone: N/A (this is infrastructure, not feature implementation)
- Inline Interface: N/A
- Inline Class: N/A

**Breaking Changes:** No - this is additive infrastructure

**Pattern Consistency:** Follows existing project organization patterns in src/

**Codebase Analysis:** Examined existing project structure in src/, test patterns in RemoteFactory.IntegrationTests, and CLAUDE.md for documentation standards.

---

## Architect Review

**Status:** Complete (2026-01-30)

**Findings incorporated:**
- Added all three factory patterns (Class, Static, Interface)
- Expanded API coverage checklist with lifecycle hooks, events, authorization
- Added GENERATOR BEHAVIOR and COMMON MISTAKE comment patterns
- Added Out of Scope section
- Added Who Updates and When maintenance section
- Reordered implementation phases for progressive complexity

---

## Developer Review

**Status:** Approved

**Reviewed By:** remotefactory-developer (2026-01-30)

### Review Summary

The plan is well-structured and implementable. The architect has done thorough analysis of the API coverage requirements. I have verified the plan against the existing codebase and found no technical blockers.

### Implementation Phase Sequencing - Approved

The 8-phase structure is correctly sequenced:
1. **Phase 1 (Foundation)** must come first - creates solution structure
2. **Phase 2 (Pattern Documentation)** builds on Phase 1 - requires projects to exist
3. **Phases 3-5 (Factory Patterns)** are logically ordered: Class Factory demonstrates the most common case, then Interface Factory, then Static Factory with Events
4. **Phase 6 (Testing)** correctly follows pattern implementation - tests need targets
5. **Phase 7 (Server/Client)** depends on Domain project being complete
6. **Phase 8 (Documentation)** is correctly last - documents what was built

No missing dependencies identified.

### Technical Feasibility - Verified

**Project References:** The plan correctly specifies project references to RemoteFactory source projects. Verified this pattern works in existing tests:
- `RemoteFactory.IntegrationTests.csproj` uses `<ProjectReference Include="..\..\RemoteFactory\RemoteFactory.csproj" />` and `<ProjectReference Include="..\..\Generator\Generator.csproj" OutputItemType="Analyzer" />`
- Same pattern will work for Design projects

**Generator Output:** Design.Tests will need `EmitCompilerGeneratedFiles=true` and `CompilerGeneratedFilesOutputPath=Generated` like IntegrationTests to capture generated code for GENERATOR BEHAVIOR comments.

**ClientServerContainers Pattern:** Verified the testing infrastructure exists and can be reused:
- `ClientServerContainers.Scopes()` method creates client/server/local scopes
- `MakeSerializedServerStandinDelegateRequest` handles round-trip serialization
- Custom service registration supported via `Scopes(configureClient, configureServer)` overload

### API Coverage Checklist - Minor Clarifications

**Verified API elements exist in codebase:**
- [x] `[Factory]`, `[Remote]`, `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]`, `[Event]` - all in `FactoryAttributes.cs`
- [x] `[Service]` attribute for injection - in `FactoryAttributes.cs`
- [x] `[AuthorizeFactory<T>]` and `[AuthorizeFactory(operation)]` - in `FactoryAttributes.cs`
- [x] `IFactorySaveMeta`, `IFactoryOnStart`, `IFactoryOnComplete`, `IFactoryOnCancelled` - separate interface files
- [x] `FactoryMode.Full`/`FactoryMode.RemoteOnly` with `[assembly: FactoryMode]` - in `FactoryAttributes.cs`
- [x] `NeatooFactory.Server`/`Remote`/`Logical` enum - used in `AddNeatooRemoteFactory()`
- [x] `RegisterMatchingName` convention - in `ClientServerContainers.cs` setup

**Minor clarification needed during implementation:**
- The plan references `[RemoteCreate]`, `[RemoteFetch]`, `[RemoteSave]` syntax, but the actual API uses separate attributes: `[Remote, Create]`, `[Remote, Fetch]`, etc. This is cosmetic - the plan shows these correctly in examples.

### Testing Patterns - Achievable

The ClientServerContainers infrastructure fully supports what the plan requires:
- Creating isolated client/server/local scopes
- Custom service registration per scope
- Serialization round-trip verification
- All three NeatooFactory modes

Design.Tests can follow the same patterns as `RemoteFactory.IntegrationTests/FactoryRoundTrip/` for round-trip tests.

### Comment Requirements - Achievable

| Requirement | Assessment |
|-------------|------------|
| 10+ "DID NOT DO THIS BECAUSE" | Achievable - many design decisions have rejected alternatives (e.g., auto-propagating [Remote], reflection-based serialization, single DI container) |
| 5+ "DESIGN DECISION" | Achievable - constructor vs method injection, aggregate vs child [Remote], etc. |
| 3+ "GENERATOR BEHAVIOR" | Achievable - will show generated delegate types, factory interfaces, DI registrations |
| 3+ "COMMON MISTAKE" | Achievable - child entity [Remote], wrong NeatooFactory mode, missing [Service] |

### Concerns - None Blocking

**Minor observation (not blocking):**
- The plan lists `[assembly: FactoryHintNameLength(N)]` but does not explain what this is for. During implementation, the comments should clarify this is for controlling generated file hint name lengths for large codebases. Not a blocker.

**Recommendation for Phase 1:**
- Create a separate `Design.sln` as specified, but consider whether to also add Design projects to main solution during development for easier IDE navigation. The plan already specifies separate solution "to avoid noise" - recommend following the plan.

### Conclusion

**Approved for implementation.** The plan is comprehensive, correctly sequenced, and technically feasible. All required patterns and APIs exist in the codebase. The testing infrastructure is mature and can be reused. Proceed to Implementation Contract phase.

---

## Implementation Contract

[To be filled before implementation]

**In Scope:**
- [ ] (To be defined)

**Out of Scope:**
- (To be defined)

---

## Implementation Progress

[To be filled during implementation]

---

## Completion Evidence

[Required before marking complete]

- **Tests Passing:** (To be provided)
- **Build Output:** (To be provided)
- **All Checklist Items:** (To be confirmed)
