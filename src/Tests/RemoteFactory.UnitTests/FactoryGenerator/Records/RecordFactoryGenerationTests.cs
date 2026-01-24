using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Records;

namespace RemoteFactory.UnitTests.FactoryGenerator.Records;

/// <summary>
/// Unit tests for record factory generation in Server mode.
/// These tests verify that factory interfaces and methods are properly generated
/// for C# record types, including primary constructor handling and service injection.
/// Integration tests for record serialization round-trips are in RemoteFactory.IntegrationTests.
/// </summary>
public class RecordFactoryGenerationTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public RecordFactoryGenerationTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region Simple Record Tests

    [Fact]
    public void Record_Simple_FactoryCanBeResolved()
    {
        // Arrange & Act
        var factory = _provider.GetService<ISimpleRecordFactory>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Record_Simple_CreateMethodGeneratedFromPrimaryConstructor()
    {
        // Arrange
        var factory = _provider.GetRequiredService<ISimpleRecordFactory>();

        // Act - Create method should match primary constructor parameters
        var record = factory.Create("TestName", 42);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestName", record.Name);
        Assert.Equal(42, record.Value);
    }

    #endregion

    #region Record With Service Tests

    [Fact]
    public void Record_WithService_FactoryCanBeResolved()
    {
        // Arrange & Act
        var factory = _provider.GetService<IRecordWithServiceFactory>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Record_WithService_ServiceIsInjected()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithServiceFactory>();

        // Act - Service parameter should NOT be in the factory method signature
        // It should be injected automatically
        var record = factory.Create("TestName");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestName", record.Name);
        Assert.NotNull(record.Service); // Service was injected
    }

    #endregion

    #region Record With Nullable Tests

    [Fact]
    public void Record_WithNullable_FactoryCanBeResolved()
    {
        // Arrange & Act
        var factory = _provider.GetService<IRecordWithNullableFactory>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Record_WithNullable_CreateWithNullValue()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithNullableFactory>();

        // Act
        var record = factory.Create("TestName", null);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestName", record.Name);
        Assert.Null(record.Description);
    }

    [Fact]
    public void Record_WithNullable_CreateWithNonNullValue()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithNullableFactory>();

        // Act
        var record = factory.Create("TestName", "TestDescription");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestName", record.Name);
        Assert.Equal("TestDescription", record.Description);
    }

    #endregion

    #region Record With Collection Tests

    [Fact]
    public void Record_WithCollection_FactoryCanBeResolved()
    {
        // Arrange & Act
        var factory = _provider.GetService<IRecordWithCollectionFactory>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Record_WithCollection_CreateWithItems()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithCollectionFactory>();
        var items = new List<string> { "A", "B", "C" };

        // Act
        var record = factory.Create("TestName", items);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestName", record.Name);
        Assert.Equal(items, record.Items);
    }

    #endregion

    #region Record With Defaults Tests

    [Fact]
    public void Record_WithDefaults_FactoryCanBeResolved()
    {
        // Arrange & Act
        var factory = _provider.GetService<IRecordWithDefaultsFactory>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Record_WithDefaults_CreateRequiresAllParameters()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithDefaultsFactory>();

        // Act - Factory method requires all parameters (defaults are not preserved in generated interface)
        var record = factory.Create("Custom", 100);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("Custom", record.Name);
        Assert.Equal(100, record.Value);
    }

    #endregion

    #region Nested Record Tests

    [Fact]
    public void Record_Nested_BothFactoriesCanBeResolved()
    {
        // Arrange & Act
        var outerFactory = _provider.GetService<IOuterRecordFactory>();
        var innerFactory = _provider.GetService<IInnerRecordFactory>();

        // Assert
        Assert.NotNull(outerFactory);
        Assert.NotNull(innerFactory);
    }

    [Fact]
    public void Record_Nested_CreateWithNestedRecord()
    {
        // Arrange
        var outerFactory = _provider.GetRequiredService<IOuterRecordFactory>();
        var innerFactory = _provider.GetRequiredService<IInnerRecordFactory>();

        // Act
        var inner = innerFactory.Create("InnerValue");
        var outer = outerFactory.Create("OuterName", inner);

        // Assert
        Assert.NotNull(outer);
        Assert.Equal("OuterName", outer.Name);
        Assert.NotNull(outer.Inner);
        Assert.Equal("InnerValue", outer.Inner.InnerValue);
    }

    #endregion

    #region Record With Fetch Tests

    [Fact]
    public void Record_WithFetch_FactoryCanBeResolved()
    {
        // Arrange & Act
        var factory = _provider.GetService<IRecordWithFetchFactory>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Record_WithFetch_FetchMethodGenerated()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithFetchFactory>();

        // Act
        var record = factory.FetchById(123);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("Fetched-123", record.Name);
    }

    [Fact]
    public void Record_WithFetch_CreateStillWorks()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithFetchFactory>();

        // Act
        var record = factory.Create("DirectCreate");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("DirectCreate", record.Name);
    }

    #endregion

    #region Record With Remote Fetch Tests

    [Fact]
    public void Record_WithRemoteFetch_FactoryCanBeResolved()
    {
        // Arrange & Act
        var factory = _provider.GetService<IRecordWithRemoteFetchFactory>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public async Task Record_WithRemoteFetch_RemoteMethodWorksInServerMode()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithRemoteFetchFactory>();

        // Act - In Server mode, [Remote] methods execute directly
        var record = await factory.RemoteFetch("Test");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("Remote-Test", record.Name);
    }

    [Fact]
    public async Task Record_WithRemoteFetch_AsyncRemoteMethodWorksInServerMode()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IRecordWithRemoteFetchFactory>();

        // Act - Async remote method should work in Server mode
        var record = await factory.RemoteFetchAsync("AsyncTest");

        // Assert
        Assert.NotNull(record);
        Assert.Equal("RemoteAsync-AsyncTest", record.Name);
    }

    #endregion

    #region Complex Record Tests

    [Fact]
    public void Record_Complex_FactoryCanBeResolved()
    {
        // Arrange & Act
        var factory = _provider.GetService<IComplexRecordFactory>();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Record_Complex_CreateWithAllParameters()
    {
        // Arrange
        var factory = _provider.GetRequiredService<IComplexRecordFactory>();
        var testDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var testGuid = Guid.NewGuid();

        // Act
        var record = factory.Create(
            "TestString",
            42,
            9876543210L,
            3.14159,
            99.99m,
            true,
            testDate,
            testGuid);

        // Assert
        Assert.NotNull(record);
        Assert.Equal("TestString", record.StringProp);
        Assert.Equal(42, record.IntProp);
        Assert.Equal(9876543210L, record.LongProp);
        Assert.Equal(3.14159, record.DoubleProp, precision: 5);
        Assert.Equal(99.99m, record.DecimalProp);
        Assert.True(record.BoolProp);
        Assert.Equal(testDate, record.DateTimeProp);
        Assert.Equal(testGuid, record.GuidProp);
    }

    #endregion
}
