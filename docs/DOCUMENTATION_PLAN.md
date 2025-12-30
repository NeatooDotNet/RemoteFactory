# RemoteFactory Documentation Plan

## Executive Summary

This document outlines the comprehensive documentation plan for Neatoo RemoteFactory, a Roslyn Source Generator-powered Data Mapper Factory framework that enables 3-tier client/server architecture without the overhead of manually written DTOs, factories, or service layer boilerplate.

**Target Audience:** Senior C# software engineers who are already experts in C# and are evaluating or implementing RemoteFactory for their projects.

**Documentation Goals:**
1. Help engineers understand what RemoteFactory does and its core value proposition
2. Demonstrate how RemoteFactory eliminates boilerplate and reduces development time
3. Provide clear, practical guidance on how to use RemoteFactory effectively
4. Showcase all features with real-world examples
5. Enable informed decision-making through framework comparisons

---

## 1. Documentation Structure Overview

```
docs/
├── DOCUMENTATION_PLAN.md          # This file
├── index.md                        # Documentation home/landing page
│
├── getting-started/
│   ├── installation.md             # NuGet installation and project setup
│   ├── quick-start.md              # 5-minute tutorial to first working example
│   └── project-structure.md        # Recommended project organization
│
├── concepts/
│   ├── architecture-overview.md    # High-level architecture and data flow
│   ├── factory-pattern.md          # How factories work in RemoteFactory
│   ├── three-tier-execution.md     # Remote vs Local vs Logical execution modes
│   ├── factory-operations.md       # Create, Fetch, Insert, Update, Delete, Execute
│   └── service-injection.md        # Using [Service] attribute for DI
│
├── authorization/
│   ├── authorization-overview.md   # Authorization concepts and patterns
│   ├── custom-authorization.md     # Using [AuthorizeFactory<T>] attribute
│   ├── asp-authorize.md            # ASP.NET Core policy-based authorization
│   └── can-methods.md              # Generated CanCreate, CanFetch, etc. methods
│
├── source-generation/
│   ├── how-it-works.md             # High-level understanding (what you need to know)
│   ├── factory-generator.md        # Factory generation explained
│   └── appendix-internals.md       # Technical appendix (deep dive for curious readers)
│
├── reference/
│   ├── attributes.md               # All attributes with descriptions and examples
│   ├── interfaces.md               # All interfaces (IFactorySaveMeta, IFactoryOnComplete, etc.)
│   ├── factory-modes.md            # NeatooFactory enum (Server, Remote, Logical)
│   └── generated-code.md           # Understanding generated factory structure
│
├── examples/
│   ├── blazor-app.md               # Complete Blazor example walkthrough
│   ├── wpf-app.md                  # WPF application example
│   └── common-patterns.md          # Common usage patterns and recipes
│
├── comparison/
│   ├── overview.md                 # Framework comparison introduction
│   ├── vs-csla.md                  # RemoteFactory vs CSLA comparison
│   ├── vs-dtos.md                  # RemoteFactory vs manual DTO approach
│   └── decision-guide.md           # When to use RemoteFactory
│
└── advanced/
    ├── factory-lifecycle.md        # IFactoryOnStart, IFactoryOnComplete hooks
    ├── interface-factories.md      # [Factory] on interfaces
    ├── static-execute.md           # Static class Execute operations
    ├── json-serialization.md       # Custom serialization with NeatooJsonSerializer
    └── extending-factory-core.md   # Custom IFactoryCore implementations
```

---

## 2. Document Outlines

### 2.1 Landing Page: `index.md`

**Purpose:** First impression; quickly communicate value proposition

**Outline:**
1. **What is RemoteFactory?** (2-3 sentences)
   - Data Mapper Factory powered by Roslyn Source Generators
   - Enables 3-tier architecture without DTOs or manual factory code
   - Works with Blazor, WPF, and any .NET client

2. **Key Benefits** (bullet list)
   - Zero DTOs required between UI and application layer
   - Single API endpoint for all operations
   - Generated factories with full DI support
   - Built-in authorization integration
   - Compile-time code generation (no runtime reflection)

3. **Quick Code Example**
   - Show PersonModel class with attributes
   - Show generated IPersonModelFactory interface
   - Show 3-line setup code

4. **Navigation to Key Sections**
   - Getting Started
   - Concepts
   - Examples
   - API Reference

---

### 2.2 Getting Started Section

#### `getting-started/installation.md`

**Purpose:** Get the NuGet package installed and basic project configured

**Outline:**
1. **Prerequisites**
   - .NET 8 or 9
   - Visual Studio 2022 / Rider / VS Code

2. **NuGet Package Installation**
   - `Neatoo.RemoteFactory` for domain/client projects
   - `Neatoo.RemoteFactory.AspNetCore` for server projects

3. **Project Configuration**
   - Enable nullable reference types (recommended)
   - Enable partial classes for mappers

4. **Verify Installation**
   - Create simple [Factory] class
   - Confirm generated code appears

---

#### `getting-started/quick-start.md`

**Purpose:** Working example in 5 minutes

**Outline:**
1. **Create Domain Model**
   - Add [Factory] attribute
   - Add [Create] and [Fetch] methods

2. **Configure Server**
   - `AddNeatooAspNetCore()` registration
   - `UseNeatoo()` middleware

3. **Configure Client**
   - `AddNeatooRemoteFactory(NeatooFactory.Remote)`
   - Register HttpClient

4. **Use the Factory**
   - Inject IPersonModelFactory
   - Call Create() and Fetch()

5. **What Just Happened?**
   - Explanation of generated code
   - Link to architecture overview

---

#### `getting-started/project-structure.md`

**Purpose:** Recommended project organization

**Outline:**
1. **Recommended Solution Structure**
   - DomainModel project (shared)
   - Server project (ASP.NET Core)
   - Client project (Blazor/WPF)

2. **Assembly Registration**
   - Why assemblies must be registered
   - Multiple assembly support

3. **Dependency Management**
   - Which projects reference RemoteFactory
   - Server vs Client dependencies

---

### 2.3 Concepts Section

#### `concepts/architecture-overview.md`

**Purpose:** Mental model for how RemoteFactory works

**Outline:**
1. **The Problem RemoteFactory Solves**
   - Traditional 3-tier architecture requires DTOs
   - Manual factory code is repetitive
   - Authorization logic scattered

2. **RemoteFactory Architecture**
   - Diagram: Client -> Factory -> HTTP -> Server -> Delegate -> Domain Method
   - Explain local vs remote execution paths

3. **Key Components**
   - Source Generators (compile-time)
   - Factory classes (generated)
   - Delegates (for remote invocation)
   - JSON serialization (automatic)

4. **Data Flow**
   - Create/Fetch: Client -> Server -> Database -> Client
   - Save: Client -> Server (Insert/Update/Delete) -> Client

---

#### `concepts/factory-pattern.md`

**Purpose:** Explain the Factory pattern as implemented

**Outline:**
1. **What is a Factory in RemoteFactory?**
   - Generated interface (IPersonModelFactory)
   - Generated implementation (PersonModelFactory)
   - Automatic DI registration

2. **Factory Interface Anatomy**
   - Create methods (synchronous)
   - Fetch methods (async)
   - Save method (combines Insert/Update/Delete)
   - TrySave for authorization-aware saves
   - Can* methods for authorization checking

3. **How Factories Are Generated**
   - From [Factory] attribute
   - From operation methods ([Create], [Fetch], etc.)
   - Signature matching for overloads

---

#### `concepts/three-tier-execution.md`

**Purpose:** Explain Remote, Server, and Logical modes

**Outline:**
1. **NeatooFactory Enum**
   - `Server`: Server-side execution (delegates registered)
   - `Remote`: Client-side with remote calls
   - `Logical`: Client-side local execution (no HTTP)

2. **How Execution Mode Affects Behavior**
   - Property assignment (Local* vs Remote* methods)
   - Service resolution differences
   - Serialization behavior

3. **When to Use Each Mode**
   - Server: ASP.NET Core applications
   - Remote: Blazor WASM, WPF clients
   - Logical: Unit testing, single-tier apps

---

#### `concepts/factory-operations.md`

**Purpose:** Deep dive into each operation type

**Outline:**
1. **Read Operations**
   - [Create]: Construct new instance
   - [Fetch]: Load existing data
   - Constructor vs method attribution

2. **Write Operations**
   - [Insert]: New record to database
   - [Update]: Modify existing record
   - [Delete]: Remove record
   - Combined operations (Upsert pattern)

3. **Execute Operations**
   - [Execute] for static classes
   - Interface factories
   - Remote method invocation

4. **The [Remote] Attribute**
   - Marking methods for remote execution
   - Local-only vs remote-capable methods

---

#### `concepts/service-injection.md`

**Purpose:** Using [Service] for dependency injection

**Outline:**
1. **The [Service] Attribute**
   - Marking parameters for DI resolution
   - Works with any registered service

2. **Service Lifetime Considerations**
   - Scoped services per request
   - Transient services

3. **Server-Only Services**
   - Services not available on client
   - Pattern for database contexts

4. **Examples**
   - Injecting IPersonContext
   - Injecting custom services

---

### 2.4 Authorization Section

#### `authorization/authorization-overview.md`

**Purpose:** Introduce authorization concepts

**Outline:**
1. **Two Authorization Approaches**
   - Custom authorization rules ([AuthorizeFactory<T>])
   - ASP.NET Core integration ([AspAuthorize])

2. **Authorization Flow**
   - Checked before operation execution
   - Authorized result type

3. **Choosing an Approach**
   - Custom for business rules
   - AspAuthorize for policy-based

---

#### `authorization/custom-authorization.md`

**Purpose:** [AuthorizeFactory<T>] detailed guide

**Outline:**
1. **Creating an Authorization Class**
   - Interface definition
   - Implementation with IUser dependency

2. **Authorization Methods**
   - [AuthorizeFactory(AuthorizeFactoryOperation.Read | Write)]
   - Operation flags explained

3. **Generated Can* Methods**
   - CanCreate(), CanFetch(), CanUpdate(), etc.
   - Using in UI for conditional rendering

4. **Complete Example**
   - PersonModelAuth walkthrough
   - Role-based authorization

---

#### `authorization/asp-authorize.md`

**Purpose:** ASP.NET Core policy integration

**Outline:**
1. **Using [AspAuthorize]**
   - Policy-based authorization
   - Roles-based authorization

2. **Configuration**
   - Policy registration
   - AddNeatooAspNetCore includes IAspAuthorize

3. **Behavior**
   - Forbid vs return Authorized result
   - Error handling

---

#### `authorization/can-methods.md`

**Purpose:** Using generated authorization check methods

**Outline:**
1. **Generated Can Methods**
   - CanCreate, CanFetch, CanUpdate, CanDelete, CanSave

2. **Return Type: Authorized**
   - HasAccess property
   - Message property

3. **UI Integration**
   - Blazor conditional rendering
   - Disabling buttons based on authorization

---

### 2.5 Source Generation Section

#### `source-generation/how-it-works.md`

**Purpose:** High-level understanding for most developers

**Outline:**
1. **What Gets Generated?**
   - Factory interface (IPersonModelFactory)
   - Factory implementation (PersonModelFactory)
   - DI registrations (FactoryServiceRegistrar)

2. **Where to Find Generated Code**
   - Generated folder under project
   - View in Solution Explorer

3. **Triggering Regeneration**
   - Automatic on build
   - Manual via Rebuild

4. **Common Issues**
   - Class must be non-abstract, non-generic
   - Correct attribute usage

---

#### `source-generation/factory-generator.md`

**Purpose:** Factory generation specifics

**Outline:**
1. **Input: Your Domain Class**
   - [Factory] attribute required
   - Operation methods with attributes

2. **Output: Factory Components**
   - Interface with operation methods
   - Delegate types for remote calls
   - Local* and Remote* method implementations

3. **Method Generation Rules**
   - Return types (nullable, Task, bool)
   - Parameter handling
   - Service injection

---

#### `source-generation/appendix-internals.md`

**Purpose:** Technical deep dive for curious engineers

**Outline:**
1. **Roslyn IIncrementalGenerator**
   - Predicate (filtering)
   - Transform (data extraction)
   - Output (code generation)

2. **Caching Strategy**
   - Equatable records for caching
   - Performance considerations

3. **Code Generation Flow**
   - ClassDeclarationSyntax analysis
   - Symbol resolution
   - StringBuilder output

4. **Debugging Source Generators**
   - Attaching debugger
   - Diagnostic output

---

### 2.6 Reference Section

#### `reference/attributes.md`

**Purpose:** Complete attribute reference

**Outline:**
1. **Class-Level Attributes**
   - [Factory] - Marks class for factory generation
   - [SuppressFactory] - Prevents factory generation
   - [AuthorizeFactory<T>] - Links authorization class

2. **Method-Level Attributes**
   - [Create] - Constructor or create method
   - [Fetch] - Load existing data
   - [Insert] - New record
   - [Update] - Modify existing
   - [Delete] - Remove record
   - [Execute] - Static method execution
   - [Remote] - Enable remote execution
   - [AspAuthorize] - ASP.NET Core authorization
   - [AuthorizeFactory] - Authorization method marker

3. **Parameter Attributes**
   - [Service] - DI injection marker

4. **Assembly Attributes**
   - [FactoryHintNameLength] - Control generated file naming

---

#### `reference/interfaces.md`

**Purpose:** Interface reference

**Outline:**
1. **IFactorySaveMeta**
   - IsNew property
   - IsDeleted property
   - Required for Save operations

2. **IFactorySave<T>**
   - Save method signature
   - Implemented by generated factories

3. **Lifecycle Interfaces**
   - IFactoryOnStart / IFactoryOnStartAsync
   - IFactoryOnComplete / IFactoryOnCompleteAsync

4. **Authorization Interfaces**
   - IAspAuthorize

---

#### `reference/factory-modes.md`

**Purpose:** NeatooFactory enum reference

**Outline:**
1. **NeatooFactory.Server**
   - Registers delegates for remote calls
   - Resolves services locally

2. **NeatooFactory.Remote**
   - Registers IMakeRemoteDelegateRequest
   - Factory methods call remote endpoints

3. **NeatooFactory.Logical**
   - Local execution with serialization
   - Useful for testing

---

#### `reference/generated-code.md`

**Purpose:** Understanding generated factory structure

**Outline:**
1. **Generated Interface**
   - Method signatures
   - Return types

2. **Generated Implementation**
   - Constructor overloads (local/remote)
   - Delegate properties
   - Public/Local/Remote method variants

3. **DI Registration**
   - FactoryServiceRegistrar method
   - What gets registered

---

### 2.7 Examples Section

#### `examples/blazor-app.md`

**Purpose:** Complete Blazor example

**Outline:**
1. **Project Setup**
   - Three projects structure
   - NuGet references

2. **Domain Model**
   - PersonModel with full attributes
   - PersonModelAuth

3. **Server Configuration**
   - Program.cs setup
   - EF Core integration

4. **Client Configuration**
   - HttpClient setup
   - Factory injection

5. **Blazor Component**
   - Using IPersonModelFactory
   - Authorization checks
   - CRUD operations

---

#### `examples/wpf-app.md`

**Purpose:** WPF application example

**Outline:**
1. **Project Structure**
2. **Dependency Injection Setup**
3. **ViewModel Integration**
4. **Data Binding with INotifyPropertyChanged**

---

#### `examples/common-patterns.md`

**Purpose:** Reusable patterns and recipes

**Outline:**
1. **Upsert Pattern**
   - Combined [Insert][Update] method

2. **Soft Delete**
   - IsDeleted flag handling

3. **Optimistic Concurrency**
   - Version/timestamp handling

4. **Child Collections**
   - Nested factory calls

---

### 2.8 Comparison Section

#### `comparison/overview.md`

**Purpose:** Set context for comparisons

**Outline:**
1. **Why Compare?**
   - Making informed decisions
   - Understanding tradeoffs

2. **Comparison Dimensions**
   - Boilerplate reduction
   - Learning curve
   - Flexibility
   - Performance
   - Ecosystem/tooling

---

#### `comparison/vs-csla.md`

**Purpose:** Detailed CSLA comparison

**Outline:**
1. **CSLA Overview**
   - History and purpose
   - Business object framework

2. **Similarities**
   - 3-tier architecture support
   - Data portal pattern (similar to RemoteFactory)
   - Business object focus

3. **Key Differences**

   | Aspect | RemoteFactory | CSLA |
   |--------|---------------|------|
   | Code Generation | Roslyn Source Generators | Runtime reflection + some codegen |
   | Base Class Requirement | None (attribute-based) | Required (BusinessBase, etc.) |
   | DTO Generation | Not needed | Built-in serialization |
   | Learning Curve | Lower | Higher |
   | Maturity | Newer | 20+ years |
   | Documentation | Growing | Extensive |
   | Community | Emerging | Established |
   | Validation | External (DataAnnotations) | Built-in rules engine |
   | Authorization | Attribute-based | Method-based |

4. **When to Choose RemoteFactory**
   - Greenfield projects
   - Simpler requirements
   - Source generator preference
   - Minimal base class coupling

5. **When to Choose CSLA**
   - Existing CSLA investment
   - Complex business rules
   - Need extensive documentation
   - Enterprise support requirements

---

#### `comparison/vs-dtos.md`

**Purpose:** Compare to manual DTO approach

**Outline:**
1. **Traditional DTO Approach**
   - Separate DTO classes
   - AutoMapper or manual mapping
   - Service layer factories
   - Controller per entity

2. **Problems with Manual DTOs**
   - Code duplication
   - Mapping maintenance
   - Synchronization bugs
   - Boilerplate overhead

3. **RemoteFactory Advantages**

   | Aspect | Manual DTOs | RemoteFactory |
   |--------|-------------|---------------|
   | Code Volume | High | Low (generated) |
   | Type Safety | Manual | Compile-time |
   | Maintenance | Manual sync | Automatic |
   | Controller Count | Multiple | Single endpoint |
   | Mapping Code | Required | Generated |
   | Authorization | Scattered | Centralized |

4. **Code Comparison**
   - Show same feature implemented both ways
   - Line count comparison

5. **When DTOs Might Still Be Appropriate**
   - API versioning requirements
   - External API contracts
   - Existing infrastructure investment

---

#### `comparison/decision-guide.md`

**Purpose:** Help engineers choose

**Outline:**
1. **Decision Flowchart**
   - New project? -> Consider RemoteFactory
   - Existing CSLA? -> May not be worth switching
   - Simple CRUD? -> RemoteFactory shines
   - Complex rules? -> Evaluate CSLA or hybrid

2. **Project Characteristics Checklist**
   - [ ] .NET 8+ project
   - [ ] Blazor or WPF client
   - [ ] 3-tier architecture needed
   - [ ] Minimal existing infrastructure
   - [ ] Team comfortable with source generators

3. **Migration Considerations**
   - Gradual adoption possible
   - Coexistence with other patterns

---

### 2.9 Advanced Section

#### `advanced/factory-lifecycle.md`

**Purpose:** IFactoryOnStart/IFactoryOnComplete hooks

**Outline:**
1. **Lifecycle Interfaces**
   - When they are called
   - Sync vs Async variants

2. **Use Cases**
   - Audit logging
   - State initialization
   - Post-save cleanup

3. **Implementation Example**

---

#### `advanced/interface-factories.md`

**Purpose:** [Factory] on interfaces

**Outline:**
1. **When to Use Interface Factories**
   - Service abstraction
   - Server-only implementations

2. **Generation Differences**
   - No implementation class
   - Delegates registered

3. **Example**
   - IExecuteMethods interface
   - ExecuteMethods implementation

---

#### `advanced/static-execute.md`

**Purpose:** Static class with [Execute]

**Outline:**
1. **Static Factory Pattern**
   - When domain model not needed
   - Remote procedure calls

2. **Requirements**
   - Static partial class
   - Static methods with [Execute]

3. **Generated Delegates**

---

#### `advanced/json-serialization.md`

**Purpose:** Custom serialization

**Outline:**
1. **Default Serialization**
   - System.Text.Json based
   - NeatooJsonSerializer

2. **Custom Type Handling**
   - Interface serialization
   - Reference preservation

---

#### `advanced/extending-factory-core.md`

**Purpose:** Custom IFactoryCore implementations

**Outline:**
1. **Why Extend FactoryCore?**
   - Cross-cutting concerns
   - Type-specific logic

2. **Implementation Pattern**
   - Derive from FactoryCore<T>
   - Register in DI

---

## 3. README Update Plan

### Current README Issues
- Limited getting started information
- No clear value proposition
- Minimal architecture explanation

### Proposed README Structure

```markdown
# Neatoo RemoteFactory

> A Roslyn Source Generator-powered Data Mapper Factory for 3-tier .NET applications.
> Build client/server applications without writing DTOs, factories, or API controllers.

## Why RemoteFactory?

[3-4 sentences on pain points solved]

## Quick Example

[Same as current, but with comments explaining what's generated]

## Key Features

- **Zero DTOs**: Domain objects serialize directly
- **Single Endpoint**: One controller handles all operations
- **Generated Factories**: Full CRUD with dependency injection
- **Built-in Authorization**: Declarative access control
- **Source Generators**: Compile-time, no reflection

## Getting Started

[Streamlined version, link to full docs]

## Documentation

[Link to docs site]

## Framework Comparison

| Feature | RemoteFactory | CSLA | Manual DTOs |
|---------|--------------|------|-------------|
| Boilerplate | Minimal | Low | High |
| Learning Curve | Low | Medium | Low |
| 3-Tier Support | Yes | Yes | Manual |

[Link to full comparison]

## Examples

- [Blazor WASM](link)
- [Person Demo](link)

## Contributing

[Standard section]

## License

[Current license]
```

---

## 4. Key Topics by Document

### Must-Cover Topics

| Topic | Documents |
|-------|-----------|
| [Factory] attribute | quick-start, factory-pattern, attributes |
| Factory operations | factory-operations, attributes, generated-code |
| 3-tier execution | three-tier-execution, architecture-overview |
| Authorization | authorization-overview, custom-authorization, can-methods |
| Source generation | how-it-works, factory-generator |
| DI integration | service-injection, installation |
| Remote execution | three-tier-execution, factory-operations |

### Feature Deep-Dives

| Feature | Primary Document |
|---------|------------------|
| Save/TrySave | factory-operations |
| IsNew/IsDeleted | interfaces |
| CanCreate/CanFetch | can-methods |
| [Remote] attribute | factory-operations |
| NeatooFactory modes | factory-modes |
| Lifecycle hooks | factory-lifecycle |

---

## 5. Documentation Priorities

### Phase 1: Essential (Must Have)
1. `index.md` - Landing page
2. `getting-started/installation.md`
3. `getting-started/quick-start.md`
4. `concepts/architecture-overview.md`
5. `concepts/factory-operations.md`
6. `source-generation/how-it-works.md`
7. `reference/attributes.md`
8. README update

### Phase 2: Core (Should Have)
1. `concepts/three-tier-execution.md`
2. `concepts/service-injection.md`
3. `authorization/authorization-overview.md`
4. `authorization/custom-authorization.md`
5. `source-generation/factory-generator.md`
6. `reference/interfaces.md`
7. `examples/blazor-app.md`

### Phase 3: Comparison (High Value)
1. `comparison/vs-csla.md`
2. `comparison/vs-dtos.md`
3. `comparison/decision-guide.md`

### Phase 4: Advanced & Polish
1. `advanced/factory-lifecycle.md`
2. `advanced/interface-factories.md`
3. `source-generation/appendix-internals.md`
4. Remaining reference docs
5. Additional examples

---

## 6. Writing Guidelines

### Style
- **Voice**: Second person ("you"), active voice
- **Tone**: Professional, direct, helpful
- **Length**: Concise paragraphs, liberal use of code examples
- **Code**: Always include runnable examples

### Format
- Use fenced code blocks with `csharp` language identifier
- Include file paths in code block titles when relevant
- Use tables for comparisons
- Use diagrams (Mermaid) for architecture/flow

### Cross-References
- Link related documents liberally
- Reference relevant generated code examples
- Link to source files in GitHub when appropriate

---

## 7. Success Metrics

Documentation will be considered successful when:

1. **New User Success**: Engineer can have working example in <15 minutes
2. **Feature Discovery**: All features documented and findable
3. **Decision Support**: Comparison docs help framework selection
4. **Self-Service**: Common questions answered in docs
5. **Maintenance**: Easy to update as framework evolves

---

## 8. Next Steps

1. Create `docs/index.md` landing page
2. Write Phase 1 essential documents
3. Update README.md
4. Review and iterate based on feedback
5. Continue with Phase 2-4 documents
