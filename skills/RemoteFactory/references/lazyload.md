# LazyLoad&lt;T&gt; — Deferred Async Loading

`LazyLoad<T>` is a wrapper for deferred async loading of related data. It enables on-demand loading with full serialization support across the client/server boundary.

## Key Principles

1. **Value is passive** — Reading `.Value` never triggers a load. It returns the current value (`null` if not loaded).
2. **LoadAsync() is explicit** — The only way to trigger loading. Invokes the loader delegate, sets Value, fires PropertyChanged.
3. **Constructor-initialization pattern** — The loader delegate is set up in factory methods using `ILazyLoadFactory`. Delegates are NOT serialized. After deserialization, the factory method re-creates the loader.
4. **Two-slot ordinal encoding** — The generator uses two consecutive ordinal slots for each `LazyLoad<T>` property: one for Value, one for IsLoaded. This is transparent to the developer.

## Complete Example

<!-- snippet: skill-lazyload-complete -->
<a id='snippet-skill-lazyload-complete'></a>
```cs
[Factory]
public partial class SkillEmployeeWithReviews
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // LazyLoad<T> property — deferred loading of expensive data.
    // Initialize with parameterless constructor; factory methods replace it.
    public LazyLoad<string> PerformanceReviews { get; set; } = new LazyLoad<string>();

    [Remote, Create]
    internal void Create(
        string firstName,
        string lastName,
        [Service] ILazyLoadFactory lazyLoadFactory,
        [Service] ISkillReviewService reviewService)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;

        // Set up loader but don't load yet — Value is null, IsLoaded is false
        PerformanceReviews = lazyLoadFactory.Create<string>(async () =>
        {
            return await reviewService.GetReviewsAsync(Id);
        });
    }

    [Remote, Fetch]
    internal void Fetch(
        Guid id,
        [Service] ILazyLoadFactory lazyLoadFactory,
        [Service] ISkillReviewService reviewService)
    {
        Id = id;
        FirstName = "Jane";
        LastName = "Smith";

        // Deferred: set up loader, don't call LoadAsync()
        // After serialization, the loader delegate is re-created on deserialization
        PerformanceReviews = lazyLoadFactory.Create<string>(async () =>
        {
            return await reviewService.GetReviewsAsync(Id);
        });
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/LazyLoadSamples.cs#L24-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazyload-complete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Key points:**
- Inject `ILazyLoadFactory` as a `[Service]` parameter
- Use `lazyLoadFactory.Create<T>(loader)` for deferred loading
- The `LazyLoad<T>` property needs a public setter (same rule as other properties)
- Initialize with `new LazyLoad<T>()` as default — factory methods replace it

## Eager vs Deferred Loading

<!-- snippet: skill-lazyload-eager -->
<a id='snippet-skill-lazyload-eager'></a>
```cs
[Factory]
public partial class SkillProductWithDetails
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LazyLoad<string> Details { get; set; } = new LazyLoad<string>();

    // Eager: pre-load the value server-side with ILazyLoadFactory.Create(value)
    [Remote, Fetch]
    internal void FetchWithDetails(
        int id,
        [Service] ILazyLoadFactory lazyLoadFactory)
    {
        Id = id;
        Name = $"Product_{id}";

        // Pre-loaded: IsLoaded = true, Value populated, no LoadAsync() needed
        Details = lazyLoadFactory.Create($"Details for product {id}");
    }

    // Deferred: set up loader, let client decide when to load
    [Remote, Fetch]
    internal void Fetch(
        int id,
        [Service] ILazyLoadFactory lazyLoadFactory,
        [Service] ISkillReviewService reviewService)
    {
        Id = id;
        Name = $"Product_{id}";

        // Deferred: IsLoaded = false, call LoadAsync() to trigger
        Details = lazyLoadFactory.Create<string>(async () =>
        {
            return await reviewService.GetReviewsAsync(Guid.Empty);
        });
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/LazyLoadSamples.cs#L74-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-lazyload-eager' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Two creation patterns:**

| Pattern | Method | Result |
|---------|--------|--------|
| **Deferred** | `lazyLoadFactory.Create<T>(async () => ...)` | `IsLoaded = false`, call `LoadAsync()` later |
| **Eager** | `lazyLoadFactory.Create(value)` | `IsLoaded = true`, Value populated immediately |

## LazyLoad&lt;T&gt; API

| Member | Type | Description |
|--------|------|-------------|
| `Value` | `T?` | Current value. Passive read — never triggers load. |
| `IsLoaded` | `bool` | Whether the value has been loaded. |
| `IsLoading` | `bool` | Whether a load is currently in progress. |
| `HasLoadError` | `bool` | Whether the last load attempt failed. |
| `LoadError` | `string?` | Error message from last failed load. |
| `LoadAsync()` | `Task<T?>` | Triggers loading. Thread-safe — concurrent calls share one load. |
| `SetValue(T?)` | `void` | Directly sets value, bypasses loader. Marks as loaded. |
| `PropertyChanged` | event | Fires for Value, IsLoaded, IsLoading changes. |

## ILazyLoadFactory API

| Method | Description |
|--------|-------------|
| `Create<T>(Func<Task<T?>> loader)` | Deferred — value loads when `LoadAsync()` is called |
| `Create<T>(T? value)` | Eager — value already loaded, `IsLoaded = true` |

`ILazyLoadFactory` is registered as a singleton by `AddNeatooRemoteFactory()`. No manual registration needed.

## Serialization Behavior

**Named format (JSON):**
```json
{
  "performanceReviews": {
    "value": "the loaded value",
    "isLoaded": true
  }
}
```

**Ordinal format (arrays):** Two consecutive slots per `LazyLoad<T>` property:
- Slot N: the Value
- Slot N+1: IsLoaded (bool)

**Unloaded state** serializes as `[null, false]`. On deserialization, the generated code calls `ILazyLoadDeserializable.ApplyDeserializedState()` to merge the serialized state into the constructor-created instance, preserving the loader delegate.

## Client Usage

After fetching an entity with a deferred `LazyLoad<T>` property:

```csharp
// Fetch returns the entity with Reviews not yet loaded
var product = await factory.Fetch(productId);
// product.PerformanceReviews.Value == null
// product.PerformanceReviews.IsLoaded == false

// Load on demand (e.g., when user expands a detail panel)
var reviews = await product.PerformanceReviews.LoadAsync();
// product.PerformanceReviews.Value == "Reviews for ..."
// product.PerformanceReviews.IsLoaded == true
```

## Common Mistakes

**Using BCL `Lazy<T>` instead of `LazyLoad<T>`:**
```csharp
// WRONG — BCL Lazy<T> has no serialization support
public Lazy<string> Reviews { get; set; }

// RIGHT — RemoteFactory LazyLoad<T> serializes correctly
public LazyLoad<string> Reviews { get; set; } = new LazyLoad<string>();
```

**Storing the loader delegate in a field:**
```csharp
// WRONG — delegates are NOT serialized (Anti-Pattern 5)
private Func<Task<string?>> _loader;

[Fetch]
internal void Fetch([Service] IReviewService svc)
{
    _loader = () => svc.GetReviewsAsync(Id);  // Lost after serialization
}

// RIGHT — use ILazyLoadFactory in the factory method
[Fetch]
internal void Fetch([Service] ILazyLoadFactory factory, [Service] IReviewService svc)
{
    Reviews = factory.Create<string>(async () => await svc.GetReviewsAsync(Id));
}
```

**Calling LoadAsync() in a Fetch method:**
```csharp
// WRONG — defeats the purpose of deferred loading
[Fetch]
internal async void Fetch([Service] ILazyLoadFactory factory, [Service] IReviewService svc)
{
    Reviews = factory.Create<string>(async () => await svc.GetReviewsAsync(Id));
    await Reviews.LoadAsync();  // Loading eagerly — use Create(value) instead
}

// RIGHT — if you need eager loading, pre-load the value
[Fetch]
internal void FetchEager([Service] ILazyLoadFactory factory, [Service] IReviewService svc)
{
    var reviews = svc.GetReviewsAsync(Id).GetAwaiter().GetResult();
    Reviews = factory.Create(reviews);  // Pre-loaded, no LoadAsync needed
}
```

**Using `LazyLoad<T>` with value types:**
```csharp
// WRONG — LazyLoad<T> requires T : class
public LazyLoad<int> Count { get; set; }

// RIGHT — use a reference type (or wrap in a class)
public LazyLoad<string> ReviewText { get; set; }
```

## When to Use LazyLoad&lt;T&gt;

| Scenario | Use LazyLoad? |
|----------|---------------|
| Expensive data not always needed (reviews, history) | Yes — deferred |
| Data always needed but loaded separately | Yes — eager via `Create(value)` |
| Core properties of the entity | No — use regular properties |
| Child collections managed by the aggregate | No — use child factory pattern |
| Value types (int, decimal, bool) | No — `T : class` constraint |

## Constraint

`LazyLoad<T>` has a `where T : class?` constraint. Value types are not supported. Record primary constructors with `LazyLoad<T>` parameters are out of scope — `LazyLoad<T>` properties should be set in factory methods, not via record constructors.
