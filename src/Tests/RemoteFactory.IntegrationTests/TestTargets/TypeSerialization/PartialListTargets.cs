using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

/// <summary>
/// Targets for #119: a <c>partial</c> <c>[Factory]</c> class deriving from <c>List&lt;T&gt;</c>.
/// </summary>
/// <remarks>
/// <see cref="AggregateChildList"/> is the same shape without <c>partial</c>, which never
/// reached the ordinal path. Being <c>partial</c> with no constructor services is what made the
/// generator emit an object-shaped ordinal converter that wrote only <c>Capacity</c>.
/// </remarks>
[Factory]
public partial class PartialListItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [Create]
    public void Create(string name)
    {
        Name = name;
    }

    [Fetch]
    internal void Fetch(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

[Factory]
public partial class PartialItemList : List<PartialListItem>
{
    [Create]
    public PartialItemList() { }

    [Fetch]
    internal void Fetch(
        IEnumerable<(int id, string name)> items,
        [Service] IPartialListItemFactory itemFactory)
    {
        foreach (var item in items)
        {
            Add(itemFactory.Fetch(item.id, item.name));
        }
    }
}

[Factory]
public partial class PartialListRoot : IFactorySaveMeta
{
    public int Id { get; set; }
    public PartialItemList Items { get; set; } = null!;

    /// <summary>The item names the server's <c>[Update]</c> received, in order.</summary>
    public List<string> NamesSeenByServer { get; set; } = [];

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Fetch]
    internal void Fetch(int id, [Service] IPartialItemListFactory listFactory)
    {
        Id = id;
        Items = listFactory.Fetch([(1, "First"), (2, "Second"), (3, "Third")]);
        IsNew = false;
    }

    [Remote, Update]
    internal void Update()
    {
        NamesSeenByServer = Items.Select(i => i.Name).ToList();
    }
}
