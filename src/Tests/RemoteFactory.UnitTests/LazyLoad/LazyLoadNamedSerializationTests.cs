using System.Text.Json;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.LazyLoad;

/// <summary>
/// Named format serialization round-trip tests for <see cref="LazyLoad{T}"/>.
/// Verifies the <see cref="LazyLoadJsonConverterFactory"/> produces correct JSON
/// and reconstructs instances on deserialization.
/// </summary>
public class LazyLoadNamedSerializationTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LazyLoadJsonConverterFactory());
        return options;
    }

    /// <summary>
    /// TS-LL-013 (BR-LL-013, BR-LL-015): Loaded LazyLoad round-trips through named JSON.
    /// </summary>
    [Fact]
    public void NamedFormat_Loaded_RoundTrip()
    {
        var options = CreateOptions();
        var original = new LazyLoad<string>("hello");

        // Serialize
        var json = JsonSerializer.Serialize(original, options);

        // Verify JSON structure
        Assert.Contains("\"value\":\"hello\"", json);
        Assert.Contains("\"isLoaded\":true", json);

        // Deserialize
        var deserialized = JsonSerializer.Deserialize<LazyLoad<string>>(json, options);

        Assert.NotNull(deserialized);
        Assert.Equal("hello", deserialized!.Value);
        Assert.True(deserialized.IsLoaded);
    }

    /// <summary>
    /// TS-LL-014 (BR-LL-014, BR-LL-016): Unloaded LazyLoad round-trips through named JSON.
    /// </summary>
    [Fact]
    public void NamedFormat_Unloaded_RoundTrip()
    {
        var options = CreateOptions();
        var original = new LazyLoad<string>();

        // Serialize
        var json = JsonSerializer.Serialize(original, options);

        // Verify JSON structure
        Assert.Contains("\"value\":null", json);
        Assert.Contains("\"isLoaded\":false", json);

        // Deserialize
        var deserialized = JsonSerializer.Deserialize<LazyLoad<string>>(json, options);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized!.Value);
        Assert.False(deserialized.IsLoaded);
    }
}
