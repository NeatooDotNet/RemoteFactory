using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.SpecificSenarios;

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
                this.CreateCalled = true;
            }

            [Fetch]
            public void Fetch()
            {
                this.FetchCalled = true;
            }
        }
    }

    public class SimpleNestedFactoryTests : FactoryTestBase<ISimpleNestedFactoryFactory>
    {
        [Fact]
        public void SimpleNestedFactory_Create_Works()
        {
            // Act
            var result = this.factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public void SimpleNestedFactory_Fetch_Works()
        {
            // Act
            var result = this.factory.Fetch();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }

        [Fact]
        public void SimpleNestedFactory_FactoryInterface_IsRegistered()
        {
            // Assert - Factory was resolved via base class, proving registration works
            Assert.NotNull(this.factory);
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
                    this.CreateCalled = true;
                }

                [Fetch]
                public void Fetch()
                {
                    this.FetchCalled = true;
                }
            }
        }
    }

    public class DeepNestedFactoryTests : FactoryTestBase<IDeepNestedFactoryFactory>
    {
        [Fact]
        public void DeepNestedFactory_Create_Works()
        {
            // Act
            var result = this.factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public void DeepNestedFactory_Fetch_Works()
        {
            // Act
            var result = this.factory.Fetch();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }

        [Fact]
        public void DeepNestedFactory_FactoryInterface_IsRegistered()
        {
            // Assert - Factory was resolved via base class, proving registration works
            Assert.NotNull(this.factory);
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
                this.CreateCalled = true;
            }

            [Fetch]
            public void Fetch()
            {
                this.FetchCalled = true;
            }

            [Insert]
            public void Insert()
            {
                this.InsertCalled = true;
            }

            [Update]
            public void Update()
            {
                this.UpdateCalled = true;
            }

            [Delete]
            public void Delete()
            {
                this.DeleteCalled = true;
            }
        }
    }

    public class NestedAllOperationsTests : FactoryTestBase<INestedAllOperationsFactory>
    {
        [Fact]
        public void NestedAllOperations_Create_Works()
        {
            // Act
            var result = this.factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public void NestedAllOperations_Fetch_Works()
        {
            // Act
            var result = this.factory.Fetch();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }

        [Fact]
        public void NestedAllOperations_Save_RoutesToInsert_WhenIsNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.IsDeleted = false;

            // Act
            var result = this.factory.Save(obj);

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
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act
            var result = this.factory.Save(obj);

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
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = true;

            // Act
            var result = this.factory.Save(obj);

            // Assert - void Delete returns the object (only bool Delete returning false returns null)
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
        }
    }

    #endregion

    #region Nested Class with Remote Operations

    /// <summary>
    /// Outer container for nested factory with remote operations.
    /// </summary>
    public class RemoteOuter
    {
        /// <summary>
        /// Nested factory with [Remote] operations.
        /// </summary>
        [Factory]
        public class NestedRemoteFactory : IFactorySaveMeta
        {
            public bool IsDeleted { get; set; }
            public bool IsNew { get; set; }

            public bool CreateCalled { get; set; }
            public bool FetchCalled { get; set; }
            public bool InsertCalled { get; set; }
            public bool UpdateCalled { get; set; }

            [Create]
            public NestedRemoteFactory()
            {
                this.CreateCalled = true;
            }

            [Fetch]
            [Remote]
            public Task FetchAsync()
            {
                this.FetchCalled = true;
                return Task.CompletedTask;
            }

            [Insert]
            [Remote]
            public Task InsertAsync()
            {
                this.InsertCalled = true;
                return Task.CompletedTask;
            }

            [Update]
            [Remote]
            public Task UpdateAsync()
            {
                this.UpdateCalled = true;
                return Task.CompletedTask;
            }
        }
    }

    public class NestedRemoteFactoryTests : FactoryTestBase<INestedRemoteFactoryFactory>
    {
        [Fact]
        public void NestedRemoteFactory_Create_WorksLocally()
        {
            // Act
            var result = this.factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public async Task NestedRemoteFactory_Fetch_WorksRemotely()
        {
            // Act
            var result = await this.factory.FetchAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }

        [Fact]
        public async Task NestedRemoteFactory_Save_RoutesToInsert_WhenIsNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.IsDeleted = false;

            // Act - SaveAsync routes to InsertAsync when IsNew=true
            var result = await this.factory.SaveAsync(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
        }

        [Fact]
        public async Task NestedRemoteFactory_Save_RoutesToUpdate_WhenNotNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act - SaveAsync routes to UpdateAsync when IsNew=false
            var result = await this.factory.SaveAsync(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
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
                this.CreateCalled = true;
                this.ServiceWasInjected = true;
            }
        }
    }

    public class NestedServiceFactoryTests : FactoryTestBase<INestedServiceFactoryFactory>
    {
        [Fact]
        public void NestedServiceFactory_Create_Works()
        {
            // Act
            var result = this.factory.Create();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void NestedServiceFactory_CreateWithService_InjectsService()
        {
            // Act
            var result = this.factory.CreateWithService();

            // Assert
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
                this.CreateCalled = true;
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
                this.CreateCalled = true;
            }
        }
    }

    public class FirstNestedFactoryTests : FactoryTestBase<IFirstNestedFactoryFactory>
    {
        [Fact]
        public void FirstNestedFactory_Create_Works()
        {
            // Act
            var result = this.factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }
    }

    public class SecondNestedFactoryTests : FactoryTestBase<ISecondNestedFactoryFactory>
    {
        [Fact]
        public void SecondNestedFactory_Create_Works()
        {
            // Act
            var result = this.factory.Create();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }
    }

    public class MultipleNestedFactoriesTests
    {
        private readonly IServiceScope scope;

        public MultipleNestedFactoriesTests()
        {
            var scopes = ClientServerContainers.Scopes();
            this.scope = scopes.client;
        }

        [Fact]
        public void MultipleNestedFactories_BothAreRegistered()
        {
            // Act
            var firstFactory = this.scope.ServiceProvider.GetService<IFirstNestedFactoryFactory>();
            var secondFactory = this.scope.ServiceProvider.GetService<ISecondNestedFactoryFactory>();

            // Assert
            Assert.NotNull(firstFactory);
            Assert.NotNull(secondFactory);
        }

        [Fact]
        public void MultipleNestedFactories_AreDifferentInstances()
        {
            // Act
            var first = this.scope.ServiceProvider.GetRequiredService<IFirstNestedFactoryFactory>().Create();
            var second = this.scope.ServiceProvider.GetRequiredService<ISecondNestedFactoryFactory>().Create();

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
                this.CreateCalled = true;
                return Task.CompletedTask;
            }

            [Fetch]
            public Task FetchAsync()
            {
                this.FetchCalled = true;
                return Task.CompletedTask;
            }

            [Fetch]
            public Task<bool> FetchBoolAsync()
            {
                this.FetchCalled = true;
                return Task.FromResult(true);
            }
        }
    }

    public class NestedAsyncFactoryTests : FactoryTestBase<INestedAsyncFactoryFactory>
    {
        [Fact]
        public async Task NestedAsyncFactory_CreateAsync_Works()
        {
            // Act
            var result = await this.factory.CreateAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public async Task NestedAsyncFactory_FetchAsync_Works()
        {
            // Act
            var result = await this.factory.FetchAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }

        [Fact]
        public async Task NestedAsyncFactory_FetchBoolAsync_Works()
        {
            // Act
            var result = await this.factory.FetchBoolAsync();

            // Assert
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
                this.CreateParam = param;
            }

            [Fetch]
            public void Fetch(string param)
            {
                this.FetchParam = param;
            }

            [Fetch]
            public void Fetch(int intParam, string stringParam)
            {
                this.CreateParam = intParam;
                this.FetchParam = stringParam;
            }
        }
    }

    public class NestedParamFactoryTests : FactoryTestBase<INestedParamFactoryFactory>
    {
        [Fact]
        public void NestedParamFactory_Create_Works()
        {
            // Act
            var result = this.factory.Create();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void NestedParamFactory_Create_WithIntParam()
        {
            // Arrange
            const int expectedParam = 42;

            // Act
            var result = this.factory.Create(expectedParam);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedParam, result.CreateParam);
        }

        [Fact]
        public void NestedParamFactory_Fetch_WithStringParam()
        {
            // Arrange
            const string expectedParam = "test-value";

            // Act
            var result = this.factory.Fetch(expectedParam);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedParam, result.FetchParam);
        }

        [Fact]
        public void NestedParamFactory_Fetch_WithMultipleParams()
        {
            // Arrange
            const int expectedInt = 123;
            const string expectedString = "multi-param";

            // Act
            var result = this.factory.Fetch(expectedInt, expectedString);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedInt, result.CreateParam);
            Assert.Equal(expectedString, result.FetchParam);
        }
    }

    #endregion
}
