namespace EmployeeManagement.Domain.Interfaces;

/// <summary>
/// Provides information about the current user for authorization.
/// </summary>
public interface IUserContext
{
    Guid UserId { get; }
    string Username { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
