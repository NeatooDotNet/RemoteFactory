using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

/// <summary>
/// Service for loading employee performance reviews on demand.
/// </summary>
public interface ISkillReviewService
{
    Task<string?> GetReviewsAsync(Guid employeeId);
}

/// <summary>
/// In-memory implementation for testing and samples.
/// </summary>
public class SkillReviewService : ISkillReviewService
{
    public Task<string?> GetReviewsAsync(Guid employeeId)
    {
        return Task.FromResult<string?>($"Reviews for {employeeId}: Exceeds expectations.");
    }
}

#region skill-lazyload-complete
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
#endregion

#region skill-lazyload-eager
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
#endregion
