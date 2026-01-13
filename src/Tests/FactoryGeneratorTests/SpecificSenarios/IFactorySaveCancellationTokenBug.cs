using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.SpecificSenarios;

/// <summary>
/// Regression test for IFactorySave not being generated when CancellationToken is used with save operations.
///
/// Bug: When Insert/Update/Delete methods have CancellationToken parameters, the generator:
/// - Extends FactoryBase&lt;T&gt; instead of FactorySaveBase&lt;T&gt;
/// - Does NOT implement IFactorySave&lt;T&gt;
/// - Does NOT register IFactorySave&lt;T&gt; in DI
///
/// This causes EntityBase.Save() to fail because it relies on IFactorySave&lt;T&gt; being registered.
/// </summary>
public class IFactorySaveCancellationTokenBugTests
{
    #region Test Entities

    /// <summary>
    /// LOCAL: Entity with CancellationToken on all save operations.
    /// The generated factory SHOULD implement IFactorySave&lt;T&gt;.
    /// </summary>
    [Factory]
    public class LocalSaveWithCancellation : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }
        public bool DeleteCalled { get; set; }

        [Create]
        public LocalSaveWithCancellation() { }

        [Insert]
        public async Task InsertAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.InsertCalled = true;
            await Task.CompletedTask;
        }

        [Update]
        public async Task UpdateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.UpdateCalled = true;
            await Task.CompletedTask;
        }

        [Delete]
        public async Task DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.DeleteCalled = true;
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// REMOTE: Entity with CancellationToken on Remote save operations.
    /// The generated factory SHOULD implement IFactorySave&lt;T&gt;.
    /// </summary>
    [Factory]
    public class RemoteSaveWithCancellation : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }
        public bool DeleteCalled { get; set; }

        [Create]
        public RemoteSaveWithCancellation() { }

        [Remote]
        [Insert]
        public async Task InsertAsync(CancellationToken cancellationToken, [Service] IServerOnlyService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            cancellationToken.ThrowIfCancellationRequested();
            this.InsertCalled = true;
            await Task.CompletedTask;
        }

        [Remote]
        [Update]
        public async Task UpdateAsync(CancellationToken cancellationToken, [Service] IServerOnlyService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            cancellationToken.ThrowIfCancellationRequested();
            this.UpdateCalled = true;
            await Task.CompletedTask;
        }

        [Remote]
        [Delete]
        public async Task DeleteAsync(CancellationToken cancellationToken, [Service] IServerOnlyService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            cancellationToken.ThrowIfCancellationRequested();
            this.DeleteCalled = true;
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// PARTIAL: Entity with CancellationToken on Insert/Update only (no Delete).
    /// The generated factory SHOULD implement IFactorySave&lt;T&gt;.
    /// </summary>
    [Factory]
    public class PartialSaveWithCancellation : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }

        [Create]
        public PartialSaveWithCancellation() { }

        [Insert]
        public async Task InsertAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.InsertCalled = true;
            await Task.CompletedTask;
        }

        [Update]
        public async Task UpdateAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.UpdateCalled = true;
            await Task.CompletedTask;
        }
    }

    #endregion

    #region Local Tests

    public class LocalSaveWithCancellationTests : FactoryTestBase<ILocalSaveWithCancellationFactory>
    {
        /// <summary>
        /// REGRESSION TEST: IFactorySave&lt;T&gt; should be resolvable from DI when
        /// LOCAL Insert/Update/Delete have CancellationToken parameters.
        /// </summary>
        [Fact]
        public void IFactorySave_ShouldBeRegistered_ForLocalWithCancellation()
        {
            // Act - Try to resolve IFactorySave<T> from DI
            var factorySave = this.clientScope.ServiceProvider.GetService<IFactorySave<LocalSaveWithCancellation>>();

            // Assert - IFactorySave<T> should be registered
            Assert.NotNull(factorySave);
        }

        /// <summary>
        /// REGRESSION TEST: Saving via IFactorySave&lt;T&gt; should work correctly.
        /// </summary>
        [Fact]
        public async Task IFactorySave_Save_ShouldWork_ForLocalWithCancellation()
        {
            // Arrange
            var factorySave = this.clientScope.ServiceProvider.GetRequiredService<IFactorySave<LocalSaveWithCancellation>>();
            var entity = this.factory.Create();
            entity.IsNew = true;

            // Act
            var result = await factorySave.Save(entity);

            // Assert
            Assert.NotNull(result);
            var typedResult = Assert.IsType<LocalSaveWithCancellation>(result);
            Assert.True(typedResult.InsertCalled);
        }

        /// <summary>
        /// Verify the factory's Save method still works.
        /// </summary>
        [Fact]
        public async Task Factory_SaveAsync_ShouldWork()
        {
            // Arrange
            var entity = this.factory.Create();
            entity.IsNew = true;

            // Act
            var result = await this.factory.SaveAsync(entity);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
        }
    }

    #endregion

    #region Remote Tests

    public class RemoteSaveWithCancellationTests : FactoryTestBase<IRemoteSaveWithCancellationFactory>
    {
        /// <summary>
        /// REGRESSION TEST: IFactorySave&lt;T&gt; should be resolvable from DI when
        /// REMOTE Insert/Update/Delete have CancellationToken parameters.
        /// </summary>
        [Fact]
        public void IFactorySave_ShouldBeRegistered_ForRemoteWithCancellation()
        {
            // Act - Try to resolve IFactorySave<T> from DI
            var factorySave = this.clientScope.ServiceProvider.GetService<IFactorySave<RemoteSaveWithCancellation>>();

            // Assert - IFactorySave<T> should be registered
            Assert.NotNull(factorySave);
        }

        /// <summary>
        /// REGRESSION TEST: Saving via IFactorySave&lt;T&gt; should work correctly with remote.
        /// </summary>
        [Fact]
        public async Task IFactorySave_Save_ShouldWork_ForRemoteWithCancellation()
        {
            // Arrange
            var factorySave = this.clientScope.ServiceProvider.GetRequiredService<IFactorySave<RemoteSaveWithCancellation>>();
            var entity = this.factory.Create();
            entity.IsNew = true;

            // Act
            var result = await factorySave.Save(entity);

            // Assert
            Assert.NotNull(result);
            var typedResult = Assert.IsType<RemoteSaveWithCancellation>(result);
            Assert.True(typedResult.InsertCalled);
        }

        /// <summary>
        /// Verify the factory's Save method still works with remote.
        /// </summary>
        [Fact]
        public async Task Factory_SaveAsync_ShouldWork()
        {
            // Arrange
            var entity = this.factory.Create();
            entity.IsNew = true;

            // Act
            var result = await this.factory.SaveAsync(entity);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
        }
    }

    #endregion

    #region Partial Save Tests

    public class PartialSaveWithCancellationTests : FactoryTestBase<IPartialSaveWithCancellationFactory>
    {
        /// <summary>
        /// REGRESSION TEST: IFactorySave&lt;T&gt; should be resolvable from DI when
        /// Insert/Update (without Delete) have CancellationToken parameters.
        /// </summary>
        [Fact]
        public void IFactorySave_ShouldBeRegistered_ForPartialWithCancellation()
        {
            // Act - Try to resolve IFactorySave<T> from DI
            var factorySave = this.clientScope.ServiceProvider.GetService<IFactorySave<PartialSaveWithCancellation>>();

            // Assert - IFactorySave<T> should be registered
            Assert.NotNull(factorySave);
        }

        /// <summary>
        /// Verify the factory's Save method works for Insert.
        /// </summary>
        [Fact]
        public async Task Factory_SaveAsync_Insert_ShouldWork()
        {
            // Arrange
            var entity = this.factory.Create();
            entity.IsNew = true;

            // Act
            var result = await this.factory.SaveAsync(entity);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.InsertCalled);
        }

        /// <summary>
        /// Verify the factory's Save method works for Update.
        /// </summary>
        [Fact]
        public async Task Factory_SaveAsync_Update_ShouldWork()
        {
            // Arrange
            var entity = this.factory.Create();
            entity.IsNew = false;

            // Act
            var result = await this.factory.SaveAsync(entity);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.UpdateCalled);
        }
    }

    #endregion
}
