using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

#region attributes-authorizefactory-generic
/// <summary>
/// Authorization interface for Employee operations.
/// </summary>
public interface IEmployeeAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

/// <summary>
/// Implementation of Employee authorization rules.
/// </summary>
public class EmployeeAuthorizationImpl : IEmployeeAuthorization
{
    private readonly IUserContext _userContext;

    public EmployeeAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanRead()
    {
        return _userContext.IsAuthenticated;
    }

    public bool CanWrite()
    {
        return _userContext.IsInRole("HRManager");
    }
}

/// <summary>
/// Employee aggregate with class-level authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class EmployeeWithAuth
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeWithAuth()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task Fetch(
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
#endregion

#region attributes-authorizefactory-interface
/// <summary>
/// Authorization interface with operation-specific methods.
/// </summary>
public interface IDepartmentAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch(Guid departmentId);

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool CanUpdate(Guid departmentId);

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete(Guid departmentId);
}

/// <summary>
/// Implementation with fine-grained authorization per operation.
/// </summary>
public class DepartmentAuthorizationImpl : IDepartmentAuthorization
{
    private readonly IUserContext _userContext;

    public DepartmentAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreate()
    {
        return _userContext.IsAuthenticated;
    }

    public bool CanFetch(Guid departmentId)
    {
        return _userContext.IsAuthenticated;
    }

    public bool CanUpdate(Guid departmentId)
    {
        return _userContext.IsInRole("DepartmentManager");
    }

    public bool CanDelete(Guid departmentId)
    {
        return _userContext.IsInRole("Administrator");
    }
}
#endregion
