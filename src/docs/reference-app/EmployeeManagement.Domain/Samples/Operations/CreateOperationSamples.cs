using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-create-constructor
/// <summary>
/// Employee aggregate with constructor-based creation.
/// </summary>
[Factory]
public partial class EmployeeCreateConstructor
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime HireDate { get; private set; }

    /// <summary>
    /// Creates a new Employee with generated ID and current hire date.
    /// </summary>
    [Create]
    public EmployeeCreateConstructor()
    {
        Id = Guid.NewGuid();
        HireDate = DateTime.UtcNow;
    }
}
#endregion

#region operations-create-static
/// <summary>
/// Employee aggregate with static factory method creation.
/// </summary>
[Factory]
public partial class EmployeeCreateStatic
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public decimal Salary { get; private set; }

    private EmployeeCreateStatic()
    {
    }

    /// <summary>
    /// Creates a new Employee with validation and formatting.
    /// </summary>
    [Create]
    public static EmployeeCreateStatic Create(
        string employeeNumber,
        string firstName,
        string lastName,
        decimal initialSalary)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("Employee number is required.", nameof(employeeNumber));

        return new EmployeeCreateStatic
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            Salary = initialSalary
        };
    }
}
#endregion

#region operations-create-return-types
/// <summary>
/// Employee aggregate demonstrating multiple Create patterns.
/// </summary>
[Factory]
public partial class EmployeeCreateReturnTypes
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool Initialized { get; private set; }

    /// <summary>
    /// Pattern 1: Constructor [Create] - returns the instance.
    /// </summary>
    [Create]
    public EmployeeCreateReturnTypes()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Pattern 2: Instance method [Create] void - sets properties and returns instance.
    /// </summary>
    [Create]
    public void Initialize(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        Initialized = true;
    }

    /// <summary>
    /// Pattern 3: Static method [Create] returning T - returns new instance with defaults.
    /// </summary>
    [Create]
    public static EmployeeCreateReturnTypes CreateWithDefaults()
    {
        return new EmployeeCreateReturnTypes
        {
            Id = Guid.NewGuid(),
            FirstName = "New",
            LastName = "Employee",
            Initialized = true
        };
    }
}
#endregion
