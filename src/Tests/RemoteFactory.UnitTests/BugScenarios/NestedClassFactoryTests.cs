using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.BugScenarios;

/// <summary>
/// Tests for [Factory] attribute on nested classes (GAP-009).
/// The generator should properly handle nested class hierarchies,
/// generate correct factory interfaces with proper naming,
/// and ensure DI registration works correctly.
/// </summary>
public class NestedClassFactoryTests
{
    #region Simple Nested Class (One Level Deep)

    /// <summary>
    /// Outer container class for simple nested factory.
    /// </summary>
    public class SimpleOuterClass
    {
        /// <summary>
        /// Factory class nested one level deep.
        /// </summary>
        [Factory]
        public class SimpleNestedFactory
        {
            public bool CreateCalled { get; set; }
            public bool FetchCalled { get; set; }

            [Create]
            public SimpleNestedFactory()
            {
                CreateCalled = true;
            }

            [Fetch]
            public void Fetch()
            {
                FetchCalled = true;
            }
        }
    }

    public class SimpleNestedFactoryTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ISimpleNestedFactoryFactory _factory;

        public SimpleNestedFactoryTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<ISimpleNestedFactoryFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void SimpleNestedFactory_Create_Works()
        {
            // Act
            var result = _factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public void SimpleNestedFactory_Fetch_Works()
        {
            // Act
            var result = _factory.Fetch();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }

        [Fact]
        public void SimpleNestedFactory_FactoryInterface_IsRegistered()
        {
            // Assert - Factory was resolved, proving registration works
            Assert.NotNull(_factory);
        }
    }

    #endregion

    #region Deeply Nested Class (Multiple Levels)

    /// <summary>
    /// Outer container for deeply nested factory.
    /// </summary>
    public class DeepOuter
    {
        /// <summary>
        /// Middle level container.
        /// </summary>
        public class DeepMiddle
        {
            /// <summary>
            /// Factory class nested two levels deep.
            /// </summary>
            [Factory]
            public class DeepNestedFactory
            {
                public bool CreateCalled { get; set; }
                public bool FetchCalled { get; set; }

                [Create]
                public DeepNestedFactory()
                {
                    CreateCalled = true;
                }

                [Fetch]
                public void Fetch()
                {
                    FetchCalled = true;
                }
            }
        }
    }

    public class DeepNestedFactoryTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly IDeepNestedFactoryFactory _factory;

        public DeepNestedFactoryTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<IDeepNestedFactoryFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void DeepNestedFactory_Create_Works()
        {
            // Act
            var result = _factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public void DeepNestedFactory_Fetch_Works()
        {
            // Act
            var result = _factory.Fetch();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }
    }

    #endregion

    #region Nested Class with All Operations

    /// <summary>
    /// Outer container for nested factory with all operations.
    /// </summary>
    public class AllOpsOuter
    {
        /// <summary>
        /// Nested factory with Create, Fetch, Insert, Update, Delete.
        /// </summary>
        [Factory]
        public class NestedAllOperations : IFactorySaveMeta
        {
            public bool IsDeleted { get; set; }
            public bool IsNew { get; set; }

            public bool CreateCalled { get; set; }
            public bool FetchCalled { get; set; }
            public bool InsertCalled { get; set; }
            public bool UpdateCalled { get; set; }
            public bool DeleteCalled { get; set; }

            [Create]
            public NestedAllOperations()
            {
                CreateCalled = true;
            }

            [Fetch]
            public void Fetch()
            {
                FetchCalled = true;
            }

            [Insert]
            public void Insert()
            {
                InsertCalled = true;
            }

            [Update]
            public void Update()
            {
                UpdateCalled = true;
            }

            [Delete]
            public void Delete()
            {
                DeleteCalled = true;
            }
        }
    }

    public class NestedAllOperationsTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly INestedAllOperationsFactory _factory;

        public NestedAllOperationsTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<INestedAllOperationsFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void NestedAllOperations_Create_Works()
        {
            var result = _factory.Create();

            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public void NestedAllOperations_Fetch_Works()
        {
            var result = _factory.Fetch();

            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }

        [Fact]
        public void NestedAllOperations_Save_RoutesToInsert_WhenIsNew()
        {
            // Arrange
            var obj = _factory.Create();
            obj.IsNew = true;
            obj.IsDeleted = false;

            // Act
            var result = _factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
            Assert.False(result.UpdateCalled);
            Assert.False(result.DeleteCalled);
        }

        [Fact]
        public void NestedAllOperations_Save_RoutesToUpdate_WhenNotNew()
        {
            // Arrange
            var obj = _factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act
            var result = _factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.False(result!.InsertCalled);
            Assert.True(result.UpdateCalled);
            Assert.False(result.DeleteCalled);
        }

        [Fact]
        public void NestedAllOperations_Save_RoutesToDelete_WhenIsDeleted()
        {
            // Arrange
            var obj = _factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = true;

            // Act
            var result = _factory.Save(obj);

            // Assert - void Delete returns the object (only bool Delete returning false returns null)
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
        }
    }

    #endregion

    #region Nested Class with Service Parameters

    /// <summary>
    /// Outer container for nested factory with service injection.
    /// </summary>
    public class ServiceOuter
    {
        /// <summary>
        /// Nested factory with [Service] parameter injection.
        /// </summary>
        [Factory]
        public class NestedServiceFactory
        {
            public bool CreateCalled { get; set; }
            public bool ServiceWasInjected { get; set; }

            [Create]
            public NestedServiceFactory() { }

            [Create]
            public void CreateWithService([Service] IService service)
            {
                ArgumentNullException.ThrowIfNull(service);
                CreateCalled = true;
                ServiceWasInjected = true;
            }
        }
    }

    public class NestedServiceFactoryTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly INestedServiceFactoryFactory _factory;

        public NestedServiceFactoryTests()
        {
            _provider = new ServerContainerBuilder()
                .WithService<IService, Service>()
                .Build();
            _factory = _provider.GetRequiredService<INestedServiceFactoryFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void NestedServiceFactory_Create_Works()
        {
            var result = _factory.Create();

            Assert.NotNull(result);
        }

        [Fact]
        public void NestedServiceFactory_CreateWithService_InjectsService()
        {
            var result = _factory.CreateWithService();

            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
            Assert.True(result.ServiceWasInjected);
        }
    }

    #endregion

    #region Multiple Nested Classes in Same Outer

    /// <summary>
    /// Outer container with multiple nested factories.
    /// </summary>
    public class MultiNestedOuter
    {
        /// <summary>
        /// First nested factory.
        /// </summary>
        [Factory]
        public class FirstNestedFactory
        {
            public bool CreateCalled { get; set; }

            [Create]
            public FirstNestedFactory()
            {
                CreateCalled = true;
            }
        }

        /// <summary>
        /// Second nested factory in the same outer class.
        /// </summary>
        [Factory]
        public class SecondNestedFactory
        {
            public bool CreateCalled { get; set; }

            [Create]
            public SecondNestedFactory()
            {
                CreateCalled = true;
            }
        }
    }

    public class FirstNestedFactoryTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly IFirstNestedFactoryFactory _factory;

        public FirstNestedFactoryTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<IFirstNestedFactoryFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void FirstNestedFactory_Create_Works()
        {
            var result = _factory.Create();

            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }
    }

    public class SecondNestedFactoryTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ISecondNestedFactoryFactory _factory;

        public SecondNestedFactoryTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<ISecondNestedFactoryFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void SecondNestedFactory_Create_Works()
        {
            var result = _factory.Create();

            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }
    }

    public class MultipleNestedFactoriesTests : IDisposable
    {
        private readonly IServiceProvider _provider;

        public MultipleNestedFactoriesTests()
        {
            _provider = new ServerContainerBuilder().Build();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void MultipleNestedFactories_BothAreRegistered()
        {
            // Act
            var firstFactory = _provider.GetService<IFirstNestedFactoryFactory>();
            var secondFactory = _provider.GetService<ISecondNestedFactoryFactory>();

            // Assert
            Assert.NotNull(firstFactory);
            Assert.NotNull(secondFactory);
        }

        [Fact]
        public void MultipleNestedFactories_AreDifferentInstances()
        {
            // Act
            var first = _provider.GetRequiredService<IFirstNestedFactoryFactory>().Create();
            var second = _provider.GetRequiredService<ISecondNestedFactoryFactory>().Create();

            // Assert
            Assert.IsType<MultiNestedOuter.FirstNestedFactory>(first);
            Assert.IsType<MultiNestedOuter.SecondNestedFactory>(second);
        }
    }

    #endregion

    #region Nested Class with Async Operations

    /// <summary>
    /// Outer container for nested factory with async operations.
    /// </summary>
    public class AsyncOuter
    {
        /// <summary>
        /// Nested factory with async Create and Fetch.
        /// </summary>
        [Factory]
        public class NestedAsyncFactory
        {
            public bool CreateCalled { get; set; }
            public bool FetchCalled { get; set; }

            [Create]
            public Task CreateAsync()
            {
                CreateCalled = true;
                return Task.CompletedTask;
            }

            [Fetch]
            public Task FetchAsync()
            {
                FetchCalled = true;
                return Task.CompletedTask;
            }

            [Fetch]
            public Task<bool> FetchBoolAsync()
            {
                FetchCalled = true;
                return Task.FromResult(true);
            }
        }
    }

    public class NestedAsyncFactoryTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly INestedAsyncFactoryFactory _factory;

        public NestedAsyncFactoryTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<INestedAsyncFactoryFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public async Task NestedAsyncFactory_CreateAsync_Works()
        {
            var result = await _factory.CreateAsync();

            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public async Task NestedAsyncFactory_FetchAsync_Works()
        {
            var result = await _factory.FetchAsync();

            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }

        [Fact]
        public async Task NestedAsyncFactory_FetchBoolAsync_Works()
        {
            var result = await _factory.FetchBoolAsync();

            Assert.NotNull(result);
            Assert.True(result!.FetchCalled);
        }
    }

    #endregion

    #region Nested Class with Parameters

    /// <summary>
    /// Outer container for nested factory with parameters.
    /// </summary>
    public class ParamOuter
    {
        /// <summary>
        /// Nested factory with parameterized methods.
        /// </summary>
        [Factory]
        public class NestedParamFactory
        {
            public int? CreateParam { get; set; }
            public string? FetchParam { get; set; }

            [Create]
            public NestedParamFactory() { }

            [Create]
            public void Create(int param)
            {
                CreateParam = param;
            }

            [Fetch]
            public void Fetch(string param)
            {
                FetchParam = param;
            }

            [Fetch]
            public void Fetch(int intParam, string stringParam)
            {
                CreateParam = intParam;
                FetchParam = stringParam;
            }
        }
    }

    public class NestedParamFactoryTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly INestedParamFactoryFactory _factory;

        public NestedParamFactoryTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<INestedParamFactoryFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void NestedParamFactory_Create_Works()
        {
            var result = _factory.Create();

            Assert.NotNull(result);
        }

        [Fact]
        public void NestedParamFactory_Create_WithIntParam()
        {
            const int expectedParam = 42;

            var result = _factory.Create(expectedParam);

            Assert.NotNull(result);
            Assert.Equal(expectedParam, result.CreateParam);
        }

        [Fact]
        public void NestedParamFactory_Fetch_WithStringParam()
        {
            const string expectedParam = "test-value";

            var result = _factory.Fetch(expectedParam);

            Assert.NotNull(result);
            Assert.Equal(expectedParam, result.FetchParam);
        }

        [Fact]
        public void NestedParamFactory_Fetch_WithMultipleParams()
        {
            const int expectedInt = 123;
            const string expectedString = "multi-param";

            var result = _factory.Fetch(expectedInt, expectedString);

            Assert.NotNull(result);
            Assert.Equal(expectedInt, result.CreateParam);
            Assert.Equal(expectedString, result.FetchParam);
        }
    }

    #endregion
}
