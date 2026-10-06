using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// Trimmed-client checks for two DTO-walk blind spots (DICT-001). See the header of
/// <c>TrimDictCarrier.cs</c> for the shapes.
/// </summary>
/// <remarks>
/// <para>
/// Each check is its own method, wired as its own named failure in <c>Program.cs</c>,
/// so one red cannot hide the other.
/// </para>
/// <para>
/// Both were observed RED on the publish-trimmed harness against the generator before
/// the walk learned these shapes, and GREEN untrimmed in the same session — so the red
/// is caused by trimming, not by the JSON or the check. The recorded runs are in
/// <c>docs/todos/DICT-dictionary-dto-preservation/reviews/001-evidence/</c>.
/// </para>
/// <para>
/// Neither method constructs the DTO it checks. Generic call sites such as
/// <c>Deserialize&lt;T&gt;</c> carry no trimmer annotation and no <c>new()</c>
/// constraint, so they do not root <c>T</c>'s constructor either.
/// </para>
/// </remarks>
public static class DictionaryAndFieldDtoSmokeTest
{
    /// <summary>
    /// A DTO reachable only as a dictionary value type, constructed only inside an
    /// async <c>[Remote]</c> body. Deserializes the dictionary itself, which is what
    /// the generated ordinal converter hands to the reflection serializer for a
    /// dictionary-typed entity property.
    /// </summary>
    public static bool RunDictionaryValue()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(DictionaryAndFieldDtoSmokeTest).Assembly);

        using var sp = services.BuildServiceProvider();
        var serializer = sp.GetRequiredService<INeatooJsonSerializer>();

        // "l-f-1" is the key from the consumer's failure path, $.l-f-1.
        Dictionary<string, TrimDictValue>? locations;
        try
        {
            locations = serializer.Deserialize<Dictionary<string, TrimDictValue>>(
                "{\"l-f-1\":{\"Region\":\"left-foot\",\"Severity\":3}}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dictionary-value DTO smoke FAILED: deserialization threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        if (locations is null
            || !locations.TryGetValue("l-f-1", out var value)
            || value.Region != "left-foot"
            || value.Severity != 3)
        {
            Console.WriteLine("Dictionary-value DTO smoke FAILED: the entry or its values were lost after deserialization.");
            return false;
        }

        Console.WriteLine("Dictionary-value DTO smoke PASSED: a dictionary value type constructed only inside an async [Remote] body survived trimming.");
        return true;
    }

    /// <summary>
    /// A DTO reachable only through a public field of a walked DTO. Deserializes the
    /// field's host, which the entity walk registers, so the host itself constructs and
    /// the check isolates the field's type.
    /// </summary>
    public static bool RunPublicField()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(DictionaryAndFieldDtoSmokeTest).Assembly);

        using var sp = services.BuildServiceProvider();
        var serializer = sp.GetRequiredService<INeatooJsonSerializer>();

        TrimFieldHost? host;
        try
        {
            host = serializer.Deserialize<TrimFieldHost>("{\"Carried\":{\"Text\":\"through-field\"}}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Public-field DTO smoke FAILED: deserialization threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        if (host?.Carried is null || host.Carried.Text != "through-field")
        {
            Console.WriteLine($"Public-field DTO smoke FAILED: the field's value was lost. Got Text=\"{host?.Carried?.Text}\".");
            return false;
        }

        Console.WriteLine("Public-field DTO smoke PASSED: a DTO reachable only through a public field survived trimming.");
        return true;
    }
}
