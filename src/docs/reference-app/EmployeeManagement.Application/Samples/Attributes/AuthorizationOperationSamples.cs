using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

// Full implementations for authorization operations - see MinimalAttributesSamples.cs for doc snippets

public interface IEmployeeOperationAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
    bool CanCreateAndRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

public class EmployeeOperationAuthImpl : IEmployeeOperationAuth
{
    private readonly IUserContext _userContext;

    public EmployeeOperationAuthImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreateAndRead()
    {
        return _userContext.IsAuthenticated;
    }

    public bool CanDelete()
    {
        return _userContext.IsInRole("Administrator");
    }
}
