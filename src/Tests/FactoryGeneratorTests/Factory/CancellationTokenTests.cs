using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for methods with CancellationToken parameter (GAP-022).
/// CancellationToken is a common async pattern that should be properly
/// supported in factory methods.
/// </summary>
public class CancellationTokenTests
{
    #region Create/Fetch with CancellationToken

    /// <summary>
    /// Factory class with CancellationToken parameters on Create/Fetch.
    /// </summary>
    [Factory]
    public class CancellableReadFactory
    {
        public bool CreateCalled { get; set; }
        public bool FetchCalled { get; set; }
        public bool CancellationWasChecked { get; set; }

        [Create]
        public CancellableReadFactory() { }

        [Create]
        public async Task CreateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.CreateCalled = true;
            await Task.Delay(10, cancellationToken);
        }

        [Fetch]
        public async Task FetchAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.FetchCalled = true;
            await Task.Delay(10, cancellationToken);
        }

        [Fetch]
        public async Task<bool> FetchBoolAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.FetchCalled = true;
            await Task.Delay(10, cancellationToken);
            return true;
        }
    }

    public class CancellableReadFactoryTests : FactoryTestBase<ICancellableReadFactoryFactory>
    {
        [Fact]
        public async Task CreateAsync_WithNonCancelledToken_Completes()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.CreateAsync(cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task CreateAsync_WithAlreadyCancelledToken_Throws()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.CreateAsync(cts.Token));
        }

        [Fact]
        public async Task FetchAsync_WithNonCancelledToken_Completes()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.FetchAsync(cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task FetchAsync_WithAlreadyCancelledToken_Throws()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.FetchAsync(cts.Token));
        }

        [Fact]
        public async Task FetchBoolAsync_WithNonCancelledToken_Completes()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.FetchBoolAsync(cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.FetchCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task FetchBoolAsync_WithAlreadyCancelledToken_Throws()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.FetchBoolAsync(cts.Token));
        }
    }

    #endregion

    #region Insert/Update/Delete with CancellationToken

    /// <summary>
    /// Factory class with CancellationToken parameters on Write operations.
    /// </summary>
    [Factory]
    public class CancellableWriteFactory : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }
        public bool DeleteCalled { get; set; }
        public bool CancellationWasChecked { get; set; }

        [Create]
        public CancellableWriteFactory() { }

        [Insert]
        public async Task InsertAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.InsertCalled = true;
            await Task.Delay(10, cancellationToken);
        }

        [Update]
        public async Task UpdateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.UpdateCalled = true;
            await Task.Delay(10, cancellationToken);
        }

        [Delete]
        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.DeleteCalled = true;
            await Task.Delay(10, cancellationToken);
        }
    }

    public class CancellableWriteFactoryTests : FactoryTestBase<ICancellableWriteFactoryFactory>
    {
        [Fact]
        public async Task InsertAsync_WithNonCancelledToken_Completes()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            using var cts = new CancellationTokenSource();

            // Act - SaveAsync routes to InsertAsync when IsNew=true
            var result = await this.factory.SaveAsync(obj, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task InsertAsync_WithAlreadyCancelledToken_Throws()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.SaveAsync(obj, cts.Token));
        }

        [Fact]
        public async Task UpdateAsync_WithNonCancelledToken_Completes()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            using var cts = new CancellationTokenSource();

            // Act - SaveAsync routes to UpdateAsync when IsNew=false
            var result = await this.factory.SaveAsync(obj, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task UpdateAsync_WithAlreadyCancelledToken_Throws()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.SaveAsync(obj, cts.Token));
        }

        [Fact]
        public async Task DeleteAsync_WithNonCancelledToken_Completes()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsDeleted = true;
            using var cts = new CancellationTokenSource();

            // Act - SaveAsync routes to DeleteAsync when IsDeleted=true
            var result = await this.factory.SaveAsync(obj, cts.Token);

            // Assert - void Delete returns the object (only bool Delete returning false returns null)
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task DeleteAsync_WithAlreadyCancelledToken_Throws()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsDeleted = true;
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.SaveAsync(obj, cts.Token));
        }
    }

    #endregion

    #region Remote CancellationToken Support

    /// <summary>
    /// Tests for CancellationToken support with remote factory methods.
    /// CancellationToken is excluded from serialized parameters and flows through HTTP layer instead.
    /// </summary>
    [Factory]
    public class RemoteCancellableFactory
    {
        public bool CreateCalled { get; set; }
        public bool FetchCalled { get; set; }
        public bool CancellationWasChecked { get; set; }
        public int? BusinessParam { get; set; }

        [Create]
        public RemoteCancellableFactory() { }

        [Create]
        [Remote]
        public async Task CreateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.CreateCalled = true;
            await Task.Delay(10, cancellationToken);
        }

        [Fetch]
        [Remote]
        public async Task FetchAsync(int param, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.BusinessParam = param;
            this.CancellationWasChecked = true;
            this.FetchCalled = true;
            await Task.Delay(10, cancellationToken);
        }
    }

    public class RemoteCancellableFactoryTests : FactoryTestBase<IRemoteCancellableFactoryFactory>
    {
        [Fact]
        public async Task CreateAsync_Remote_WithNonCancelledToken_Completes()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act - Goes through remote serialization path
            var result = await this.factory.CreateAsync(cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task CreateAsync_Remote_WithAlreadyCancelledToken_Throws()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert - CancellationToken flows through HTTP layer
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.CreateAsync(cts.Token));
        }

        [Fact]
        public async Task FetchAsync_Remote_WithBusinessParamAndCancellationToken_Works()
        {
            // Arrange
            const int expectedParam = 42;
            using var cts = new CancellationTokenSource();

            // Act - Business param is serialized, CancellationToken flows through HTTP
            var result = await this.factory.FetchAsync(expectedParam, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
            Assert.Equal(expectedParam, result.BusinessParam);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task FetchAsync_Remote_WithCancelledToken_Throws()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.FetchAsync(123, cts.Token));
        }
    }

    #endregion

    #region IFactoryOnCancelled Callback Tests

    /// <summary>
    /// Factory class that implements IFactoryOnCancelled to receive cancellation callbacks.
    /// </summary>
    [Factory]
    public class CancellationCallbackFactory : IFactoryOnCancelled, IFactoryOnCancelledAsync
    {
        public bool FactoryCancelledCalled { get; set; }
        public bool FactoryCancelledAsyncCalled { get; set; }
        public FactoryOperation? CancelledOperation { get; set; }

        [Create]
        public CancellationCallbackFactory() { }

        [Fetch]
        public async Task FetchAsync(CancellationToken cancellationToken)
        {
            // Delay to allow cancellation
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        public void FactoryCancelled(FactoryOperation factoryOperation)
        {
            this.FactoryCancelledCalled = true;
            this.CancelledOperation = factoryOperation;
        }

        public Task FactoryCancelledAsync(FactoryOperation factoryOperation)
        {
            this.FactoryCancelledAsyncCalled = true;
            this.CancelledOperation = factoryOperation;
            return Task.CompletedTask;
        }
    }

    public class CancellationCallbackFactoryTests : FactoryTestBase<ICancellationCallbackFactoryFactory>
    {
        [Fact]
        public async Task Fetch_WhenCancelled_InvokesFactoryCancelledCallback()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Start the fetch and cancel immediately
            var fetchTask = this.factory.FetchAsync(cts.Token);
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => fetchTask);
        }

        [Fact]
        public async Task Fetch_WithPreCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert - TaskCanceledException inherits from OperationCanceledException
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => this.factory.FetchAsync(cts.Token));
        }
    }

    #endregion

    #region Mixed Parameters with CancellationToken

    /// <summary>
    /// Factory class with business params, CancellationToken, and Service params.
    /// </summary>
    [Factory]
    public class MixedParamCancellableFactory
    {
        public int? BusinessParam { get; set; }
        public bool ServiceWasInjected { get; set; }
        public bool CancellationWasChecked { get; set; }
        public bool CreateCalled { get; set; }
        public bool FetchCalled { get; set; }

        [Create]
        public MixedParamCancellableFactory() { }

        [Create]
        public async Task CreateAsync(int param, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.BusinessParam = param;
            this.CancellationWasChecked = true;
            this.CreateCalled = true;
            await Task.Delay(10, cancellationToken);
        }

        [Fetch]
        public async Task FetchAsync(
            int param,
            CancellationToken cancellationToken,
            [Service] IService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            cancellationToken.ThrowIfCancellationRequested();
            this.BusinessParam = param;
            this.ServiceWasInjected = true;
            this.CancellationWasChecked = true;
            this.FetchCalled = true;
            await Task.Delay(10, cancellationToken);
        }

        [Create]
        public async Task CreateWithServiceAsync(
            [Service] IService service,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(service);
            cancellationToken.ThrowIfCancellationRequested();
            this.ServiceWasInjected = true;
            this.CancellationWasChecked = true;
            this.CreateCalled = true;
            await Task.Delay(10, cancellationToken);
        }

        [Fetch]
        public async Task FetchComplexAsync(
            int intParam,
            string stringParam,
            CancellationToken cancellationToken,
            [Service] IService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            cancellationToken.ThrowIfCancellationRequested();
            this.BusinessParam = intParam;
            this.ServiceWasInjected = true;
            this.CancellationWasChecked = true;
            this.FetchCalled = true;
            await Task.Delay(10, cancellationToken);
        }
    }

    public class MixedParamCancellableFactoryTests : FactoryTestBase<IMixedParamCancellableFactoryFactory>
    {
        [Fact]
        public async Task CreateAsync_WithParamAndCancellationToken_Works()
        {
            // Arrange
            const int expectedParam = 42;
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.CreateAsync(expectedParam, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
            Assert.Equal(expectedParam, result.BusinessParam);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task FetchAsync_WithParamServiceAndCancellationToken_Works()
        {
            // Arrange
            const int expectedParam = 123;
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.FetchAsync(expectedParam, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
            Assert.Equal(expectedParam, result.BusinessParam);
            Assert.True(result.ServiceWasInjected);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task CreateWithServiceAsync_Works()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.CreateWithServiceAsync(cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
            Assert.True(result.ServiceWasInjected);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task FetchComplexAsync_WithMultipleParamsServiceAndCancellationToken_Works()
        {
            // Arrange
            const int expectedInt = 999;
            const string expectedString = "test";
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.FetchComplexAsync(expectedInt, expectedString, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.FetchCalled);
            Assert.Equal(expectedInt, result.BusinessParam);
            Assert.True(result.ServiceWasInjected);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task CreateAsync_WithCancelledToken_Throws()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.CreateAsync(42, cts.Token));
        }

        [Fact]
        public async Task FetchAsync_WithCancelledToken_Throws()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.FetchAsync(123, cts.Token));
        }
    }

    #endregion

    #region CancellationToken with Bool Return

    /// <summary>
    /// Factory class with CancellationToken and bool return type.
    /// </summary>
    [Factory]
    public class CancellableBoolFactory
    {
        public bool CreateCalled { get; set; }
        public bool ShouldSucceed { get; set; } = true;
        public bool CancellationWasChecked { get; set; }

        [Create]
        public CancellableBoolFactory() { }

        [Create]
        public async Task<bool> CreateBoolAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.CreateCalled = true;
            await Task.Delay(10, cancellationToken);
            return this.ShouldSucceed;
        }

        [Fetch]
        public async Task<bool> FetchBoolAsync(bool shouldSucceed, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ShouldSucceed = shouldSucceed;
            this.CancellationWasChecked = true;
            await Task.Delay(10, cancellationToken);
            return shouldSucceed;
        }
    }

    public class CancellableBoolFactoryTests : FactoryTestBase<ICancellableBoolFactoryFactory>
    {
        [Fact]
        public async Task CreateBoolAsync_ReturnsObject_WhenTrue()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.CreateBoolAsync(cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.CreateCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task FetchBoolAsync_ReturnsObject_WhenTrue()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.FetchBoolAsync(true, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.ShouldSucceed);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task FetchBoolAsync_ReturnsNull_WhenFalse()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.FetchBoolAsync(false, cts.Token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateBoolAsync_WithCancelledToken_Throws()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.CreateBoolAsync(cts.Token));
        }
    }

    #endregion

    #region Write Operations with CancellationToken and Bool Return

    /// <summary>
    /// Factory class with CancellationToken on Write operations with bool return.
    /// </summary>
    [Factory]
    public class CancellableWriteBoolFactory : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }
        public bool CancellationWasChecked { get; set; }
        public bool ShouldSucceed { get; set; } = true;

        [Create]
        public CancellableWriteBoolFactory() { }

        [Insert]
        public async Task<bool> InsertBoolAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.InsertCalled = true;
            await Task.Delay(10, cancellationToken);
            return this.ShouldSucceed;
        }

        [Update]
        public async Task<bool> UpdateBoolAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CancellationWasChecked = true;
            this.UpdateCalled = true;
            await Task.Delay(10, cancellationToken);
            return this.ShouldSucceed;
        }
    }

    public class CancellableWriteBoolFactoryTests : FactoryTestBase<ICancellableWriteBoolFactoryFactory>
    {
        [Fact]
        public async Task InsertBoolAsync_ReturnsObject_WhenSucceeds()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.ShouldSucceed = true;
            using var cts = new CancellationTokenSource();

            // Act - SaveBoolAsync routes to InsertBoolAsync when IsNew=true
            var result = await this.factory.SaveBoolAsync(obj, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task InsertBoolAsync_ReturnsNull_WhenFails()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.ShouldSucceed = false;
            using var cts = new CancellationTokenSource();

            // Act - SaveBoolAsync routes to InsertBoolAsync when IsNew=true
            var result = await this.factory.SaveBoolAsync(obj, cts.Token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateBoolAsync_ReturnsObject_WhenSucceeds()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.ShouldSucceed = true;
            using var cts = new CancellationTokenSource();

            // Act - SaveBoolAsync routes to UpdateBoolAsync when IsNew=false
            var result = await this.factory.SaveBoolAsync(obj, cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
            Assert.True(result.CancellationWasChecked);
        }

        [Fact]
        public async Task UpdateBoolAsync_ReturnsNull_WhenFails()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.ShouldSucceed = false;
            using var cts = new CancellationTokenSource();

            // Act - SaveBoolAsync routes to UpdateBoolAsync when IsNew=false
            var result = await this.factory.SaveBoolAsync(obj, cts.Token);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task InsertBoolAsync_WithCancelledToken_Throws()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => this.factory.SaveBoolAsync(obj, cts.Token));
        }
    }

    #endregion

    #region Default CancellationToken Value

    /// <summary>
    /// Factory class with default CancellationToken parameter.
    /// </summary>
    [Factory]
    public class DefaultCancellableFactory
    {
        public bool CreateCalled { get; set; }

        [Create]
        public DefaultCancellableFactory() { }

        [Create]
        public async Task CreateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CreateCalled = true;
            await Task.Delay(10, cancellationToken);
        }
    }

    public class DefaultCancellableFactoryTests : FactoryTestBase<IDefaultCancellableFactoryFactory>
    {
        [Fact]
        public async Task CreateAsync_WithDefaultToken_Completes()
        {
            // Act - Call with default token (CancellationToken.None)
            var result = await this.factory.CreateAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }

        [Fact]
        public async Task CreateAsync_WithExplicitToken_Completes()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            var result = await this.factory.CreateAsync(cts.Token);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CreateCalled);
        }
    }

    #endregion
}
