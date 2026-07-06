using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// Positional-record DTOs carried by <see cref="TrimTestCommands._ProcessRecord"/>.
/// None of these are constructed anywhere in client-reachable code — their
/// constructors and properties survive trimming only through the generator-emitted
/// DtoConstructorRegistry.PreserveType&lt;T&gt;() calls (TRIM-001). RecordDtoSmokeTest
/// deserializes them from JSON literals to prove that preservation.
/// </summary>
public record TrimRecordDetail(string Notes);
public record TrimRecordResult(int Id, string Message, TrimRecordDetail Detail);
public record TrimRecordCommand(int PatientId, string Reason);

/// <summary>
/// Static factory used to test IL trimming of server-only dependencies.
/// Static factories use delegate types (not factory interfaces), which are
/// the original scenario where IL trimming broke factory registration.
/// </summary>
[Factory]
public static partial class TrimTestCommands
{
    [Remote]
    [Execute]
    private static Task<string> _DoWork(string input, [Service] IServerOnlyRepository repo)
    {
        return Task.FromResult(repo.DoServerWork(input));
    }

    // Positional records as [Execute] return type (with a nested record) and as a
    // non-service parameter — the zTreatment StartVisitResultV2 shape (TRIM-001).
    // DTO discovery is signature-based, so the body deliberately never constructs
    // the records: a `new TrimRecordResult(...)` here would root the ctor from the
    // (retained, guarded-dead) method body and make RecordDtoSmokeTest pass even
    // without the generator's PreserveType emission — a vacuous check.
    [Remote]
    [Execute]
    private static Task<TrimRecordResult?> _ProcessRecord(TrimRecordCommand command, [Service] IServerOnlyRepository repo)
    {
        repo.DoServerWork(command.Reason);
        return Task.FromResult<TrimRecordResult?>(null);
    }
}
