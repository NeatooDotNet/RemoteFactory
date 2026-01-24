using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.Save;

/// <summary>
/// Code Path 17-22 tests: Save method generation patterns.
/// Tests cover: Update branch, Delete branch, IFactorySave explicit interface,
/// Can public method, Can remote method, and Can local method.
/// </summary>
public class SaveCodePathTests
{
    #region Code Path 17: Save Update Branch

    /// <summary>
    /// Code Path 17: Tests the Save update branch (when target.IsNew=false, target.IsDeleted=false).
    /// Location: RenderSaveLocalMethod, lines 631-641.
    /// Tests: Update branch logic: if (method.UpdateMethod != null) { var updateCall = BuildSaveBranchCall(...); }
    /// </summary>
    [Factory]
    public class SaveUpdateTarget : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }
        public bool UpdateCalled { get; set; }
        public string UpdateValue { get; set; } = string.Empty;

        [Create]
        public SaveUpdateTarget() { }

        [Update]
        public void Update()
        {
            UpdateCalled = true;
            UpdateValue = "UpdateExecuted";
        }
    }

    public class SaveUpdateBranchTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ISaveUpdateTargetFactory _factory;

        public SaveUpdateBranchTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<ISaveUpdateTargetFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void Save_RoutesToUpdate_WhenNotNewNotDeleted()
        {
            // Arrange - object exists and is not deleted
            var obj = _factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act - Should call LocalUpdate via BuildSaveBranchCall
            var result = _factory.Save(obj);

            // Assert - Update branch executed correctly
            Assert.NotNull(result);
            Assert.True(result.UpdateCalled);
            Assert.Equal("UpdateExecuted", result.UpdateValue);
        }

        [Fact]
        public void Save_CorrectMethodCallSyntax_UpdateBranch()
        {
            // This test verifies the return statement is correctly generated
            // If syntax bug (missing semicolon) occurred, compile would fail
            var obj = _factory.Create();
            obj.IsNew = false;

            // Multiple saves should work correctly (verifies no syntax issues)
            var result1 = _factory.Save(obj);
            var result2 = _factory.Save(result1);

            Assert.True(result1.UpdateCalled);
            Assert.True(result2.UpdateCalled);
        }
    }

    #endregion

    #region Code Path 18: Save Delete Branch

    /// <summary>
    /// Code Path 18: Tests the Save delete branch.
    /// Location: RenderSaveLocalMethod, lines 602-614.
    /// Tests: Delete branch has: if (target.IsNew) {{ return {defaultReturn}; }}
    /// </summary>
    [Factory]
    public class SaveDeleteTarget : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }
        public bool DeleteCalled { get; set; }
        public string DeleteValue { get; set; } = string.Empty;

        [Create]
        public SaveDeleteTarget() { }

        [Insert]
        public void Insert() { }

        [Update]
        public void Update() { }

        [Delete]
        public void Delete()
        {
            DeleteCalled = true;
            DeleteValue = "DeleteExecuted";
        }
    }

    public class SaveDeleteBranchTests : IDisposable
    {
        private readonly IServiceProvider _provider;
        private readonly ISaveDeleteTargetFactory _factory;

        public SaveDeleteBranchTests()
        {
            _provider = new ServerContainerBuilder().Build();
            _factory = _provider.GetRequiredService<ISaveDeleteTargetFactory>();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public void Save_RoutesToDelete_WhenIsDeletedIsTrue()
        {
            // Arrange - object is marked for deletion
            var obj = _factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = true;

            // Act - Should call LocalDelete
            var result = _factory.Save(obj);

            // Assert - Delete branch executed correctly
            Assert.NotNull(result);
            Assert.True(result!.DeleteCalled);
            Assert.Equal("DeleteExecuted", result.DeleteValue);
        }

        [Fact]
        public void Save_ReturnsNull_WhenIsDeletedAndIsNew()
        {
            // Arrange - new object marked for deletion (created then deleted before save)
            var obj = _factory.Create();
            obj.IsNew = true;
            obj.IsDeleted = true;

            // Act - Should return default (null) for new+deleted
            var result = _factory.Save(obj);

            // Assert - Return null for new deleted object (braces generate correctly)
            Assert.Null(result);
        }

        [Fact]
        public void Save_ChecksIsDeleted_NotInverted()
        {
            // Arrange - IsDeleted=false should NOT go to delete branch
            var obj = _factory.Create();
            obj.IsNew = false;
            obj.IsDeleted = false;

            // Act - Should call Update, not Delete
            var result = _factory.Save(obj);

            // Assert - Condition is correct (not inverted)
            Assert.NotNull(result);
            Assert.False(result!.DeleteCalled);
        }
    }

    #endregion

    #region Code Path 19: IFactorySave Explicit Interface

    /// <summary>
    /// Code Path 19: Tests the IFactorySave explicit interface method.
    /// Location: RenderSaveExplicitInterfaceMethod, lines 708-729.
    /// Key line 715: async Task&lt;IFactorySaveMeta?&gt; IFactorySave&lt;{model.ImplementationTypeName}&gt;.Save(...)
    /// </summary>
    [Factory]
    public class IFactorySaveTarget : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }
        public bool InsertCalled { get; set; }

        [Create]
        public IFactorySaveTarget() { }

        [Insert]
        public void Insert()
        {
            InsertCalled = true;
        }

        [Update]
        public void Update() { }
    }

    public class IFactorySaveExplicitTests : IDisposable
    {
        private readonly IServiceProvider _provider;

        public IFactorySaveExplicitTests()
        {
            _provider = new ServerContainerBuilder().Build();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public async Task IFactorySave_ExplicitInterface_IsAccessible()
        {
            // Arrange
            var factory = _provider.GetRequiredService<IIFactorySaveTargetFactory>();
            var target = factory.Create();
            target.IsNew = true;

            // Get the IFactorySave<T> explicit interface implementation
            var factorySave = _provider.GetRequiredService<IFactorySave<IFactorySaveTarget>>();

            // Act - Call through the explicit interface (async keyword must work correctly)
            var result = await factorySave.Save(target, CancellationToken.None);

            // Assert - Interface name is correct (IFactorySave, not IFactorySaveX)
            Assert.NotNull(result);
            Assert.IsType<IFactorySaveTarget>(result);
            Assert.True(((IFactorySaveTarget)result).InsertCalled);
        }

        [Fact]
        public async Task IFactorySave_ReturnType_IsIFactorySaveMeta()
        {
            // Arrange
            var factorySave = _provider.GetRequiredService<IFactorySave<IFactorySaveTarget>>();
            var factory = _provider.GetRequiredService<IIFactorySaveTargetFactory>();
            var target = factory.Create();
            target.IsNew = true;

            // Act
            IFactorySaveMeta? result = await factorySave.Save(target, CancellationToken.None);

            // Assert - Return type is correct interface
            Assert.NotNull(result);
        }
    }

    #endregion

    #region Code Path 19: IFactorySave with Async Operations

    /// <summary>
    /// Additional test for IFactorySave with async insert to verify async keyword works.
    /// </summary>
    [Factory]
    public class IFactorySaveAsyncTarget : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }
        public bool InsertCalled { get; set; }

        [Create]
        public IFactorySaveAsyncTarget() { }

        [Insert]
        public async Task InsertAsync()
        {
            await Task.Delay(1);
            InsertCalled = true;
        }

        [Update]
        public async Task UpdateAsync()
        {
            await Task.Delay(1);
        }
    }

    public class IFactorySaveAsyncTests : IDisposable
    {
        private readonly IServiceProvider _provider;

        public IFactorySaveAsyncTests()
        {
            _provider = new ServerContainerBuilder().Build();
        }

        public void Dispose() => (_provider as IDisposable)?.Dispose();

        [Fact]
        public async Task IFactorySave_WithAsyncOperations_WorksCorrectly()
        {
            // Arrange
            var factorySave = _provider.GetRequiredService<IFactorySave<IFactorySaveAsyncTarget>>();
            var factory = _provider.GetRequiredService<IIFactorySaveAsyncTargetFactory>();
            var target = factory.Create();
            target.IsNew = true;

            // Act - async operations through IFactorySave interface
            var result = await factorySave.Save(target, CancellationToken.None);

            // Assert - async keyword was correct (not asyncXXX)
            Assert.NotNull(result);
            Assert.True(((IFactorySaveAsyncTarget)result).InsertCalled);
        }
    }

    #endregion
}
