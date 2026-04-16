---
name: RemoteFactory
description: |
  This skill should be used when the user mentions "RemoteFactory", "Neatoo.RemoteFactory", "[Factory] attribute", "[Remote] attribute", "[Execute] attribute", "[FactoryEventHandler<T>]", "FactoryEventBase", "IFactoryEvents", "IFactoryEventRelay", "factory event relay", "ServerOnly", "RaiseOptions", "IFactorySaveMeta", "[AspAuthorize]", "[AuthorizeFactory]", "Save routing", "domain events", "client-server factory", "IL trimming", "bundle size", "PublishTrimmed", "NeatooRuntime", "ViewModel factory", "LazyLoad", "ILazyLoadFactory", "deferred loading", "lazy loading", or asks about factory patterns for 3-tier .NET applications. RemoteFactory works with any .NET class — Neatoo entities, ViewModels, POCOs, or static commands. It does NOT require Neatoo base classes. Provides guidance for building enterprise line-of-business applications using RemoteFactory's source-generated factory patterns.
version: 1.0.0
---

# RemoteFactory Development Guide

RemoteFactory is a Roslyn Source Generator-powered factory for 3-tier .NET applications. It generates factories, handles serialization, and creates ASP.NET Core endpoints at compile time — eliminating DTOs, manual factories, and API controllers.

**RemoteFactory works with any .NET class.** It does not require Neatoo base classes. Use it with Neatoo entities for full DDD persistence, or with plain ViewModels, POCOs, and static classes for client-server communication without a domain model.

## Works With Any Class

RemoteFactory generates factories for whatever class has `[Factory]`:

```csharp
// ViewModel — no Neatoo base class, just a plain class
[Factory]
public partial class DashboardViewModel
{
    public partial List<OrderSummary> RecentOrders { get; set; }
    public partial CustomerStats Stats { get; set; }

    [Fetch][Remote]
    internal void Fetch([Service] IOrderRepo orders, [Service] IStatsService stats)
    {
        RecentOrders = orders.GetRecent();
        Stats = stats.GetForDashboard();
    }
}

// Neatoo entity — same factory attributes, adds change tracking + validation
[Factory]
public partial class Order : EntityBase<Order>
{
    public Order(IEntityBaseServices<Order> services) : base(services) { }
    [Create] public void Create() { }
}
```

Both generate an `IXxxFactory` with the appropriate methods. The factory pattern is the same whether the target class inherits from `EntityBase<T>`, `ValidateBase<T>`, or nothing at all.

## What RemoteFactory Does

- **Generates factory interfaces and implementations** from attributed classes
- **Handles serialization automatically** — objects cross client/server boundary without DTOs
- **Generates ASP.NET Core endpoints** — no manual controller code
- **Supports three factory patterns** for different use cases

## The Three Factory Patterns

| Pattern | Use When | Example | Reference |
|---------|----------|---------|-----------|
| **Class Factory** | Aggregate roots with lifecycle | `Order`, `Customer` | `references/class-factory.md` |
| **Interface Factory** | Remote services without entity identity | `IOrderRepository` | `references/interface-factory.md` |
| **Static Factory** | Stateless commands | `EmailCommands` | `references/static-factory.md` |

## Quick Decisions Reference

| Question | Answer |
|----------|--------|
| Should this method be [Remote]? | Only aggregate root entry points |
| Must [Remote] methods be `internal`? | Yes - `[Remote] public` is error NF0105; `[Remote] internal` promotes to `public` on factory interface |
| Can I use private setters? | No - breaks serialization |
| Should interface methods have attributes? | No - interface IS the boundary |
| Do I need `partial` keyword? | Yes, always |
| Should child entities have [Remote]? | No - causes N+1 remote calls |
| Should child entity methods be `internal`? | Yes - server-only, trimmable, invisible to client |
| Can [Execute] return void? | No, must return Task<T> |
| Can [Execute] go on a class factory? | Yes, if `public static` and returns containing type |
| How do I handle a factory event on the server? | `[FactoryEventHandler<T>]` class with a `static` matching method — runs in the caller's scope (shared DbContext/transaction), sequentially, awaited |
| How do I handle a factory event on the client? | Implement `IFactoryEventRelay` and register it in DI — RemoteFactory invokes `Relay(IReadOnlyList<FactoryEventBase>)` once per `[Remote]` call |
| Does `[FactoryEventHandler<T>]` need `[Factory]`? | No — separate generator pipeline |
| I want a handler that participates in the factory's DB transaction | Use `[FactoryEventHandler<T>]` + `IFactoryEvents.Raise` — shared scope, sequential, exceptions propagate and roll back |
| I want to fire-and-forget external IO (email, webhook, queue) inside a factory method | Call `Task.Run` + `IServiceScopeFactory.CreateScope()` directly. RemoteFactory does not own this — snapshot any ambient context (correlation ID, tenant) before the `Task.Run` body and re-assign inside the child scope. |
| How do I stop an event from relaying to the client? | Pass `RaiseOptions.ServerOnly` to `IFactoryEvents.Raise` |
| Where must factory events be raised? | Inside a factory method via `[Service] IFactoryEvents` |
| Where does business logic go? | In the entity, not the factory |
| Can multiple types share a generic base? | Yes — use CRTP constraint (`where T : MyBase<T>`), `[Factory]` on base |
| How do I reduce Blazor WASM bundle size? | Enable IL trimming (strongly recommended) |
| How do I defer loading of related data? | Use `LazyLoad<T>` with `ILazyLoadFactory` |
| Can I use BCL `Lazy<T>`? | No — use `LazyLoad<T>` (has serialization support) |

## Reference Files

Consult these files for detailed patterns and examples:

### Core Patterns
- **`references/class-factory.md`** - Aggregate roots, IFactorySaveMeta, lifecycle hooks
- **`references/interface-factory.md`** - Remote service proxies
- **`references/static-factory.md`** - Execute commands on static factory classes
- **`references/factory-events.md`** - `[FactoryEventHandler<T>]` mediator + server-to-client relay, `IFactoryEvents.Raise`, `RaiseOptions.ServerOnly`, `IFactoryEventRelay`

### Implementation Details
- **`references/lazyload.md`** - Deferred async loading with LazyLoad&lt;T&gt;, ILazyLoadFactory
- **`references/service-injection.md`** - Constructor vs method injection, child entities
- **`references/setup.md`** - Server and client configuration
- **`references/anti-patterns.md`** - Common mistakes to avoid

### Advanced Topics
- **`references/advanced-patterns.md`** - Authorization, correlation context, complex aggregates, testing
- **`references/polymorphic-hierarchy.md`** - Generic base class hierarchy with multiple concrete types, CRTP constraint, routing pattern
- **`references/trimming.md`** - IL trimming setup (removes server code from deployed client)

---

## Code Sample Sources

This skill contains two types of code examples:

### Compiled Code (via MarkdownSnippets)
Code blocks marked with `<!-- snippet: name -->` are extracted from the reference application and are guaranteed to compile and work. These include:
- Complete class/interface examples
- Full working implementations
- Configuration samples

### Hand-Written Code
The following code blocks are intentionally hand-written and NOT extracted from compiled source:

1. **Anti-pattern examples** (`references/anti-patterns.md`)
   - All code in this file shows intentionally wrong patterns
   - Marked with `// WRONG` comments
   - Cannot come from compiled source because they demonstrate errors

2. **Partial/illustrative snippets** (various files)
   - Short excerpts showing just properties or method signatures
   - Usage examples that aren't full compilable units
   - Inline comparisons (WRONG vs RIGHT patterns)

When updating this skill, edit anti-patterns and partial examples directly in the markdown files. For compiled examples, update the reference application code and run `mdsnippets` to re-extract.
