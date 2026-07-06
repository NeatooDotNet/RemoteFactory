using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// End-to-end trimming smoke test for entity property-graph DTO discovery (TRIM-002).
///
/// TrimEntityCarriedInfo (plain DTO → Register bucket) and TrimEntityCarriedBanner
/// (positional record → PreserveType bucket) are reachable ONLY as properties of the
/// [Factory] entity TrimTestEntity — they appear in no factory method signature and
/// are never constructed in harness code. Their constructors and property metadata
/// survive trimming only through the entity walk's emissions in TrimTestEntity's
/// FactoryServiceRegistrar. Without that walk, the trimmer strips them and
/// deserialization fails — the zTreatment TreatmentBanner / DashboardContactResult
/// failure class.
/// </summary>
public static class EntityPropertyDtoSmokeTest
{
    public static bool Run()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(EntityPropertyDtoSmokeTest).Assembly);

        using var sp = services.BuildServiceProvider();
        var serializer = sp.GetRequiredService<INeatooJsonSerializer>();

        // Plain DTO carried only as an entity property (Register bucket).
        TrimEntityCarriedInfo? info;
        try
        {
            info = serializer.Deserialize<TrimEntityCarriedInfo>("{\"Text\":\"carried\"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Entity property DTO smoke FAILED: carried DTO deserialization threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        if (info is null || info.Text != "carried")
        {
            Console.WriteLine($"Entity property DTO smoke FAILED: carried DTO values lost. Got Text=\"{info?.Text}\".");
            return false;
        }

        // Positional record carried only as an entity property (PreserveType bucket).
        TrimEntityCarriedBanner? banner;
        try
        {
            banner = serializer.Deserialize<TrimEntityCarriedBanner>("{\"Text\":\"warn\",\"Severity\":\"high\"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Entity property DTO smoke FAILED: carried record deserialization threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        if (banner is null || banner.Text != "warn" || banner.Severity != "high")
        {
            Console.WriteLine($"Entity property DTO smoke FAILED: carried record values lost. Got Text=\"{banner?.Text}\", Severity=\"{banner?.Severity}\".");
            return false;
        }

        Console.WriteLine("Entity property DTO smoke PASSED: entity-carried DTO and record survived trimming via the entity property-graph walk.");
        return true;
    }
}
