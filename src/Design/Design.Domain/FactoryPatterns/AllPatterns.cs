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
/// - Method injection ([Service] on parameters) = server-only
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
    // 1. Reflection is slow and breaks AOT compilation
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
    // Constructor injection ([Service] on constructor): The service is available
    // on BOTH client and server. Use sparingly - only for services genuinely
    // needed on both sides.
    //
    // Method injection ([Service] on method parameters): Server-only. This is
    // the common case. Most services (repositories, external APIs, etc.) should
    // only run on the server.
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
    public Task Create(string name, [Service] IExampleService service)
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
    public Task Fetch(int id, [Service] IExampleService service)
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
///     [Fetch]  // <-- Unnecessary, causes duplicate generation
///     Task<Data> GetData();
/// }
///
/// RIGHT:
/// [Factory]
/// public interface IMyService {
///     Task<Data> GetData();  // No attribute needed
/// }
/// </remarks>
[Factory]
public interface IExampleRepository
{
    /// <summary>
    /// Fetches all items. Executed on server, called from client.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: The proxy serializes this call to the server,
    /// where the actual IExampleRepository implementation runs.
    /// No attributes needed - the [Factory] on the interface is sufficient.
    /// </remarks>
    Task<IReadOnlyList<ExampleDto>> GetAllAsync();

    /// <summary>
    /// Fetches a single item by ID.
    /// </summary>
    Task<ExampleDto?> GetByIdAsync(int id);
}

// DID NOT DO THIS: Require [Remote] on interface methods
//
// Reasons:
// 1. Redundant - the interface itself is the remote boundary
// 2. Noise - every method would need the same attribute
// 3. Consistency - interface factories are always fully remote
//
// The rule: [Factory] on interface = all methods are remote entry points.

/// <summary>
/// Server-only implementation of IExampleRepository.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Server implementations don't need [Factory] because they're
/// not exposed to clients. The interface defines the factory contract.
///
/// COMMON MISTAKE: Adding [Factory] to the implementation class
///
/// WRONG:
/// [Factory]  // <-- Unnecessary and causes duplicate registration
/// public class ExampleRepository : IExampleRepository { }
///
/// RIGHT:
/// public class ExampleRepository : IExampleRepository { }  // No [Factory]
///
/// The interface already has [Factory]; the implementation is just a service.
/// </remarks>
public class ExampleRepository : IExampleRepository
{
    public Task<IReadOnlyList<ExampleDto>> GetAllAsync()
    {
        // In real code, this would query a database
        return Task.FromResult<IReadOnlyList<ExampleDto>>(
        [
            new ExampleDto { Id = 1, Name = "Item 1" },
            new ExampleDto { Id = 2, Name = "Item 2" }
        ]);
    }

    public Task<ExampleDto?> GetByIdAsync(int id)
    {
        return Task.FromResult<ExampleDto?>(new ExampleDto { Id = id, Name = $"Item {id}" });
    }
}

// =============================================================================
// PATTERN 3: STATIC FACTORY (Execute/Event Commands)
// =============================================================================
//
// Use [Factory] on a static class to generate command handlers.
// Use [Execute] for request-response commands, [Event] for fire-and-forget.
//
// GENERATOR BEHAVIOR: For a static class with [Factory], the generator creates:
//   1. Delegate types for each [Execute] or [Event] method
//   2. DI registrations to resolve the delegates
//   3. For [Event]: Isolated scope with fire-and-forget semantics
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
    /// 1. All remote calls need a response to confirm completion
    /// 2. The client needs something to await
    /// 3. Enables returning success/failure status or results
    ///
    /// The rule: [Execute] methods must return Task&lt;T&gt;, not just Task.
    /// </remarks>
    [Remote, Execute]
    private static async Task<bool> _SendNotification(string recipient, string message, [Service] INotificationService service)
    {
        await service.SendAsync(recipient, message);
        return true; // Return value required for Execute methods
    }

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

/// <summary>
/// Demonstrates: STATIC FACTORY pattern with [Event] handlers.
///
/// Key points:
/// - [Event] methods are fire-and-forget (don't await completion)
/// - Static classes must be partial (generator adds to them)
/// - Each event runs in an isolated DI scope
/// - CancellationToken receives ApplicationStopping for graceful shutdown
/// - Perfect for async side effects that shouldn't block the caller
/// </summary>
[Factory]
public static partial class ExampleEvents
{
    /// <summary>
    /// Handles order placed event. Runs in isolated scope, fire-and-forget.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Same as [Execute] - methods are private with underscore.
    ///
    /// GENERATOR BEHAVIOR: Events use IHostApplicationLifetime.ApplicationStopping
    /// for the CancellationToken, enabling graceful shutdown.
    ///
    /// The generated public method:
    ///   ExampleEvents.OnOrderPlaced(int orderId)
    ///
    /// Notice: CancellationToken is NOT in the public signature - it's injected
    /// from IHostApplicationLifetime.ApplicationStopping by the generator.
    ///
    /// DESIGN DECISION: Events are isolated to prevent scope pollution.
    /// Each event gets its own DI scope, so long-running operations don't
    /// hold references to the original request's scoped services.
    ///
    /// COMMON MISTAKE: Expecting to await event completion
    ///
    /// Events return Task but calling code should fire-and-forget:
    ///   _ = ExampleEvents.OnOrderPlaced(orderId);  // Discard the task
    ///
    /// Events complete asynchronously after the caller continues.
    /// </remarks>
    [Remote, Event]
    private static async Task _OnOrderPlaced(
        int orderId,
        [Service] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        // This runs in its own scope, after the caller has moved on
        await notificationService.SendAsync("admin@example.com", $"Order {orderId} placed!");
    }

    // -------------------------------------------------------------------------
    // DESIGN DECISION: Events require CancellationToken as final parameter
    //
    // Reasons:
    // 1. Graceful shutdown - events can be cancelled when app stops
    // 2. Consistency - all async operations should respect cancellation
    // 3. Resource management - prevents orphaned background tasks
    //
    // The generator enforces this: [Event] methods without CancellationToken
    // produce a compile-time diagnostic.
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
/// Simple DTO for data transfer.
/// </summary>
public class ExampleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
