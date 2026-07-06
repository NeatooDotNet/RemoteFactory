using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// End-to-end trimming smoke test for positional-record DTO preservation (TRIM-001).
///
/// The records (TrimRecordResult, TrimRecordCommand, TrimRecordDetail) appear in a
/// [Remote, Execute] signature but are never constructed in client-reachable code —
/// this test deserializes them from JSON literals, so their parameterized constructors
/// are rooted ONLY by the generator-emitted DtoConstructorRegistry.PreserveType&lt;T&gt;()
/// calls in the FactoryServiceRegistrar. If that emission is missing, the trimmer
/// strips the ctor metadata and deserialization fails — exactly the consumer-side
/// DeserializeNoConstructor failure this plan closes.
///
/// Covers all three TRIM-001 shapes:
///   - record as [Execute] return type      (TrimRecordResult)
///   - record nested in a discovered record (TrimRecordDetail, property of the result)
///   - record as non-service parameter      (TrimRecordCommand)
/// </summary>
public static class RecordDtoSmokeTest
{
    public static bool Run()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(RecordDtoSmokeTest).Assembly);

        using var sp = services.BuildServiceProvider();
        var serializer = sp.GetRequiredService<INeatooJsonSerializer>();

        // Record as return type, with nested record property.
        TrimRecordResult? result;
        try
        {
            result = serializer.Deserialize<TrimRecordResult>(
                "{\"Id\":42,\"Message\":\"trim-smoke\",\"Detail\":{\"Notes\":\"nested\"}}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Record DTO smoke FAILED: return-shape deserialization threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        if (result is null || result.Id != 42 || result.Message != "trim-smoke")
        {
            Console.WriteLine($"Record DTO smoke FAILED: return-shape values lost. Got Id={result?.Id}, Message=\"{result?.Message}\".");
            return false;
        }

        if (result.Detail is null || result.Detail.Notes != "nested")
        {
            Console.WriteLine($"Record DTO smoke FAILED: nested record lost. Got Notes=\"{result.Detail?.Notes}\".");
            return false;
        }

        // Record as non-service parameter.
        TrimRecordCommand? command;
        try
        {
            command = serializer.Deserialize<TrimRecordCommand>(
                "{\"PatientId\":7,\"Reason\":\"checkup\"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Record DTO smoke FAILED: parameter-shape deserialization threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        if (command is null || command.PatientId != 7 || command.Reason != "checkup")
        {
            Console.WriteLine($"Record DTO smoke FAILED: parameter-shape values lost. Got PatientId={command?.PatientId}, Reason=\"{command?.Reason}\".");
            return false;
        }

        Console.WriteLine("Record DTO smoke PASSED: positional records survived trimming via generator-emitted PreserveType (return, parameter, and nested shapes).");
        return true;
    }
}
