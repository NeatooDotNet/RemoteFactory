using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// Trimmed round trip of a public-setter <see cref="IFactorySaveMeta"/> class that has
/// no ordinal converter (<see cref="TrimCtorSaveTarget"/>), in both formats.
/// </summary>
/// <remarks>
/// The flags are deserialized from JSON and serialized back, and only the text is
/// checked, so the serializer is the one thing that touches the accessors: a trimmed
/// setter leaves the defaults (IsNew true, IsDeleted false), and a trimmed getter
/// drops the property from the output. The input values are the opposite of the
/// defaults, so either loss changes the text.
/// </remarks>
public static class SaveMetaRoundTripSmokeTest
{
    private const string Input = "{\"IsNew\":false,\"IsDeleted\":true}";

    public static bool Run()
    {
        var passed = true;
        foreach (var format in new[] { SerializationFormat.Ordinal, SerializationFormat.Named })
        {
            passed &= RunFormat(format);
        }
        return passed;
    }

    private static bool RunFormat(SerializationFormat format)
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = format },
            typeof(SaveMetaRoundTripSmokeTest).Assembly);
        services.AddScoped<IClientTallyPort, ClientTallyEngine>();

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var serializer = scope.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();

        string? output;
        try
        {
            var target = serializer.Deserialize<TrimCtorSaveTarget>(Input);
            output = serializer.Serialize(target);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SaveMeta round trip ({format}) FAILED: threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        // The Named format writes indented JSON; compare without whitespace.
        var compact = output is null ? null : string.Concat(output.Where(c => !char.IsWhiteSpace(c)));
        if (compact is null
            || !compact.Contains("\"IsNew\":false", StringComparison.Ordinal)
            || !compact.Contains("\"IsDeleted\":true", StringComparison.Ordinal))
        {
            Console.WriteLine($"SaveMeta round trip ({format}) FAILED: expected IsNew false and IsDeleted true to survive, got {output ?? "null"}.");
            return false;
        }

        Console.WriteLine($"SaveMeta round trip ({format}) PASSED: IsNew and IsDeleted survived on a class with no ordinal converter.");
        return true;
    }
}
