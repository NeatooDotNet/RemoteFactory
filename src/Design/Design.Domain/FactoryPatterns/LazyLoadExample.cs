// =============================================================================
// DESIGN SOURCE OF TRUTH: LazyLoad<T> Property Pattern
// =============================================================================
//
// This file demonstrates the LazyLoad<T> property pattern for deferred
// loading of related data. LazyLoad<T> enables explicit, on-demand loading
// with full serialization support across the client/server boundary.
//
// KEY CONCEPTS:
//
// 1. Value is PASSIVE: Reading .Value never triggers a load. It returns the
//    current value (null if not loaded, the loaded value otherwise).
//
// 2. LoadAsync() is EXPLICIT: Call LoadAsync() to trigger loading. The loader
//    delegate is invoked, Value is set, and PropertyChanged fires.
//
// 3. Constructor-Initialization Pattern: The loader delegate is set up in the
//    constructor using an injected factory. Since delegates are NOT serialized
//    (they reference server-side services), the constructor re-creates the
//    loader on deserialization. The ILazyLoadDeserializable merge pattern
//    preserves loaded state while keeping the fresh loader.
//
// 4. Two-Slot Ordinal Encoding: When a [Factory] class has a LazyLoad<T>
//    property, the generator uses two consecutive ordinal slots -- one for
//    the Value and one for IsLoaded. This is transparent to the developer.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryPatterns;

/// <summary>
/// Public interface for the Product entity with lazy-loaded reviews.
/// </summary>
public interface IProductWithReviews
{
    int Id { get; set; }
    string Name { get; set; }
    decimal Price { get; set; }

    /// <summary>
    /// Lazy-loaded reviews. Value is null until LoadAsync() is called.
    /// </summary>
    LazyLoad<string> Reviews { get; set; }
}

/// <summary>
/// Demonstrates LazyLoad&lt;T&gt; property on a [Factory] class.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Constructor-Initialization Pattern for LazyLoad
///
/// The loader delegate is created in the constructor using an injected
/// factory (IProductReviewService). This pattern ensures:
/// 1. The loader is always available (even after deserialization)
/// 2. Server-only services are method-injected (not stored in fields)
/// 3. The LazyLoad property can be loaded on-demand after fetch
///
/// HOW IT WORKS ACROSS THE WIRE:
///
/// 1. Server creates instance -> constructor sets up LazyLoad with loader
/// 2. Fetch runs -> loads Id, Name, Price but NOT Reviews (stays unloaded)
/// 3. Object serializes to client -> Reviews serialized as [null, false]
/// 4. Client deserializes -> constructor re-creates LazyLoad with new loader
/// 5. Generated code calls ILazyLoadDeserializable.ApplyDeserializedState()
///    to merge the serialized state (unloaded) into the constructor-created
///    instance, preserving the loader delegate
/// 6. Client can call Reviews.LoadAsync() -> loader invokes the service
///
/// DID NOT DO THIS: Load Reviews eagerly in Fetch
///
/// Reasons:
/// 1. Reviews may be expensive to load and often not needed
/// 2. Reduces payload size for list views
/// 3. Client controls when to load (e.g., when user expands a detail panel)
/// 4. Consistent with DDD's "lazy loading" pattern for optional associations
///
/// DID NOT DO THIS: Store the loader delegate in a field
///
/// Reasons:
/// 1. Delegates are NOT serialized (Anti-Pattern 5)
/// 2. The constructor-initialization pattern re-creates the delegate
/// 3. Method-injected services ([Service] on Fetch) are server-only
///
/// COMMON MISTAKE: Using BCL Lazy&lt;T&gt; instead of LazyLoad&lt;T&gt;
///
/// WRONG:
/// public Lazy&lt;string&gt; Reviews { get; set; }  // BCL Lazy - no serialization support
///
/// RIGHT:
/// public LazyLoad&lt;string&gt; Reviews { get; set; }  // RemoteFactory LazyLoad - serializable
///
/// BCL Lazy&lt;T&gt; does not support serialization. LazyLoad&lt;T&gt; has built-in
/// named-format and ordinal-format serialization with two-slot encoding.
/// </remarks>
[Factory]
internal partial class ProductWithReviews : IProductWithReviews
{
    // -------------------------------------------------------------------------
    // Properties
    //
    // DESIGN DECISION: LazyLoad<T> properties need public getter + setter
    //
    // Same rule as other serializable properties: the generated
    // IOrdinalSerializable uses property setters to restore state.
    // LazyLoad<T> itself has private setters on Value/IsLoaded (the inner
    // type manages its own state), but the LazyLoad<T> PROPERTY on the
    // owning class needs a public setter.
    // -------------------------------------------------------------------------

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>
    /// Lazy-loaded product reviews. Value is null until LoadAsync() is called.
    /// </summary>
    /// <remarks>
    /// The property is initialized with a parameterless LazyLoad (no loader).
    /// The constructor and Fetch methods replace it with a properly configured
    /// LazyLoad that has a loader delegate.
    ///
    /// GENERATOR BEHAVIOR: This property generates two ordinal slots:
    /// - Slot N: the Value (string)
    /// - Slot N+1: IsLoaded (bool)
    ///
    /// PropertyNames includes "Reviews" and "Reviews__IsLoaded".
    /// PropertyTypes includes typeof(string) and typeof(bool).
    /// </remarks>
    public LazyLoad<string> Reviews { get; set; } = new LazyLoad<string>();

    // -------------------------------------------------------------------------
    // Factory Operations
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new product with a lazy-loaded reviews property.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Set up LazyLoad with ILazyLoadFactory in the operation
    ///
    /// The ILazyLoadFactory.Create(loader) method creates a LazyLoad with the
    /// loader delegate. The loader captures the IProductReviewService so
    /// LoadAsync() can invoke it later.
    ///
    /// The loader delegate is NOT serialized. After deserialization, the
    /// constructor-initialization pattern re-creates the LazyLoad with a
    /// fresh loader in the next factory operation (Fetch, Create, etc.).
    /// </remarks>
    [Remote, Create]
    internal void Create(
        string name,
        decimal price,
        [Service] ILazyLoadFactory lazyLoadFactory,
        [Service] IProductReviewService reviewService)
    {
        Name = name;
        Price = price;

        // Set up LazyLoad with a loader that calls the review service.
        // LoadAsync() has NOT been called -- Value is null, IsLoaded is false.
        Reviews = lazyLoadFactory.Create<string>(async () =>
        {
            return await reviewService.GetReviewsAsync(Id);
        });
    }

    /// <summary>
    /// Fetches an existing product. Reviews are NOT loaded -- call
    /// Reviews.LoadAsync() to load them on demand.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Fetch does NOT load the LazyLoad property
    ///
    /// This is the key pattern: the aggregate root loads its direct state
    /// but defers loading of optional/expensive associations. The client
    /// decides when to trigger LoadAsync().
    ///
    /// After this Fetch completes and the object crosses the wire:
    /// - Reviews.Value == null
    /// - Reviews.IsLoaded == false
    /// - Reviews.LoadAsync() is available (loader re-created on deserialization)
    /// </remarks>
    [Remote, Fetch]
    internal void Fetch(
        int id,
        [Service] ILazyLoadFactory lazyLoadFactory,
        [Service] IProductReviewService reviewService)
    {
        // Load direct state
        Id = id;
        Name = $"Product_{id}";
        Price = 29.99m;

        // Set up LazyLoad with loader -- but do NOT call LoadAsync()
        Reviews = lazyLoadFactory.Create<string>(async () =>
        {
            return await reviewService.GetReviewsAsync(Id);
        });
    }

    /// <summary>
    /// Fetches a product with reviews pre-loaded (for cases where you know
    /// you'll need them immediately).
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Provide a separate Fetch overload for eager loading
    ///
    /// When you know reviews are needed (e.g., product detail page),
    /// use this overload to load them server-side in one round-trip.
    /// The LazyLoad property crosses the wire with IsLoaded=true and
    /// the Value populated.
    /// </remarks>
    [Remote, Fetch]
    internal void FetchWithReviews(
        int id,
        bool includeReviews,
        [Service] ILazyLoadFactory lazyLoadFactory,
        [Service] IProductReviewService reviewService)
    {
        // Load direct state
        Id = id;
        Name = $"Product_{id}";
        Price = 29.99m;

        if (includeReviews)
        {
            // Pre-load reviews server-side using ILazyLoadFactory.Create(value)
            var reviews = reviewService.GetReviewsAsync(Id).GetAwaiter().GetResult();
            Reviews = lazyLoadFactory.Create(reviews);
        }
        else
        {
            // Deferred -- loader set up, but LoadAsync() not called
            Reviews = lazyLoadFactory.Create<string>(async () =>
            {
                return await reviewService.GetReviewsAsync(Id);
            });
        }
    }
}

// =============================================================================
// SUPPORTING TYPES
// =============================================================================

/// <summary>
/// Service interface for loading product reviews.
/// </summary>
/// <remarks>
/// Server-only service. Injected via [Service] on factory methods.
/// In a real app, this would call a database or external API.
/// </remarks>
public interface IProductReviewService
{
    Task<string?> GetReviewsAsync(int productId);
}

/// <summary>
/// Mock implementation for testing and design validation.
/// </summary>
public class InMemoryProductReviewService : IProductReviewService
{
    public Task<string?> GetReviewsAsync(int productId)
    {
        // Simulate loading reviews from a data store
        return Task.FromResult<string?>($"Reviews for product {productId}: Great product! Highly recommended.");
    }
}
