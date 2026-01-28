using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-aspauthorize
/// <summary>
/// Employee aggregate with various [AspAuthorize] patterns.
/// </summary>
[Factory]
public partial class EmployeeWithAspAuth
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }

    [Create]
    public EmployeeWithAspAuth()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Policy-based authorization.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public async Task FetchWithPolicy(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }

    /// <summary>
    /// Role-based authorization with multiple roles.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(Roles = "HRManager,Administrator")]
    public async Task FetchWithRoles(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            Salary = entity.SalaryAmount;
        }
    }

    /// <summary>
    /// Scheme-based authorization.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(AuthenticationSchemes = "Bearer")]
    public async Task FetchWithScheme(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }
}

/// <summary>
/// Payroll commands with [AspAuthorize].
/// </summary>
[Factory]
public static partial class PayrollCommands
{
    [Remote, Execute]
    [AspAuthorize(Roles = "Payroll")]
    private static Task<bool> _ProcessPayroll(
        Guid departmentId,
        DateTime payPeriodEnd,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        return Task.FromResult(true);
    }
}
#endregion
