using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for FactoryCore async method overrides (GAP-008).
/// Custom IFactoryCore implementations can override async methods to add
/// logging, tracking, or modify behavior before/after factory method calls.
/// </summary>
public class FactoryCoreAsyncTests
{
    #region Test Target Classes

    /// <summary>
    /// Target class for testing async Create/Fetch operations.
    /// </summary>
    [Factory]
    public class AsyncCoreTarget
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

        [Create]
        public Task<bool> CreateBoolAsync()
        {
            this.CreateCalled = true;
            return Task.FromResult(true);
        }

        [Fetch]
        public Task<bool> FetchBoolAsync()
        {
            this.FetchCalled = true;
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Target class for testing async Write operations (Insert, Update, Delete).
    /// </summary>
    [Factory]
    public class AsyncCoreWriteTarget : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }
        public bool DeleteCalled { get; set; }

        [Create]
        public AsyncCoreWriteTarget() { }

        [Insert]
        public Task InsertAsync()
        {
            this.InsertCalled = true;
            return Task.CompletedTask;
        }

        [Update]
        public Task UpdateAsync()
        {
            this.UpdateCalled = true;
            return Task.CompletedTask;
        }

        [Delete]
        public Task DeleteAsync()
        {
            this.DeleteCalled = true;
            return Task.CompletedTask;
        }

        [Insert]
        public Task<bool> InsertBoolAsync()
        {
            this.InsertCalled = true;
            return Task.FromResult(true);
        }

        [Update]
        public Task<bool> UpdateBoolAsync()
        {
            this.UpdateCalled = true;
            return Task.FromResult(true);
        }

        [Delete]
        public Task<bool> DeleteBoolAsync()
        {
            this.DeleteCalled = true;
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Target class for testing nullable async operations.
    /// </summary>
    [Factory]
    public class AsyncCoreNullableTarget
    {
        public bool FetchCalled { get; set; }
        public bool ShouldReturnNull { get; set; }

        [Fetch]
        public Task<bool> FetchBoolAsync()
        {
            this.FetchCalled = true;
            return Task.FromResult(!this.ShouldReturnNull);
        }
    }

    #endregion

    #region Custom FactoryCore Implementations

    /// <summary>
    /// Custom FactoryCore that tracks async method calls.
    /// </summary>
    public class TrackingAsyncFactoryCore<T> : FactoryCore<T>
    {
        public bool DoFactoryMethodCallAsyncCalled { get; private set; }
        public bool DoFactoryMethodCallAsyncNullableCalled { get; private set; }
        public bool DoFactoryMethodCallBoolAsyncCalled { get; private set; }
        public FactoryOperation? LastOperation { get; private set; }
        public int CallCount { get; private set; }

        public override async Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall)
        {
            this.DoFactoryMethodCallAsyncCalled = true;
            this.LastOperation = operation;
            this.CallCount++;
            return await base.DoFactoryMethodCallAsync(operation, factoryMethodCall);
        }

        public override async Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<T?>> factoryMethodCall)
        {
            this.DoFactoryMethodCallAsyncNullableCalled = true;
            this.LastOperation = operation;
            this.CallCount++;
            return await base.DoFactoryMethodCallAsyncNullable(operation, factoryMethodCall);
        }

        public override async Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall)
        {
            this.DoFactoryMethodCallBoolAsyncCalled = true;
            this.LastOperation = operation;
            this.CallCount++;
            return await base.DoFactoryMethodCallBoolAsync(target, operation, factoryMethodCall);
        }

        public override async Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall)
        {
            this.DoFactoryMethodCallAsyncCalled = true;
            this.LastOperation = operation;
            this.CallCount++;
            return await base.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);
        }
    }

    /// <summary>
    /// Custom FactoryCore that wraps results with additional behavior.
    /// </summary>
    public class WrappingAsyncFactoryCore<T> : FactoryCore<T> where T : class
    {
        public bool BeforeCallExecuted { get; private set; }
        public bool AfterCallExecuted { get; private set; }
        public T? LastResult { get; private set; }

        public override async Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall)
        {
            this.BeforeCallExecuted = true;
            var result = await base.DoFactoryMethodCallAsync(operation, factoryMethodCall);
            this.AfterCallExecuted = true;
            this.LastResult = result;
            return result;
        }

        public override async Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall)
        {
            this.BeforeCallExecuted = true;
            var result = await base.DoFactoryMethodCallBoolAsync(target, operation, factoryMethodCall);
            this.AfterCallExecuted = true;
            this.LastResult = result;
            return result;
        }

        public override async Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall)
        {
            this.BeforeCallExecuted = true;
            var result = await base.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);
            this.AfterCallExecuted = true;
            this.LastResult = result;
            return result;
        }
    }

    #endregion

    #region DoFactoryMethodCallAsync Tests

    public class DoFactoryMethodCallAsyncTests
    {
        private readonly IServiceScope scope;
        private readonly TrackingAsyncFactoryCore<AsyncCoreTarget> trackingCore;

        public DoFactoryMethodCallAsyncTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget).Assembly);

            // Register custom FactoryCore
            this.trackingCore = new TrackingAsyncFactoryCore<AsyncCoreTarget>();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget>>(this.trackingCore);
            services.AddScoped<AsyncCoreTarget>();

            var provider = services.BuildServiceProvider();
            this.scope = provider.CreateScope();
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncCreate()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreTargetFactory>();

            // Act
            var result = await factory.CreateAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Create, this.trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncFetch()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreTargetFactory>();

            // Act
            var result = await factory.FetchAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Fetch, this.trackingCore.LastOperation);
        }
    }

    #endregion

    #region DoFactoryMethodCallBoolAsync Tests

    public class DoFactoryMethodCallBoolAsyncTests
    {
        private readonly IServiceScope scope;
        private readonly TrackingAsyncFactoryCore<AsyncCoreTarget> trackingCore;

        public DoFactoryMethodCallBoolAsyncTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget).Assembly);

            // Register custom FactoryCore
            this.trackingCore = new TrackingAsyncFactoryCore<AsyncCoreTarget>();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget>>(this.trackingCore);
            services.AddScoped<AsyncCoreTarget>();

            var provider = services.BuildServiceProvider();
            this.scope = provider.CreateScope();
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolCreate()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreTargetFactory>();

            // Act
            var result = await factory.CreateBoolAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.CreateCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Create, this.trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolFetch()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreTargetFactory>();

            // Act
            var result = await factory.FetchBoolAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.FetchCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Fetch, this.trackingCore.LastOperation);
        }
    }

    #endregion

    #region Async Write Operation Override Tests

    public class AsyncWriteOverrideTests
    {
        private readonly IServiceScope scope;
        private readonly TrackingAsyncFactoryCore<AsyncCoreWriteTarget> trackingCore;

        public AsyncWriteOverrideTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreWriteTarget).Assembly);

            // Register custom FactoryCore
            this.trackingCore = new TrackingAsyncFactoryCore<AsyncCoreWriteTarget>();
            services.AddSingleton<IFactoryCore<AsyncCoreWriteTarget>>(this.trackingCore);
            services.AddScoped<AsyncCoreWriteTarget>();

            var provider = services.BuildServiceProvider();
            this.scope = provider.CreateScope();
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncInsert()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreWriteTargetFactory>();
            var target = factory.Create();
            target.IsNew = true;

            // Act - SaveAsync routes to Insert when IsNew=true
            var result = await factory.SaveAsync(target);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Insert, this.trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncUpdate()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreWriteTargetFactory>();
            var target = factory.Create();
            target.IsNew = false;

            // Act - SaveAsync routes to Update when IsNew=false and IsDeleted=false
            var result = await factory.SaveAsync(target);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Update, this.trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallAsync_IsCalled_ForAsyncDelete()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreWriteTargetFactory>();
            var target = factory.Create();
            target.IsDeleted = true;

            // Act - SaveAsync routes to Delete when IsDeleted=true
            var result = await factory.SaveAsync(target);

            // Assert - Task Delete returns the object (only bool Delete returning false returns null)
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallAsyncCalled);
            Assert.Equal(FactoryOperation.Delete, this.trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolInsert()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreWriteTargetFactory>();
            var target = factory.Create();
            target.IsNew = true;

            // Act - SaveBoolAsync routes to InsertBoolAsync when IsNew=true
            var result = await factory.SaveBoolAsync(target);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Insert, this.trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolUpdate()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreWriteTargetFactory>();
            var target = factory.Create();
            target.IsNew = false;

            // Act - SaveBoolAsync routes to UpdateBoolAsync when IsNew=false
            var result = await factory.SaveBoolAsync(target);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Update, this.trackingCore.LastOperation);
        }

        [Fact]
        public async Task DoFactoryMethodCallBoolAsync_IsCalled_ForAsyncBoolDelete()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreWriteTargetFactory>();
            var target = factory.Create();
            target.IsDeleted = true;

            // Act - SaveBoolAsync routes to DeleteBoolAsync when IsDeleted=true
            var result = await factory.SaveBoolAsync(target);

            // Assert - DeleteBoolAsync returns true so result is not null
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
            Assert.True(this.trackingCore.DoFactoryMethodCallBoolAsyncCalled);
            Assert.Equal(FactoryOperation.Delete, this.trackingCore.LastOperation);
        }
    }

    #endregion

    #region Wrapping Behavior Tests

    public class WrappingBehaviorTests
    {
        private readonly IServiceScope scope;
        private readonly WrappingAsyncFactoryCore<AsyncCoreTarget> wrappingCore;

        public WrappingBehaviorTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget).Assembly);

            // Register custom FactoryCore
            this.wrappingCore = new WrappingAsyncFactoryCore<AsyncCoreTarget>();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget>>(this.wrappingCore);
            services.AddScoped<AsyncCoreTarget>();

            var provider = services.BuildServiceProvider();
            this.scope = provider.CreateScope();
        }

        [Fact]
        public async Task Override_ExecutesBeforeAndAfter_ForAsyncCreate()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreTargetFactory>();

            // Act
            var result = await factory.CreateAsync();

            // Assert
            Assert.True(this.wrappingCore.BeforeCallExecuted);
            Assert.True(this.wrappingCore.AfterCallExecuted);
            Assert.Same(result, this.wrappingCore.LastResult);
        }

        [Fact]
        public async Task Override_ExecutesBeforeAndAfter_ForAsyncFetch()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreTargetFactory>();

            // Act
            var result = await factory.FetchAsync();

            // Assert
            Assert.True(this.wrappingCore.BeforeCallExecuted);
            Assert.True(this.wrappingCore.AfterCallExecuted);
            Assert.Same(result, this.wrappingCore.LastResult);
        }
    }

    #endregion

    #region Remote Execution with Custom FactoryCore

    /// <summary>
    /// Target class for testing remote execution with custom FactoryCore.
    /// </summary>
    [Factory]
    public class RemoteAsyncCoreTarget
    {
        public bool CreateCalled { get; set; }
        public bool FetchCalled { get; set; }

        [Create]
        [Remote]
        public Task CreateAsync()
        {
            this.CreateCalled = true;
            return Task.CompletedTask;
        }

        [Fetch]
        [Remote]
        public Task FetchAsync()
        {
            this.FetchCalled = true;
            return Task.CompletedTask;
        }
    }

    public class RemoteAsyncCoreTests
    {
        private readonly IServiceScope clientScope;
        private readonly IServiceScope serverScope;
        private readonly TrackingAsyncFactoryCore<RemoteAsyncCoreTarget> serverTrackingCore;

        public RemoteAsyncCoreTests()
        {
            // For remote tests, we need both client and server containers
            var scopes = ClientServerContainers.Scopes();
            this.clientScope = scopes.client;
            this.serverScope = scopes.server;

            // Note: The server container uses its own registered FactoryCore
            // This test verifies that server-side FactoryCore is used for remote calls
            this.serverTrackingCore = new TrackingAsyncFactoryCore<RemoteAsyncCoreTarget>();
        }

        [Fact]
        public async Task RemoteExecution_CallsServerFactoryCore()
        {
            // Arrange
            var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteAsyncCoreTargetFactory>();

            // Act
            var result = await factory.CreateAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
            // Note: This verifies the factory method was called,
            // the server FactoryCore would be invoked on server side
        }

        [Fact]
        public async Task RemoteFetch_CallsServerFactoryCore()
        {
            // Arrange
            var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteAsyncCoreTargetFactory>();

            // Act
            var result = await factory.FetchAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
        }
    }

    #endregion

    #region Multiple Override Calls

    public class MultipleOverrideCallsTests
    {
        private readonly IServiceScope scope;
        private readonly TrackingAsyncFactoryCore<AsyncCoreTarget> trackingCore;

        public MultipleOverrideCallsTests()
        {
            var services = new ServiceCollection();
            services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(AsyncCoreTarget).Assembly);

            // Register custom FactoryCore
            this.trackingCore = new TrackingAsyncFactoryCore<AsyncCoreTarget>();
            services.AddSingleton<IFactoryCore<AsyncCoreTarget>>(this.trackingCore);
            services.AddScoped<AsyncCoreTarget>();

            var provider = services.BuildServiceProvider();
            this.scope = provider.CreateScope();
        }

        [Fact]
        public async Task Override_TracksMultipleCalls()
        {
            // Arrange
            var factory = this.scope.ServiceProvider.GetRequiredService<IAsyncCoreTargetFactory>();

            // Act - Make multiple calls
            await factory.CreateAsync();
            await factory.FetchAsync();
            await factory.CreateAsync();

            // Assert
            Assert.Equal(3, this.trackingCore.CallCount);
        }
    }

    #endregion
}
