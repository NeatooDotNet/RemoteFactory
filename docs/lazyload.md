# LazyLoad&lt;T&gt;

RemoteFactory includes `LazyLoad<T>`, a wrapper for deferred async loading of related data. It gives you on-demand loading with full serialization support across the client/server boundary — no eager loading of expensive data, no extra round-trips when you don't need them.

The core pattern: your entity has a `LazyLoad<T>` property that is set up during a factory method with a loader delegate. The client receives the entity with `IsLoaded = false` and calls `LoadAsync()` when it needs the data. If the data is already available server-side, you can pre-load it instead.

## Key Concepts

**Value is passive.** Reading `.Value` never triggers a load. It returns the current value (`null` if not loaded). This is intentional — no surprise network calls from property access.

**LoadAsync() is explicit.** The only way to trigger loading. Call it when you need the data. It invokes the loader delegate, sets Value, and fires `PropertyChanged`.

**Loader delegates are not serialized.** They reference server-side services and cannot cross the wire. The factory method re-creates the loader on both sides — server-side during Fetch, client-side during deserialization (the generated code runs the factory method's constructor path again).

**`T : class` only.** Value types are not supported. Use `LazyLoad<string>`, `LazyLoad<List<Review>>`, etc.

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

**What happens across the wire:**

1. Server creates instance → factory method sets up `LazyLoad` with a loader delegate
2. Fetch runs → loads Id, FirstName, LastName but NOT PerformanceReviews (stays unloaded)
3. Object serializes to client → PerformanceReviews serialized as `[null, false]`
4. Client deserializes → factory method re-creates the `LazyLoad` with a new loader
5. Generated code calls `ApplyDeserializedState()` to merge the serialized state (unloaded) into the freshly created instance, preserving the loader delegate
6. Client calls `PerformanceReviews.LoadAsync()` when the user requests the data

## ILazyLoadFactory

Create `LazyLoad<T>` instances through `ILazyLoadFactory`, injected via `[Service]`:

| Method | Description |
|--------|-------------|
| `Create<T>(Func<Task<T?>> loader)` | Deferred — `IsLoaded = false`, call `LoadAsync()` later |
| `Create<T>(T? value)` | Pre-loaded — `IsLoaded = true`, Value populated immediately |

`ILazyLoadFactory` is registered as a singleton by `AddNeatooRemoteFactory()`. No manual registration needed.

## Eager vs Deferred Loading

Use deferred loading when the data is expensive and not always needed. Use eager loading when you know the data is needed immediately.

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

**Decision guide:**

| Scenario | Pattern | Method |
|----------|---------|--------|
| Data not always needed (reviews, history, details) | Deferred | `lazyLoadFactory.Create<T>(loader)` |
| Data always needed, loaded from a separate source | Eager | `lazyLoadFactory.Create(value)` |
| Core properties of the entity | Regular property | No LazyLoad needed |

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

`LazyLoad<T>` implements `INotifyPropertyChanged`, so Blazor components and WPF bindings react to value changes automatically.

## Client Usage

After fetching an entity with a deferred `LazyLoad<T>` property:

```csharp
// Fetch returns the entity — Reviews not loaded yet
var employee = await factory.Fetch(employeeId);
// employee.PerformanceReviews.Value == null
// employee.PerformanceReviews.IsLoaded == false

// Load on demand (e.g., user expands a detail panel)
var reviews = await employee.PerformanceReviews.LoadAsync();
// employee.PerformanceReviews.Value == "Reviews for ..."
// employee.PerformanceReviews.IsLoaded == true
```

Thread safety: multiple concurrent `LoadAsync()` calls share a single load operation. The loader runs once.

## Property Declaration

`LazyLoad<T>` properties on `[Factory]` classes follow the same rules as other properties:

```csharp
// Public getter + setter required for serialization
public LazyLoad<string> Reviews { get; set; } = new LazyLoad<string>();
```

Initialize with `new LazyLoad<T>()` as the default. Factory methods replace it with a properly configured instance via `ILazyLoadFactory`.

## Serialization

`LazyLoad<T>` has full serialization support in both formats. See [Serialization — LazyLoad Properties](serialization.md#lazyloadt-properties) for format details.

**Named format:**
```json
{"performanceReviews": {"value": "loaded data", "isLoaded": true}}
```

**Ordinal format:** Two consecutive array slots per `LazyLoad<T>` property — one for Value, one for IsLoaded:
```
["Jane", "Smith", null, false]
                  ^Value ^IsLoaded
```

An unloaded property serializes as `[null, false]`. A loaded property serializes as `[value, true]`.

## Common Mistakes

**Using BCL `Lazy<T>` instead of `LazyLoad<T>`:**
```csharp
// WRONG — BCL Lazy<T> has no serialization support
public Lazy<string> Reviews { get; set; }

// RIGHT — RemoteFactory LazyLoad<T> with serialization
public LazyLoad<string> Reviews { get; set; } = new LazyLoad<string>();
```

**Storing the loader delegate in a field:**
```csharp
// WRONG — delegates are NOT serialized
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

**Calling LoadAsync() inside a Fetch method:**
```csharp
// WRONG — defeats the purpose of deferred loading
[Fetch]
internal async void Fetch([Service] ILazyLoadFactory factory, [Service] IReviewService svc)
{
    Reviews = factory.Create<string>(async () => await svc.GetReviewsAsync(Id));
    await Reviews.LoadAsync();  // Loading eagerly — use Create(value) instead
}

// RIGHT — pre-load if you need the data immediately
[Fetch]
internal void FetchEager([Service] ILazyLoadFactory factory, [Service] IReviewService svc)
{
    var reviews = svc.GetReviewsAsync(Id).GetAwaiter().GetResult();
    Reviews = factory.Create(reviews);  // Pre-loaded, no LoadAsync needed
}
```

## Next Steps

- [Serialization](serialization.md) — How LazyLoad properties are encoded in both formats
- [Interfaces Reference](interfaces-reference.md#ilazyloadfactory) — ILazyLoadFactory API
- [Service Injection](service-injection.md) — Constructor vs method injection patterns
- [Client-Server Architecture](client-server-architecture.md) — How objects cross the wire
