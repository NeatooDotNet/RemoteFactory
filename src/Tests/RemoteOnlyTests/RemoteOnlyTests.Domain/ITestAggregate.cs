using Neatoo.RemoteFactory;

namespace RemoteOnlyTests.Domain;

/// <summary>
/// Interface for the test aggregate root.
/// </summary>
public interface ITestAggregate : IFactorySaveMeta
{
    Guid Id { get; set; }
    string? Name { get; set; }
    ITestChildList Children { get; set; }

    // Settable versions (hide IFactorySaveMeta's read-only properties)
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}
