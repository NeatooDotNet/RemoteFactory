using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

/// <summary>
/// Test targets for identified serialization coverage gaps:
/// - Dictionary types
/// - Enum types
/// - Large objects
/// </summary>

// ============================================================================
// Dictionary Serialization
// ============================================================================

/// <summary>
/// Status enum for testing enum serialization.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

/// <summary>
/// Priority enum with explicit values for testing.
/// </summary>
public enum Priority
{
    Low = 10,
    Medium = 20,
    High = 30,
    Critical = 100
}

/// <summary>
/// Entity with Dictionary properties for testing dictionary serialization.
/// </summary>
[Factory]
public partial class DictionaryTarget
{
    public Dictionary<string, string> StringDictionary { get; set; } = new();
    public Dictionary<int, string> IntKeyDictionary { get; set; } = new();
    public Dictionary<string, int> IntValueDictionary { get; set; } = new();
    public Dictionary<Guid, string> GuidKeyDictionary { get; set; } = new();

    [Create]
    public static DictionaryTarget Create()
    {
        return new DictionaryTarget();
    }

    [Fetch]
    [Remote]
    public static DictionaryTarget FetchWithData()
    {
        var entity = new DictionaryTarget
        {
            StringDictionary = new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2",
                ["key3"] = "value3"
            },
            IntKeyDictionary = new Dictionary<int, string>
            {
                [1] = "one",
                [2] = "two",
                [42] = "forty-two"
            },
            IntValueDictionary = new Dictionary<string, int>
            {
                ["a"] = 1,
                ["b"] = 2,
                ["c"] = 100
            },
            GuidKeyDictionary = new Dictionary<Guid, string>
            {
                [Guid.Parse("11111111-1111-1111-1111-111111111111")] = "first",
                [Guid.Parse("22222222-2222-2222-2222-222222222222")] = "second"
            }
        };
        return entity;
    }
}

// ============================================================================
// Enum Serialization
// ============================================================================

/// <summary>
/// Entity with enum properties for testing enum serialization.
/// </summary>
[Factory]
public partial class EnumTarget
{
    public OrderStatus Status { get; set; }
    public Priority Priority { get; set; }
    public OrderStatus? NullableStatus { get; set; }

    [Create]
    public static EnumTarget Create(OrderStatus status, Priority priority)
    {
        return new EnumTarget { Status = status, Priority = priority };
    }

    [Fetch]
    [Remote]
    public static EnumTarget FetchByStatus(OrderStatus status)
    {
        return new EnumTarget
        {
            Status = status,
            Priority = status == OrderStatus.Pending ? Priority.Low : Priority.High,
            NullableStatus = status == OrderStatus.Cancelled ? null : status
        };
    }
}

/// <summary>
/// Record with enum parameters for testing record + enum combination.
/// </summary>
[Factory]
[Create]
public partial record EnumRecord(string Name, OrderStatus Status, Priority Priority);

// ============================================================================
// Large Object Serialization
// ============================================================================

/// <summary>
/// Entity with large data for testing serialization of large objects.
/// </summary>
[Factory]
public partial class LargeObjectTarget
{
    public string LargeString { get; set; } = "";
    public List<string> LargeList { get; set; } = new();
    public Dictionary<int, string> LargeDictionary { get; set; } = new();
    public byte[] BinaryData { get; set; } = Array.Empty<byte>();

    [Create]
    public static LargeObjectTarget Create()
    {
        return new LargeObjectTarget();
    }

    [Fetch]
    [Remote]
    public static LargeObjectTarget FetchLargeString(int charCount)
    {
        return new LargeObjectTarget
        {
            LargeString = new string('X', charCount)
        };
    }

    [Fetch]
    [Remote]
    public static LargeObjectTarget FetchLargeList(int itemCount)
    {
        var list = new List<string>(itemCount);
        for (int i = 0; i < itemCount; i++)
        {
            list.Add($"Item-{i:D6}");
        }
        return new LargeObjectTarget { LargeList = list };
    }

    [Fetch]
    [Remote]
    public static LargeObjectTarget FetchLargeDictionary(int itemCount)
    {
        var dict = new Dictionary<int, string>(itemCount);
        for (int i = 0; i < itemCount; i++)
        {
            dict[i] = $"Value-{i:D6}";
        }
        return new LargeObjectTarget { LargeDictionary = dict };
    }

    [Fetch]
    [Remote]
    public static LargeObjectTarget FetchBinaryData(int byteCount)
    {
        var data = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
        {
            data[i] = (byte)(i % 256);
        }
        return new LargeObjectTarget { BinaryData = data };
    }
}

// ============================================================================
// Combined Types (Dictionary + Enum)
// ============================================================================

/// <summary>
/// Entity with dictionary of enum values for complex type testing.
/// </summary>
[Factory]
public partial class EnumDictionaryTarget
{
    public Dictionary<string, OrderStatus> StatusByName { get; set; } = new();
    public Dictionary<OrderStatus, string> NameByStatus { get; set; } = new();

    [Create]
    public static EnumDictionaryTarget Create()
    {
        return new EnumDictionaryTarget();
    }

    [Fetch]
    [Remote]
    public static EnumDictionaryTarget FetchWithData()
    {
        return new EnumDictionaryTarget
        {
            StatusByName = new Dictionary<string, OrderStatus>
            {
                ["Order1"] = OrderStatus.Pending,
                ["Order2"] = OrderStatus.Shipped,
                ["Order3"] = OrderStatus.Delivered
            },
            NameByStatus = new Dictionary<OrderStatus, string>
            {
                [OrderStatus.Pending] = "Waiting",
                [OrderStatus.Processing] = "In Progress",
                [OrderStatus.Shipped] = "On the Way"
            }
        };
    }
}
