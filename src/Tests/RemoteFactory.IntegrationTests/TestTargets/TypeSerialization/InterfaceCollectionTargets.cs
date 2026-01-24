using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

/// <summary>
/// Test targets for interface-typed collection serialization round-trip.
/// Tests IList&lt;IInterface&gt;, ICollection&lt;IInterface&gt;, and related patterns.
/// </summary>

// ============================================================================
// Domain Interfaces
// ============================================================================

/// <summary>
/// Interface for testing interface-typed property serialization.
/// </summary>
public interface ITestChild
{
    Guid Id { get; }
    string Name { get; set; }
    decimal Value { get; set; }
}

/// <summary>
/// Interface for nested collection testing (IParent with IList&lt;IChild&gt;).
/// </summary>
public interface ITestParent
{
    Guid Id { get; }
    string Name { get; set; }
    IList<ITestChild> Children { get; }
}

// ============================================================================
// Concrete Implementations
// ============================================================================

/// <summary>
/// Concrete implementation of ITestChild for serialization tests.
/// </summary>
[Factory]
public class TestChildImpl : ITestChild
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Value { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]
    public void Fetch(Guid id, string name, decimal value)
    {
        Id = id;
        Name = name;
        Value = value;
    }
}

/// <summary>
/// Concrete implementation of ITestParent with IList&lt;ITestChild&gt; property.
/// </summary>
[Factory]
public class TestParentImpl : ITestParent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public IList<ITestChild> Children { get; set; } = new List<ITestChild>();

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]
    public void Fetch(Guid id, [Service] ITestChildImplFactory childFactory)
    {
        Id = id;
        Name = $"Parent-{id}";
        Children = new List<ITestChild>
        {
            childFactory.Fetch(Guid.NewGuid(), "Child1", 10.0m),
            childFactory.Fetch(Guid.NewGuid(), "Child2", 20.0m),
            childFactory.Fetch(Guid.NewGuid(), "Child3", 30.0m)
        };
    }
}

// ============================================================================
// Collection Property Variations
// ============================================================================

/// <summary>
/// Entity with various interface collection property types for comprehensive testing.
/// </summary>
[Factory]
public class InterfaceCollectionContainer : IFactorySaveMeta
{
    // Single interface property (baseline - should work)
    public ITestChild? SingleChild { get; set; }

    // Concrete collection, interface elements
    public List<ITestChild> ListOfInterface { get; set; } = new();

    // Interface collection, interface elements (this is the key test)
    public IList<ITestChild> IListOfInterface { get; set; } = new List<ITestChild>();

    // Other interface collection types
    public ICollection<ITestChild> ICollectionOfInterface { get; set; } = new List<ITestChild>();

    // Read-only interface (special case - no setter needed)
    public IReadOnlyList<ITestChild> IReadOnlyListOfInterface { get; set; } = new List<ITestChild>();

    // Nested: parent with interface collection
    public ITestParent? NestedParent { get; set; }

    // IFactorySaveMeta implementation
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    // Factory methods for testing
    [Create]
    [Remote]
    public static InterfaceCollectionContainer Create()
    {
        return new InterfaceCollectionContainer { IsNew = true };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchWithAllCollectionTypes(
        [Service] ITestChildImplFactory childFactory,
        [Service] ITestParentImplFactory parentFactory)
    {
        var child1 = childFactory.Fetch(Guid.NewGuid(), "Child1", 10.0m);
        var child2 = childFactory.Fetch(Guid.NewGuid(), "Child2", 20.0m);
        var child3 = childFactory.Fetch(Guid.NewGuid(), "Child3", 30.0m);

        return new InterfaceCollectionContainer
        {
            IsNew = false,
            SingleChild = child1,
            ListOfInterface = new List<ITestChild> { child1, child2 },
            IListOfInterface = new List<ITestChild> { child1, child2, child3 },
            ICollectionOfInterface = new List<ITestChild> { child1 },
            IReadOnlyListOfInterface = new List<ITestChild> { child2, child3 },
            NestedParent = parentFactory.Fetch(Guid.NewGuid())
        };
    }

    // Separate fetch methods for isolating test failures
    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchSingleInterface([Service] ITestChildImplFactory childFactory)
    {
        return new InterfaceCollectionContainer
        {
            IsNew = false,
            SingleChild = childFactory.Fetch(Guid.NewGuid(), "SingleChild", 100.0m)
        };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchListOfInterface([Service] ITestChildImplFactory childFactory)
    {
        return new InterfaceCollectionContainer
        {
            IsNew = false,
            ListOfInterface = new List<ITestChild>
            {
                childFactory.Fetch(Guid.NewGuid(), "ListChild1", 10.0m),
                childFactory.Fetch(Guid.NewGuid(), "ListChild2", 20.0m)
            }
        };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchIListOfInterface([Service] ITestChildImplFactory childFactory)
    {
        return new InterfaceCollectionContainer
        {
            IsNew = false,
            IListOfInterface = new List<ITestChild>
            {
                childFactory.Fetch(Guid.NewGuid(), "IListChild1", 10.0m),
                childFactory.Fetch(Guid.NewGuid(), "IListChild2", 20.0m)
            }
        };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchICollectionOfInterface([Service] ITestChildImplFactory childFactory)
    {
        return new InterfaceCollectionContainer
        {
            IsNew = false,
            ICollectionOfInterface = new List<ITestChild>
            {
                childFactory.Fetch(Guid.NewGuid(), "ICollectionChild1", 10.0m)
            }
        };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchNestedParent([Service] ITestParentImplFactory parentFactory)
    {
        return new InterfaceCollectionContainer
        {
            IsNew = false,
            NestedParent = parentFactory.Fetch(Guid.NewGuid())
        };
    }

    [Insert]
    [Remote]
    public Task Insert()
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    public Task Update()
    {
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    public Task Delete()
    {
        return Task.CompletedTask;
    }
}
