using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.BugScenarios;

#region Test Target Classes

/// <summary>
/// Save classes are Nullable for instances where there is a Delete of an IsNew item
/// because it should return null. But, if there is no Delete then don't have the Save be nullable.
/// </summary>
[Factory]
public class SaveWNoDeleteIsNotNullable : IFactorySaveMeta
{
    [Create]
    public SaveWNoDeleteIsNotNullable()
    {
    }

    public bool IsDeleted => false;
    public bool IsNew => true;

    [Insert]
    public void Insert() { }

    [Update]
    public void Update() { }
}

#endregion

/// <summary>
/// Tests that Save without Delete has a non-nullable return type.
/// </summary>
public class SaveWNoDeleteIsNotNullableTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly ISaveWNoDeleteIsNotNullableFactory _factory;

    public SaveWNoDeleteIsNotNullableTests()
    {
        _provider = new ServerContainerBuilder().Build();
        _factory = _provider.GetRequiredService<ISaveWNoDeleteIsNotNullableFactory>();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0007:Use implicit type", Justification = "Explicit type verifies non-nullable return")]
    public void SaveWNoDeleteIsNotNullable_NoNullableError()
    {
        // Explicit type annotation verifies the return type is non-nullable
        SaveWNoDeleteIsNotNullable save = _factory.Create();
        save = _factory.Save(save);
        Assert.NotNull(save);
    }
}
