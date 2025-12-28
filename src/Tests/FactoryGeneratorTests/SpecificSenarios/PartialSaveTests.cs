using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.SpecificSenarios;

/// <summary>
/// Tests for partial write operation combinations (GAP-006).
/// Classes may not implement all three write operations (Insert, Update, Delete).
/// This affects the nullability of the Save return type and IFactorySaveMeta routing.
/// </summary>
public class PartialSaveTests
{
    #region InsertUpdateOnly - No Delete

    /// <summary>
    /// Class with Insert + Update but NO Delete.
    /// Save should NOT be nullable because Delete cannot return null.
    /// </summary>
    [Factory]
    public class InsertUpdateOnly : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }

        [Create]
        public InsertUpdateOnly() { }

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
    }

    public class InsertUpdateOnlyTests : FactoryTestBase<IInsertUpdateOnlyFactory>
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0007:Use implicit type", Justification = "Explicit type shows non-nullable return")]
        public void InsertUpdateOnly_Save_IsNotNullable()
        {
            // Arrange
            InsertUpdateOnly obj = this.factory.Create();
            obj.IsNew = true;

            // Act - Save should return non-nullable InsertUpdateOnly (not InsertUpdateOnly?)
            InsertUpdateOnly result = this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.InsertCalled);
        }

        [Fact]
        public void InsertUpdateOnly_Save_RoutesToInsert_WhenIsNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.IsDeleted = false;

            // Act
            var result = this.factory.Save(obj);

            // Assert
            Assert.True(result.InsertCalled);
            Assert.False(result.UpdateCalled);
        }

        [Fact]
        public void InsertUpdateOnly_Save_RoutesToUpdate_WhenNotNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act
            var result = this.factory.Save(obj);

            // Assert
            Assert.False(result.InsertCalled);
            Assert.True(result.UpdateCalled);
        }
    }

    #endregion

    #region InsertDeleteOnly - No Update

    /// <summary>
    /// Class with Insert + Delete but NO Update.
    /// An unusual but valid pattern where objects cannot be modified after creation.
    /// Save IS nullable because Delete can return null (deleted object).
    /// </summary>
    [Factory]
    public class InsertDeleteOnly : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool DeleteCalled { get; set; }

        [Create]
        public InsertDeleteOnly() { }

        [Insert]
        public void Insert()
        {
            this.InsertCalled = true;
        }

        [Delete]
        public void Delete()
        {
            this.DeleteCalled = true;
        }
    }

    public class InsertDeleteOnlyTests : FactoryTestBase<IInsertDeleteOnlyFactory>
    {
        [Fact]
        public void InsertDeleteOnly_Save_RoutesToInsert_WhenIsNew()
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
            Assert.False(result.DeleteCalled);
        }

        [Fact]
        public void InsertDeleteOnly_Save_RoutesToDelete_WhenIsDeleted()
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

        [Fact]
        public void InsertDeleteOnly_Save_ThrowsNotImplemented_WhenNotNewNotDeleted()
        {
            // Arrange - object exists but has no changes (no Update method)
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act & Assert - Without Update, Save throws NotImplementedException
            Assert.Throws<NotImplementedException>(() => this.factory.Save(obj));
        }
    }

    #endregion

    #region UpdateDeleteOnly - No Insert

    /// <summary>
    /// Class with Update + Delete but NO Insert.
    /// For scenarios where objects are created elsewhere (e.g., database-generated).
    /// Save IS nullable because Delete can return null.
    /// </summary>
    [Factory]
    public class UpdateDeleteOnly : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool UpdateCalled { get; set; }
        public bool DeleteCalled { get; set; }

        [Fetch]
        public UpdateDeleteOnly() { }

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

    public class UpdateDeleteOnlyTests : FactoryTestBase<IUpdateDeleteOnlyFactory>
    {
        [Fact]
        public void UpdateDeleteOnly_Save_RoutesToUpdate_WhenNotNewNotDeleted()
        {
            // Arrange
            var obj = this.factory.Fetch();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act
            var result = this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
            Assert.False(result.DeleteCalled);
        }

        [Fact]
        public void UpdateDeleteOnly_Save_RoutesToDelete_WhenIsDeleted()
        {
            // Arrange
            var obj = this.factory.Fetch();
            obj.IsNew = false;
            obj.IsDeleted = true;

            // Act
            var result = this.factory.Save(obj);

            // Assert - void Delete returns the object (only bool Delete returning false returns null)
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
        }

        [Fact]
        public void UpdateDeleteOnly_Save_ThrowsNotImplemented_WhenIsNew()
        {
            // Arrange - IsNew but no Insert method
            var obj = this.factory.Fetch();
            obj.IsNew = true;
            obj.IsDeleted = false;

            // Act & Assert - Without Insert, Save throws NotImplementedException
            Assert.Throws<NotImplementedException>(() => this.factory.Save(obj));
        }
    }

    #endregion

    #region InsertOnly - No Update or Delete

    /// <summary>
    /// Class with only Insert (no Update or Delete).
    /// For immutable objects that can only be created once.
    /// Save is NOT nullable because there is no Delete.
    /// </summary>
    [Factory]
    public class InsertOnly : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }

        [Create]
        public InsertOnly() { }

        [Insert]
        public void Insert()
        {
            this.InsertCalled = true;
        }
    }

    public class InsertOnlyTests : FactoryTestBase<IInsertOnlyFactory>
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0007:Use implicit type", Justification = "Explicit type shows non-nullable return")]
        public void InsertOnly_Save_IsNotNullable()
        {
            // Arrange
            InsertOnly obj = this.factory.Create();
            obj.IsNew = true;

            // Act - Save should return non-nullable InsertOnly
            InsertOnly result = this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.InsertCalled);
        }

        [Fact]
        public void InsertOnly_Save_ThrowsNotImplemented_WhenNotNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act & Assert - Without Update, Save throws NotImplementedException
            Assert.Throws<NotImplementedException>(() => this.factory.Save(obj));
        }
    }

    #endregion

    #region Remote Partial Operations

    /// <summary>
    /// Tests remote execution of partial operations.
    /// Insert + Update only with [Remote] attribute.
    /// </summary>
    [Factory]
    public class RemoteInsertUpdateOnly : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }

        [Create]
        public RemoteInsertUpdateOnly() { }

        [Insert]
        [Remote]
        public void Insert()
        {
            this.InsertCalled = true;
        }

        [Update]
        [Remote]
        public void Update()
        {
            this.UpdateCalled = true;
        }
    }

    public class RemotePartialSaveTests : FactoryTestBase<IRemoteInsertUpdateOnlyFactory>
    {
        [Fact]
        public async Task RemoteInsertUpdateOnly_Save_RoutesToInsert_WhenIsNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.IsDeleted = false;

            // Act - Remote save (method named "Save" returning Task<T>)
            var result = await this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.InsertCalled);
            Assert.False(result.UpdateCalled);
        }

        [Fact]
        public async Task RemoteInsertUpdateOnly_Save_RoutesToUpdate_WhenNotNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act - Remote save (method named "Save" returning Task<T>)
            var result = await this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.InsertCalled);
            Assert.True(result.UpdateCalled);
        }
    }

    #endregion

    #region Async Partial Operations

    /// <summary>
    /// Tests async partial operations with Task return types.
    /// </summary>
    [Factory]
    public class AsyncInsertUpdateOnly : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }

        [Create]
        public AsyncInsertUpdateOnly() { }

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
    }

    public class AsyncPartialSaveTests : FactoryTestBase<IAsyncInsertUpdateOnlyFactory>
    {
        [Fact]
        public async Task AsyncInsertUpdateOnly_Save_RoutesToInsertAsync_WhenIsNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.IsDeleted = false;

            // Act - SaveAsync routes to InsertAsync when IsNew=true
            var result = await this.factory.SaveAsync(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.InsertCalled);
            Assert.False(result.UpdateCalled);
        }

        [Fact]
        public async Task AsyncInsertUpdateOnly_Save_RoutesToUpdateAsync_WhenNotNew()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act - SaveAsync routes to UpdateAsync when IsNew=false
            var result = await this.factory.SaveAsync(obj);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.InsertCalled);
            Assert.True(result.UpdateCalled);
        }
    }

    #endregion

    #region Bool Returning Partial Operations

    /// <summary>
    /// Tests partial operations that return bool.
    /// When bool returns false, Save should return null.
    /// </summary>
    [Factory]
    public class BoolInsertUpdateOnly : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }
        public bool ShouldSucceed { get; set; } = true;

        [Create]
        public BoolInsertUpdateOnly() { }

        [Insert]
        public bool Insert()
        {
            this.InsertCalled = true;
            return this.ShouldSucceed;
        }

        [Update]
        public bool Update()
        {
            this.UpdateCalled = true;
            return this.ShouldSucceed;
        }
    }

    public class BoolPartialSaveTests : FactoryTestBase<IBoolInsertUpdateOnlyFactory>
    {
        [Fact]
        public void BoolInsertUpdateOnly_Save_ReturnsObject_WhenInsertSucceeds()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.ShouldSucceed = true;

            // Act - Save routes to Insert when IsNew=true
            var result = this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.InsertCalled);
        }

        [Fact]
        public void BoolInsertUpdateOnly_Save_ReturnsNull_WhenInsertFails()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;
            obj.ShouldSucceed = false;

            // Act - Save routes to Insert when IsNew=true
            var result = this.factory.Save(obj);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void BoolInsertUpdateOnly_Save_ReturnsObject_WhenUpdateSucceeds()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.ShouldSucceed = true;

            // Act - Save routes to Update when IsNew=false
            var result = this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.UpdateCalled);
        }

        [Fact]
        public void BoolInsertUpdateOnly_Save_ReturnsNull_WhenUpdateFails()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;
            obj.ShouldSucceed = false;

            // Act - Save routes to Update when IsNew=false
            var result = this.factory.Save(obj);

            // Assert
            Assert.Null(result);
        }
    }

    #endregion

    #region Partial Operations with Service Parameters

    /// <summary>
    /// Tests partial operations that use [Service] dependency injection.
    /// </summary>
    [Factory]
    public class ServiceInsertUpdateOnly : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        public bool InsertCalled { get; set; }
        public bool UpdateCalled { get; set; }
        public bool ServiceWasInjected { get; set; }

        [Create]
        public ServiceInsertUpdateOnly() { }

        [Insert]
        public void Insert([Service] IService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.InsertCalled = true;
            this.ServiceWasInjected = true;
        }

        [Update]
        public void Update([Service] IService service)
        {
            ArgumentNullException.ThrowIfNull(service);
            this.UpdateCalled = true;
            this.ServiceWasInjected = true;
        }
    }

    public class ServicePartialSaveTests : FactoryTestBase<IServiceInsertUpdateOnlyFactory>
    {
        [Fact]
        public void ServiceInsertUpdateOnly_Save_InjectsService_ForInsert()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = true;

            // Act
            var result = this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.InsertCalled);
            Assert.True(result.ServiceWasInjected);
        }

        [Fact]
        public void ServiceInsertUpdateOnly_Save_InjectsService_ForUpdate()
        {
            // Arrange
            var obj = this.factory.Create();
            obj.IsNew = false;

            // Act
            var result = this.factory.Save(obj);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.UpdateCalled);
            Assert.True(result.ServiceWasInjected);
        }
    }

    #endregion
}
