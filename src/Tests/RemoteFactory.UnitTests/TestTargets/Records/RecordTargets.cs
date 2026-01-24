using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Records;

/// <summary>
/// Simple positional record with [Create] on type.
/// Tests that factory method is generated for primary constructor.
/// </summary>
[Factory]
[Create]
public partial record SimpleRecord(string Name, int Value);

/// <summary>
/// Record with [Service] in primary constructor.
/// Tests that service parameters are injected, not passed through factory.
/// </summary>
[Factory]
[Create]
public partial record RecordWithService(string Name, [Service] IService Service);

/// <summary>
/// Record with nullable parameters.
/// </summary>
[Factory]
[Create]
public partial record RecordWithNullable(string Name, string? Description);

/// <summary>
/// Record with collection in primary constructor.
/// </summary>
[Factory]
[Create]
public partial record RecordWithCollection(string Name, List<string> Items);

/// <summary>
/// Record with default parameter values.
/// </summary>
[Factory]
[Create]
public partial record RecordWithDefaults(string Name = "default", int Value = 42);

/// <summary>
/// Nested record - outer.
/// </summary>
[Factory]
[Create]
public partial record OuterRecord(string Name, InnerRecord Inner);

/// <summary>
/// Nested record - inner.
/// </summary>
[Factory]
[Create]
public partial record InnerRecord(string InnerValue);

/// <summary>
/// Record with explicit [Fetch] method.
/// </summary>
[Factory]
[Create]
public partial record RecordWithFetch(string Name)
{
    [Fetch]
    public static RecordWithFetch FetchById(int id)
    {
        return new RecordWithFetch($"Fetched-{id}");
    }
}

/// <summary>
/// Record with [Remote] fetch method.
/// Tests Server mode handling of [Remote] on records.
/// </summary>
[Factory]
[Create]
public partial record RecordWithRemoteFetch(string Name)
{
    [Fetch]
    [Remote]
    public static RecordWithRemoteFetch RemoteFetch(string prefix)
    {
        return new RecordWithRemoteFetch($"Remote-{prefix}");
    }

    [Fetch]
    [Remote]
    public static Task<RecordWithRemoteFetch> RemoteFetchAsync(string prefix)
    {
        return Task.FromResult(new RecordWithRemoteFetch($"RemoteAsync-{prefix}"));
    }
}

/// <summary>
/// Record with multiple primary constructor parameters of various types.
/// </summary>
[Factory]
[Create]
public partial record ComplexRecord(
    string StringProp,
    int IntProp,
    long LongProp,
    double DoubleProp,
    decimal DecimalProp,
    bool BoolProp,
    DateTime DateTimeProp,
    Guid GuidProp);
