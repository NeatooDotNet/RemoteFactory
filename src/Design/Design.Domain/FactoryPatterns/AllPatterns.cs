// =============================================================================
// DESIGN SOURCE OF TRUTH: Factory Patterns Overview
// =============================================================================
//
// This file demonstrates all three factory patterns available in RemoteFactory.
// Use this as a reference when deciding which pattern fits your use case.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryPatterns;

// =============================================================================
// PATTERN 1: CLASS FACTORY
// =============================================================================
//
// Use [Factory] on a class to generate a factory that creates and manages instances.
// This is the most common pattern for domain entities and aggregate roots.
//
// GENERATOR BEHAVIOR: For a class with [Factory], the generator creates:
//   1. An interface: IExampleClassFactory
//   2. A factory implementation with methods for each [Create], [Fetch], etc.
//   3. Delegate types for each factory method
//   4. DI registrations for both local and remote execution
//
// DESIGN DECISION: Class factories own their instances. The factory creates
// new instances and manages their lifecycle through Create, Fetch, and Save.
// This matches DDD aggregate patterns where the factory is the entry point.
//
// =============================================================================

/// <summary>
/// Demonstrates: CLASS FACTORY pattern.
///
/// Key points:
/// - [Factory] on a class generates IExampleClassFactoryFactory
/// - Classes with [Factory] must be partial (generator adds partial class)
/// - [Remote] marks client-to-server entry points
/// - [Create] generates a factory method that instantiates and initializes
/// - [Fetch] generates a factory method that loads existing state
/// - Method injection ([Service] on parameters) = resolved on the tier that runs the method
/// </summary>
/// <remarks>
/// COMMON MISTAKE: Forgetting the 'partial' keyword
///
/// WRONG:
/// [Factory]
/// public class MyEntity { }  // <-- Missing partial
///
/// RIGHT:
/// [Factory]
/// public partial class MyEntity { }
///
/// The generator creates a partial class to add the IOrdinalSerializable
/// interface implementation. Without 'partial', the code won't compile.
/// </remarks>
[Factory]
public partial class ExampleClassFactory
{
    // -------------------------------------------------------------------------
    // DESIGN DECISION: Properties need public setters for serialization
    //
    // The generated IOrdinalSerializable implementation uses property setters
    // to restore state after deserialization. Private setters won't work
    // because the generated code is in a partial class, not a derived class.
    //
    // DID NOT DO THIS: Support private setters via reflection
    //
    // Reasons:
    // 1. Reflection is slow and incompatible with IL trimming
    // 2. Source generation requires compile-time accessible setters
    // 3. Explicit public setters make serialization behavior obvious
    //
    // COMMON MISTAKE: Using private setters
    //
    // WRONG:
    // public int Id { get; private set; }  // <-- Won't serialize properly
    //
    // RIGHT:
    // public int Id { get; set; }  // Public setter for serialization
    // -------------------------------------------------------------------------
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // -------------------------------------------------------------------------
    // DESIGN DECISION: Constructor injection vs method injection
    //
    // Constructor injection: DI fills the constructor wherever the object is
    // built, on BOTH client and server. Use sparingly - only for services
    // genuinely needed on both sides.
    //
    // Method injection ([Service] on method parameters): resolved from the
    // container of the tier that runs the method, so for a [Remote] method the
    // server's. This is the common case: most services (repositories, external
    // APIs, etc.) exist only on the server and are reached through [Remote].
    //
    // SERIALIZATION INTERACTION: see CtorInjectionExample.cs
    //
    // Constructor shape changes which deserialization path runs. A class whose
    // only ctors require non-default arguments cannot be built via object
    // initializer, so the generator SKIPS IOrdinalSerializable for it. The type
    // still serializes -- it routes through the named JSON path, which resolves
    // the instance via IServiceProvider.GetRequiredService and lets DI fill the
    // ctor parameters on each side of the wire.
    //
    // ExampleClassFactory has a parameterless ctor (below) and so uses ordinal.
    // CtorInjectedEntity in CtorInjectionExample.cs requires DI in its ctor and
    // so goes through the named/DI path. The choice is shape-based -- the
    // generator's RequiresServiceInstantiationCheck does not consult [Service].
    // -------------------------------------------------------------------------

    public ExampleClassFactory() { }

    /// <summary>
    /// Entry point from client. Creates a new instance on the server.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: This generates:
    /// - Delegate: Create_ExampleClassFactory_Create(...)
    /// - Factory method: IExampleClassFactory.Create(string name)
    /// - Remote stub: Serializes call to server, deserializes response
    /// </remarks>
    [Remote, Create]
    internal Task Create(string name, [Service] IExampleService service)
    {
        // COMMON MISTAKE: Calling service methods in Create without [Service]
        //
        // WRONG:
        // public Task Create(string name, IExampleService service) // Missing [Service]
        //
        // The generator won't recognize this as a service to inject.
        // Always use [Service] attribute on service parameters.

        Id = service.GenerateId();
        Name = name;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Entry point from client. Fetches an existing instance from the server.
    /// </summary>
    [Remote, Fetch]
    internal Task Fetch(int id, [Service] IExampleService service)
    {
        var data = service.LoadData(id);
        Id = data.Id;
        Name = data.Name;
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // DID NOT DO THIS: Make all methods [Remote] by default
    //
    // Reasons:
    // 1. Explicit is better - developers should consciously decide boundaries
    // 2. Performance - unnecessary remote calls are expensive
    // 3. Security - accidental exposure of internal methods
    //
    // The rule: Only aggregate root entry points need [Remote]. Once on the
    // server, subsequent calls stay server-side.
    // -------------------------------------------------------------------------
}

// =============================================================================
// PATTERN 2: INTERFACE FACTORY (Remote Service Proxy)
// =============================================================================
//
// Use [Factory] on an interface to create a remote proxy. The implementation
// lives only on the server; clients call through the generated proxy.
//
// GENERATOR BEHAVIOR: For an interface with [Factory], the generator creates:
//   1. A proxy implementation that routes calls to the server
//   2. Delegate types for each method
//   3. DI registrations for remote execution
//
// DESIGN DECISION: Interface factories enable clean separation between
// client and server. The interface defines the contract; the server provides
// the implementation. Clients only see the interface.
//
// DESIGN DECISION: An interface factory is a DOMAIN SERVICE the client calls
//
// Name it for what it does (a catalog, a directory, a pricing service). A
// repository is never an interface factory: persistence is a [Service]
// injected into server-side operations, as Order injects IOrderDbContext
// (VisionAlignment D12, #112). Exposing a repository to the client would
// let the UI read and write rows that no aggregate guards.
//
// =============================================================================

/// <summary>
/// Demonstrates: INTERFACE FACTORY pattern (remote service proxy).
///
/// Key points:
/// - [Factory] on interface generates a remote proxy
/// - NO operation attributes on methods - all methods are implicitly remote
/// - Server provides the implementation (registered via convention or explicit)
/// - Use RegisterMatchingName to auto-register IFoo → Foo
/// </summary>
/// <remarks>
/// DESIGN DECISION: Interface methods don't need any attributes
///
/// DID NOT DO THIS: Require [Fetch], [Execute], etc. on interface methods
///
/// Reasons:
/// 1. The interface IS the remote boundary - every method crosses it
/// 2. No need to distinguish between Create/Fetch/etc. - the interface
///    just defines the contract, server implementation handles semantics
/// 3. Keeps interfaces clean and focused on the contract
///
/// COMMON MISTAKE: Adding operation attributes to interface methods
///
/// WRONG:
/// [Factory]
/// public interface IMyService {
///     [Fetch]  // <-- NF0106: factory-operation attribute on interface factory method
///     Task<Data> GetData();
/// }
///
/// RIGHT:
/// [Factory]
/// public interface IMyService {
///     Task<Data> GetData();  // No attribute needed
/// }
///
/// Enforced at compile time by NF0106 — see CLAUDE-DESIGN.md Anti-Pattern 2.
/// </remarks>
#region interface-factory-contract
[Factory]
public interface IProductCatalog
{
    /// <summary>
    /// Products whose name contains the text. Executed on the server, called from the client.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: The proxy serializes this call to the server,
    /// where the actual ProductCatalog implementation runs.
    /// No attributes needed - the [Factory] on the interface is sufficient.
    /// </remarks>
    Task<IReadOnlyList<ProductListing>> SearchAsync(string text);

    /// <summary>
    /// One product by SKU, or null when there is none.
    /// </summary>
    Task<ProductListing?> FindBySkuAsync(string sku);

    /// <summary>
    /// A product's details, returned as a record with a primary constructor.
    /// </summary>
    /// <remarks>
    /// Demonstrates that Interface Factory methods can return plain records
    /// (no [Factory] attribute). The record is serialized without $id/$ref
    /// metadata, avoiding the System.Text.Json parameterized constructor limitation.
    /// ProductListing (a class) and ProductDetails (a record) take the two
    /// serialization paths a returned type can take.
    /// </remarks>
    Task<ProductDetails?> GetDetailsAsync(string sku);
}
#endregion

// DID NOT DO THIS: Require [Remote] on interface methods
//
// Reasons:
// 1. Redundant - the interface itself is the remote boundary
// 2. Noise - every method would need the same attribute
// 3. Consistency - interface factories are always fully remote
//
// The rule: [Factory] on interface = all methods are remote entry points.

/// <summary>
/// Server-side implementation of IProductCatalog.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Server implementations don't need [Factory] because they're
/// not exposed to clients. The interface defines the factory contract.
///
/// It is named for the interface without its leading I and sits in the same
/// namespace, so RegisterMatchingName registers it (IProductCatalog -> ProductCatalog).
///
/// COMMON MISTAKE: Adding [Factory] to the implementation class
///
/// WRONG:
/// [Factory]  // <-- Unnecessary and causes duplicate registration
/// public class ProductCatalog : IProductCatalog { }
///
/// RIGHT:
/// public class ProductCatalog : IProductCatalog { }  // No [Factory]
///
/// The interface already has [Factory]; the implementation is just a service.
/// </remarks>
#region interface-factory-implementation
public class ProductCatalog : IProductCatalog
{
    // In real code, this would query a product store
    private static readonly ProductDetails[] Products =
    [
        new("WA-100", "Widget A", 10.00m, "The standard widget."),
        new("WB-200", "Widget B", 25.00m, "The heavy-duty widget."),
        new("WC-300", "Widget C", 7.00m, "The compact widget."),
    ];

    public Task<IReadOnlyList<ProductListing>> SearchAsync(string text)
    {
        IReadOnlyList<ProductListing> found = Products
            .Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Select(ToListing)
            .ToList();
        return Task.FromResult(found);
    }

    public Task<ProductListing?> FindBySkuAsync(string sku)
    {
        var product = Products.FirstOrDefault(p => p.Sku == sku);
        return Task.FromResult(product == null ? null : ToListing(product));
    }

    public Task<ProductDetails?> GetDetailsAsync(string sku)
    {
        return Task.FromResult(Products.FirstOrDefault(p => p.Sku == sku));
    }

    private static ProductListing ToListing(ProductDetails product) =>
        new() { Sku = product.Sku, Name = product.Name, UnitPrice = product.UnitPrice };
}
#endregion

// =============================================================================
// PATTERN 3: STATIC FACTORY (Execute Commands)
// =============================================================================
//
// Use [Factory] on a static class to generate command handlers.
// Use [Execute] for request-response commands.
//
// GENERATOR BEHAVIOR: For a static class with [Factory], the generator creates:
//   1. Delegate types for each [Execute] method
//   2. DI registrations to resolve the delegates
//
// DESIGN DECISION: Static factories enable stateless command patterns.
// No instance management needed - just call the method with parameters.
//
// =============================================================================

/// <summary>
/// Demonstrates: STATIC FACTORY pattern with [Execute] commands.
///
/// Key points:
/// - [Factory] on static class enables command pattern
/// - Static classes must be partial (generator adds to them)
/// - [Execute] methods are request-response (await the result)
/// - Inject services via [Service] parameters
/// - No instance state - pure functions with side effects via services
/// - [Remote] decides where a command runs: _SendNotification crosses to the
///   server, _ScoreText runs on whichever tier resolves it
/// </summary>
/// <remarks>
/// COMMON MISTAKE: Forgetting 'partial' on static factory classes
///
/// WRONG:
/// [Factory]
/// public static class Commands { }  // <-- Missing partial
///
/// RIGHT:
/// [Factory]
/// public static partial class Commands { }
///
/// The generator adds delegate types and registration methods to the
/// static class, which requires the partial modifier.
/// </remarks>
[Factory]
public static partial class ExampleCommands
{
    /// <summary>
    /// Sends a notification. Returns success status.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Static factory methods are private with underscore prefix
    ///
    /// The generator creates a public method/delegate without the underscore.
    /// This pattern:
    /// 1. Keeps implementation details private
    /// 2. Generated public API is clean (no underscore)
    /// 3. Services are injected by the generated code, not passed by caller
    ///
    /// GENERATOR BEHAVIOR: For this method, the generator creates:
    ///   - Delegate: Execute_ExampleCommands_SendNotification(string, string)
    ///   - Public method: ExampleCommands.SendNotification(string, string)
    ///   - A forwarding holder, NeatooFactoryRegistrar_ExampleCommands, which the
    ///     assembly-level [NeatooFactoryRegistrar] attribute points at
    ///
    /// TRIMMING: the holder is not incidental. The registrar attribute carries
    /// [DynamicallyAccessedMembers], which preserves every method on the type it
    /// names -- method BODIES included. Static factories have no separate generated
    /// factory class (the generator re-opens this partial to host
    /// FactoryServiceRegistrar), so before v1.7.0 the attribute named
    /// ExampleCommands itself and _SendNotification's body shipped to trimmed
    /// Blazor WASM clients, decompilable. The holder gives the attribute a
    /// single-method type to preserve instead.
    ///
    /// [Remote] decides where this runs, as on every operation: with it, the client
    /// gets a remote delegate and the server a local one guarded by IsServerRuntime;
    /// without it (see _ScoreText below) one unguarded local delegate is registered
    /// in every mode. Static factories stay exempt from the NF0105 [Remote] public check.
    ///
    /// So [Remote] is what makes the body trimmable, by deciding whether a guard is
    /// emitted at all. The guard is the mechanism; the attribute is the cause. That
    /// is measured rather than reasoned: RemoteFactory.TrimmingTests carries both
    /// halves of this pair on this same shape -- same class, registrar and holder,
    /// controlled for async-ness, with the attribute the difference under test --
    /// and the CI gate asserts _DoWork's body ABSENT from a publish-trimmed client
    /// while the bare sibling's body is PRESENT and still runs there. An earlier version of this note had it the other way round
    /// ("the guard, not the attribute"), which was true only while [Execute] was
    /// welded to remote and every delegate got a guard regardless.
    ///
    /// Not demonstrated by a Design test: preservation and over-preservation are
    /// only observable in a publish-trimmed artifact, and Design.Tests run
    /// untrimmed. RemoteFactory.TrimmingTests is the verification surface.
    ///
    /// Usage from client:
    ///   var success = await ExampleCommands.SendNotification("recipient@example.com", "Hello!");
    ///
    /// COMMON MISTAKE: Making the method public
    ///
    /// WRONG:
    /// [Execute]
    /// public static Task SendNotification(...) // <-- Public conflicts with generated
    ///
    /// RIGHT:
    /// [Execute]
    /// private static Task&lt;bool&gt; _SendNotification(...) // Private with underscore
    ///
    /// DID NOT DO THIS: Allow void-returning [Execute] methods
    ///
    /// Reasons:
    /// 1. A remote call needs a response to confirm completion
    /// 2. The client needs something to await
    ///
    /// The rule: [Execute] returns Task or Task&lt;T&gt;, never void. Task&lt;T&gt; when
    /// the caller needs a value; Task when it does not (see _Notify below). Awaiting
    /// either confirms completion and rethrows a server exception.
    /// </remarks>
    [Remote, Execute]
    private static async Task<bool> _SendNotification(string recipient, string message, [Service] INotificationService service)
    {
        await service.SendAsync(recipient, message);
        return true;
    }

    /// <summary>
    /// Sends a notification. A command with no result.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: A command with nothing to report returns Task
    ///
    /// The caller awaits it exactly as it awaits Task&lt;T&gt;: the await returns when
    /// the server method has completed, and a server exception rethrows at the await.
    /// No result is serialized. Do not return Task&lt;bool&gt; just to have something
    /// to return; return Task&lt;T&gt; when the caller needs the value.
    ///
    /// GENERATOR BEHAVIOR: The delegate is declared Task (ExampleCommands.Notify), and
    /// the client's remote registration round-trips with no result. Before #113 a
    /// Task-returning [Execute] generated a Task&lt;Task&gt; delegate that did not compile.
    /// </remarks>
    [Remote, Execute]
    private static Task _Notify(string recipient, string message, [Service] INotificationService service)
        => service.SendAsync(recipient, message);

    /// <summary>
    /// Scores text on the caller's tier. Returns the word count.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: [Execute] without [Remote] is a local command
    ///
    /// [Remote] decides where an [Execute] runs, exactly as it does for every other
    /// operation. Without it the generator emits ONE unguarded local registration in
    /// every factory mode and no remote delegate, no endpoint: the delegate runs on
    /// whichever tier resolves it, and its [Service] parameters come from that tier's
    /// container. On a Blazor WASM client that means the body ships to the browser
    /// and runs there -- the shape for client-side engines that must not round-trip.
    ///
    /// GENERATOR BEHAVIOR: For this method, the generator creates:
    ///   - Delegate: ExampleCommands.ScoreText(string)
    ///   - One local registration per mode (Server, Remote, Logical), none guarded
    ///   - No remote registration; the server refuses a crafted request naming it
    ///     exactly as it refuses an unknown delegate
    ///
    /// The service must exist where the call runs. ITextScorer is pure computation
    /// and is registered on every tier: RegisterMatchingName maps it in Design.Tests
    /// and Design.Server, and Design.Client.Blazor registers it explicitly. A bare
    /// [Execute] taking a service the client does not register compiles, then fails
    /// at call time on the client with a DI resolution error -- see LocalExecuteTests.
    ///
    /// COMMON MISTAKE: Omitting [Remote] on a command that needs the server
    ///
    /// WRONG:
    /// [Execute]
    /// private static Task&lt;bool&gt; _SendNotification(..., [Service] INotificationService service)
    /// // Runs on the client; a client that does not register INotificationService
    /// // (Design.Client.Blazor registers no server-side services) throws at call time
    ///
    /// RIGHT:
    /// [Remote, Execute]
    /// private static Task&lt;bool&gt; _SendNotification(...)
    ///
    /// The rule: [Remote] when the command needs the server; bare when it must run
    /// where it is called.
    /// </remarks>
    #region attributes-execute-local
    [Execute]
    private static Task<int> _ScoreText(string text, [Service] ITextScorer scorer)
    {
        return Task.FromResult(scorer.Score(text));
    }
    #endregion

    // -------------------------------------------------------------------------
    // DID NOT DO THIS: Use instance methods for stateless operations
    //
    // Reasons:
    // 1. Static methods are clearer for pure operations
    // 2. No instance lifecycle to manage
    // 3. Easier to test - just call the method with mock services
    //
    // The rule: If your operation doesn't need instance state, prefer static.
    // -------------------------------------------------------------------------
}

// =============================================================================
// SUPPORTING TYPES
// =============================================================================

/// <summary>
/// Example service interface for demonstrating service injection.
/// </summary>
public interface IExampleService
{
    int GenerateId();
    (int Id, string Name) LoadData(int id);
}

/// <summary>
/// Example service implementation (server-only).
/// </summary>
public class ExampleService : IExampleService
{
    private int _nextId = 1;

    public int GenerateId() => _nextId++;
    public (int Id, string Name) LoadData(int id) => (id, $"Loaded_{id}");
}

/// <summary>
/// Client-safe service for the local [Execute] samples: pure computation, no I/O.
/// </summary>
/// <remarks>
/// DESIGN DECISION: A service a bare [Execute] takes must be registered on every
/// tier that calls the command. This one is, on purpose. IExampleService and
/// INotificationService are server-side and stay out of Design.Client.Blazor.
/// </remarks>
public interface ITextScorer
{
    int Score(string text);
}

/// <summary>
/// Word count. Registered explicitly on the Blazor client
/// (Design.Client.Blazor/Program.cs) and mapped by RegisterMatchingName elsewhere.
/// </summary>
public class TextScorer : ITextScorer
{
    public int Score(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}

/// <summary>
/// Example notification service interface.
/// </summary>
public interface INotificationService
{
    Task SendAsync(string recipient, string message);
}

/// <summary>
/// Example notification service implementation.
/// </summary>
public class NotificationService : INotificationService
{
    public Task SendAsync(string recipient, string message)
    {
        // In real code, this would send an email, SMS, etc.
        Console.WriteLine($"Notification to {recipient}: {message}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// A product as a search result: a class with public setters and a parameterless
/// constructor.
/// </summary>
/// <remarks>
/// IL trimming: the generator emits DtoConstructorRegistry.Register&lt;ProductListing&gt;
/// for a returned class with a parameterless constructor. No consumer action needed.
/// </remarks>
public class ProductListing
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Record DTO for data transfer (primary constructor, no [Factory] attribute).
/// </summary>
/// <remarks>
/// Plain records work as Interface Factory return types. They are serialized
/// without $id/$ref metadata, so parameterized constructors are not blocked
/// by System.Text.Json's reference handling limitation.
///
/// IL trimming: because this record has no parameterless constructor, the
/// generator emits DtoConstructorRegistry.PreserveType&lt;ProductDetails&gt;()
/// in the FactoryServiceRegistrar (plain DTOs with a parameterless ctor get
/// Register&lt;T&gt;(() => new T()) instead). No consumer action needed.
///
/// Do NOT add [Factory] here -- that would route serialization through the
/// ordinal converter. Plain records fall through to standard STJ handling.
/// </remarks>
public record ProductDetails(string Sku, string Name, decimal UnitPrice, string Description);
