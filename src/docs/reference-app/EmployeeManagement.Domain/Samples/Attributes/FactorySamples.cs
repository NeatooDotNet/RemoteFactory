using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-factory
/// <summary>
/// Employee aggregate root with [Factory] attribute on class.
/// </summary>
[Factory]
public partial class EmployeeWithFactory
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeWithFactory()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Employee interface - [Factory] on interface generates factory for implementations.
/// </summary>
public interface IEmployeeContract
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
}

/// <summary>
/// Employee implementation with [Factory] on the class.
/// The factory is generated for this concrete class.
/// </summary>
[Factory]
public partial class EmployeeContract : IEmployeeContract
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeContract()
    {
        Id = Guid.NewGuid();
    }
}
#endregion
