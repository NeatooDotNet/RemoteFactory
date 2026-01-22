using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.UnitTests.TestTargets.Core;

namespace RemoteFactory.UnitTests.FactoryGenerator.Core;

/// <summary>
/// Tests for FactoryCore async method overrides.
/// Custom IFactoryCore implementations can override async methods to add
/// logging, tracking, or modify behavior before/after factory method calls.
/// </summary>
public class FactoryCoreAsyncTests
{
    #region DoFactoryMethodCallAsync Tests for Read Operations

    public class DoFactoryMethodCallAsyncReadTests
    {
        private readonly IServiceScope _scope;
        private readonly TrackingAsyncFactoryCore_Read _trackingCore;

        public DoFactoryMethodCallAsyncReadTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget_Read).Assembly);

            // Register custom FactoryCore
            _trackingCore = new TrackingAsyncFactoryCore_Read();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget_Read>>(_trackingCore);
            services.AddScoped<AsyncCoreTarget_Read>();

            var provider = services.BuildServiceProvider();
            _scope = provider.CreateScope();
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncCreate()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_ReadFactory>();

            // Act
            var result = await factory.CreateAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Create, _trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncFetch()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_ReadFactory>();

            // Act
            var result = await factory.FetchAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Fetch, _trackingCore.LastOperation);
        }
    }

    #endregion

    #region DoFactoryMethodCallBoolAsync Tests for Read Operations

    public class DoFactoryMethodCallBoolAsyncReadTests
    {
        private readonly IServiceScope _scope;
        private readonly TrackingAsyncFactoryCore_Read _trackingCore;

        public DoFactoryMethodCallBoolAsyncReadTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget_Read).Assembly);

            // Register custom FactoryCore
            _trackingCore = new TrackingAsyncFactoryCore_Read();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget_Read>>(_trackingCore);
            services.AddScoped<AsyncCoreTarget_Read>();

            var provider = services.BuildServiceProvider();
            _scope = provider.CreateScope();
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolCreate()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_ReadFactory>();

            // Act
            var result = await factory.CreateBoolAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.CreateCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Create, _trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolFetch()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_ReadFactory>();

            // Act
            var result = await factory.FetchBoolAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.FetchCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Fetch, _trackingCore.LastOperation);
        }
    }

    #endregion

    #region Async Write Operation Override Tests

    public class AsyncWriteOverrideTests
    {
        private readonly IServiceScope _scope;
        private readonly TrackingAsyncFactoryCore_Write _trackingCore;

        public AsyncWriteOverrideTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget_Write).Assembly);

            // Register custom FactoryCore
            _trackingCore = new TrackingAsyncFactoryCore_Write();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget_Write>>(_trackingCore);
            services.AddScoped<AsyncCoreTarget_Write>();

            var provider = services.BuildServiceProvider();
            _scope = provider.CreateScope();
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncInsert()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_WriteFactory>();
            var target = factory.Create();
            target.IsNew = true;

            // Act - SaveAsync routes to Insert when IsNew=true
            var result = await factory.SaveAsync(target);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Insert, _trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncUpdate()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_WriteFactory>();
            var target = factory.Create();
            target.IsNew = false;

            // Act - SaveAsync routes to Update when IsNew=false and IsDeleted=false
            var result = await factory.SaveAsync(target);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Update, _trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncDelete()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_WriteFactory>();
            var target = factory.Create();
            target.IsDeleted = true;

            // Act - SaveAsync routes to Delete when IsDeleted=true
            var result = await factory.SaveAsync(target);

            // Assert - Task Delete returns the object (only bool Delete returning false returns null)
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Delete, _trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolInsert()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_WriteFactory>();
            var target = factory.Create();
            target.IsNew = true;

            // Act - SaveBoolAsync routes to InsertBoolAsync when IsNew=true
            var result = await factory.SaveBoolAsync(target);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Insert, _trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolUpdate()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_WriteFactory>();
            var target = factory.Create();
            target.IsNew = false;

            // Act - SaveBoolAsync routes to UpdateBoolAsync when IsNew=false
            var result = await factory.SaveBoolAsync(target);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Update, _trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolDelete()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_WriteFactory>();
            var target = factory.Create();
            target.IsDeleted = true;

            // Act - SaveBoolAsync routes to DeleteBoolAsync when IsDeleted=true
            var result = await factory.SaveBoolAsync(target);

            // Assert - DeleteBoolAsync returns true so result is not null
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
            Assert.True(_trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Delete, _trackingCore.LastOperation);
        }
    }

    #endregion

    #region Wrapping Behavior Tests

    public class WrappingBehaviorTests
    {
        private readonly IServiceScope _scope;
        private readonly WrappingAsyncFactoryCore_Read _wrappingCore;

        public WrappingBehaviorTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget_Read).Assembly);

            // Register custom FactoryCore
            _wrappingCore = new WrappingAsyncFactoryCore_Read();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget_Read>>(_wrappingCore);
            services.AddScoped<AsyncCoreTarget_Read>();

            var provider = services.BuildServiceProvider();
            _scope = provider.CreateScope();
        }

        [Fact]
        public async Task Override_ExecutesBeforeAndAfter_ForAsyncCreate()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_ReadFactory>();

            // Act
            var result = await factory.CreateAsync();

            // Assert
            Assert.True(_wrappingCore.BeforeCallExecuted);
            Assert.True(_wrappingCore.AfterCallExecuted);
            Assert.Same(result, _wrappingCore.LastResult);
        }

        [Fact]
        public async Task Override_ExecutesBeforeAndAfter_ForAsyncFetch()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_ReadFactory>();

            // Act
            var result = await factory.FetchAsync();

            // Assert
            Assert.True(_wrappingCore.BeforeCallExecuted);
            Assert.True(_wrappingCore.AfterCallExecuted);
            Assert.Same(result, _wrappingCore.LastResult);
        }
    }

    #endregion

    #region Multiple Override Calls

    public class MultipleOverrideCallsTests
    {
        private readonly IServiceScope _scope;
        private readonly TrackingAsyncFactoryCore_Read _trackingCore;

        public MultipleOverrideCallsTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget_Read).Assembly);

            // Register custom FactoryCore
            _trackingCore = new TrackingAsyncFactoryCore_Read();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget_Read>>(_trackingCore);
            services.AddScoped<AsyncCoreTarget_Read>();

            var provider = services.BuildServiceProvider();
            _scope = provider.CreateScope();
        }

        [Fact]
        public async Task Override_TracksMultipleCalls()
        {
            // Arrange
            var factory = _scope.ServiceProvider.GetRequiredService<IAsyncCoreTarget_ReadFactory>();

            // Act - Make multiple calls
            await factory.CreateAsync();
            await factory.FetchAsync();
            await factory.CreateAsync();

            // Assert
            Assert.Equal(3, _trackingCore.CallCount);
        }
    }

    #endregion
}
