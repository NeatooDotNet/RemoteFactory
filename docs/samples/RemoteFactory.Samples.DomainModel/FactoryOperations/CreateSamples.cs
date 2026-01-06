/// <summary>
/// Code samples for docs/concepts/factory-operations.md - Create operations
/// </summary>

using Neatoo.RemoteFactory;

namespace RemoteFactory.Samples.DomainModel.FactoryOperations.CreateExamples;

#region docs:concepts/factory-operations:multiple-constructors
[Factory]
public class PersonWithMultipleConstructors : IPersonModel
{
    [Create]
    public PersonWithMultipleConstructors()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsNew = true;
    }

    [Create]
    public PersonWithMultipleConstructors(string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        IsNew = true;
    }

    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }
}
#endregion

// Note: The generated factory interface is shown in the docs as:
// public interface IPersonWithMultipleConstructorsFactory
// {
//     IPersonModel? Create();
//     IPersonModel? Create(string firstName, string lastName);
// }

#region docs:concepts/factory-operations:create-on-method
[Factory]
public class PersonWithMethodCreate
{
    public PersonWithMethodCreate() { }

    [Create]
    public void Initialize(string template)
    {
        // Setup from template
        Template = template;
    }

    [Create]
    public async Task InitializeAsync([Service] ITemplateService templates)
    {
        // Async initialization with services
        Template = await templates.GetDefaultTemplateAsync();
    }

    public string? Template { get; private set; }
}
#endregion

#region docs:concepts/factory-operations:static-create
[Factory]
public class PersonWithStaticCreate
{
    private PersonWithStaticCreate() { }

    [Create]
    public static async Task<PersonWithStaticCreate> CreateWithDefaults([Service] IDefaultsService defaults)
    {
        var model = new PersonWithStaticCreate();
        await model.ApplyDefaults(defaults);
        return model;
    }

    private async Task ApplyDefaults(IDefaultsService defaults)
    {
        DefaultValue = await defaults.GetDefaultValueAsync();
    }

    public string? DefaultValue { get; private set; }
}
#endregion

// Supporting interfaces
public interface IPersonModel : IFactorySaveMeta
{
    Guid Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}

public interface ITemplateService
{
    Task<string> GetDefaultTemplateAsync();
}

public interface IDefaultsService
{
    Task<string> GetDefaultValueAsync();
}
