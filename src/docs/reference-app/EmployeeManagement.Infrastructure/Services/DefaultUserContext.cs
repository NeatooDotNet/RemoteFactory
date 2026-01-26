using EmployeeManagement.Domain.Interfaces;

namespace EmployeeManagement.Infrastructure.Services;

/// <summary>
/// Default implementation of IUserContext for demonstration.
/// In a real application, this would integrate with authentication.
/// </summary>
public class DefaultUserContext : IUserContext
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "demo-user";
    public IReadOnlyList<string> Roles { get; set; } = new[] { "User" };
    public bool IsAuthenticated { get; set; } = true;

    public bool IsInRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
