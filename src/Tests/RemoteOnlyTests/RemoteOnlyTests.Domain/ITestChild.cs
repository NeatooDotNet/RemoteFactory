namespace RemoteOnlyTests.Domain;

/// <summary>
/// Interface for child entities.
/// </summary>
public interface ITestChild
{
    Guid Id { get; set; }
    string? Name { get; set; }
    decimal Value { get; set; }
}
