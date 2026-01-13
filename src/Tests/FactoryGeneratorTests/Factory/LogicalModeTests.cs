using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for NeatooFactory.Logical mode with [Remote] save methods.
/// These tests validate that Logical mode works correctly when entities
/// have [Remote] attributes on their [Insert], [Update], and [Delete] methods.
///
/// Reproduces the bug reported in docs/todos/neatoofactory-logical-mode-regression.md
/// </summary>
public class LogicalModeTests
{
	/// <summary>
	/// Entity with [Remote] on all save methods (Insert, Update, Delete).
	/// This matches the pattern used in Neatoo's Person entity.
	/// </summary>
	[Factory]
	public class LogicalModeEntity : IFactorySaveMeta
	{
		public bool IsDeleted { get; set; }
		public bool IsNew { get; set; } = true;

		public bool InsertCalled { get; set; }
		public bool UpdateCalled { get; set; }
		public bool DeleteCalled { get; set; }

		[Insert]
		[Remote]
		public Task Insert()
		{
			this.InsertCalled = true;
			return Task.CompletedTask;
		}

		[Update]
		[Remote]
		public Task Update()
		{
			this.UpdateCalled = true;
			return Task.CompletedTask;
		}

		[Delete]
		[Remote]
		public Task Delete()
		{
			this.DeleteCalled = true;
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// Entity with [Remote] save methods that have [Service] parameters.
	/// This is a common pattern where save methods depend on injected services.
	/// </summary>
	[Factory]
	public class LogicalModeEntityWithService : IFactorySaveMeta
	{
		public bool IsDeleted { get; set; }
		public bool IsNew { get; set; } = true;

		public bool InsertCalled { get; set; }
		public bool UpdateCalled { get; set; }
		public bool DeleteCalled { get; set; }

		[Insert]
		[Remote]
		public Task Insert([Service] IService service)
		{
			Assert.NotNull(service);
			this.InsertCalled = true;
			return Task.CompletedTask;
		}

		[Update]
		[Remote]
		public Task Update([Service] IService service)
		{
			Assert.NotNull(service);
			this.UpdateCalled = true;
			return Task.CompletedTask;
		}

		[Delete]
		[Remote]
		public Task Delete([Service] IService service)
		{
			Assert.NotNull(service);
			this.DeleteCalled = true;
			return Task.CompletedTask;
		}
	}

	#region IFactorySave<T> Resolution Tests

	/// <summary>
	/// Validates that IFactorySave&lt;T&gt; is registered and resolvable in Logical mode.
	/// Bug Error 1: "No factory save method is configured" suggests IFactorySave is not registered.
	/// </summary>
	[Fact]
	public void LogicalMode_IFactorySave_CanBeResolved()
	{
		var scopes = ClientServerContainers.Scopes();

		// Should be able to resolve IFactorySave<T> in Logical mode
		var factorySave = scopes.local.ServiceProvider.GetService<IFactorySave<LogicalModeEntity>>();

		Assert.NotNull(factorySave);
	}

	/// <summary>
	/// Validates that IFactorySave&lt;T&gt; is registered for entities with [Service] parameters.
	/// </summary>
	[Fact]
	public void LogicalMode_IFactorySave_WithService_CanBeResolved()
	{
		var scopes = ClientServerContainers.Scopes();

		var factorySave = scopes.local.ServiceProvider.GetService<IFactorySave<LogicalModeEntityWithService>>();

		Assert.NotNull(factorySave);
	}

	#endregion

	#region IFactorySave<T>.Save() Tests

	/// <summary>
	/// Tests IFactorySave&lt;T&gt;.Save() for Insert operation in Logical mode.
	/// </summary>
	[Fact]
	public async Task LogicalMode_IFactorySave_Save_Insert()
	{
		var scopes = ClientServerContainers.Scopes();
		var factorySave = scopes.local.ServiceProvider.GetRequiredService<IFactorySave<LogicalModeEntity>>();

		var entity = new LogicalModeEntity { IsNew = true };
		var result = await factorySave.Save(entity);

		Assert.NotNull(result);
		// Note: In Logical mode, serialization creates a new object, so we check the result
		var typedResult = result as LogicalModeEntity;
		Assert.NotNull(typedResult);
		Assert.True(typedResult.InsertCalled);
	}

	/// <summary>
	/// Tests IFactorySave&lt;T&gt;.Save() for Update operation in Logical mode.
	/// </summary>
	[Fact]
	public async Task LogicalMode_IFactorySave_Save_Update()
	{
		var scopes = ClientServerContainers.Scopes();
		var factorySave = scopes.local.ServiceProvider.GetRequiredService<IFactorySave<LogicalModeEntity>>();

		var entity = new LogicalModeEntity { IsNew = false, IsDeleted = false };
		var result = await factorySave.Save(entity);

		Assert.NotNull(result);
		var typedResult = result as LogicalModeEntity;
		Assert.NotNull(typedResult);
		Assert.True(typedResult.UpdateCalled);
	}

	/// <summary>
	/// Tests IFactorySave&lt;T&gt;.Save() for Delete operation in Logical mode.
	/// </summary>
	[Fact]
	public async Task LogicalMode_IFactorySave_Save_Delete()
	{
		var scopes = ClientServerContainers.Scopes();
		var factorySave = scopes.local.ServiceProvider.GetRequiredService<IFactorySave<LogicalModeEntity>>();

		var entity = new LogicalModeEntity { IsNew = false, IsDeleted = true };
		var result = await factorySave.Save(entity);

		Assert.NotNull(result);
		var typedResult = result as LogicalModeEntity;
		Assert.NotNull(typedResult);
		Assert.True(typedResult.DeleteCalled);
	}

	/// <summary>
	/// Tests IFactorySave&lt;T&gt;.Save() with [Service] parameters in Logical mode.
	/// </summary>
	[Fact]
	public async Task LogicalMode_IFactorySave_Save_WithService_Insert()
	{
		var scopes = ClientServerContainers.Scopes();
		var factorySave = scopes.local.ServiceProvider.GetRequiredService<IFactorySave<LogicalModeEntityWithService>>();

		var entity = new LogicalModeEntityWithService { IsNew = true };
		var result = await factorySave.Save(entity);

		Assert.NotNull(result);
		var typedResult = result as LogicalModeEntityWithService;
		Assert.NotNull(typedResult);
		Assert.True(typedResult.InsertCalled);
	}

	#endregion

	#region Factory.Save() Tests

	/// <summary>
	/// Tests factory.Save() directly in Logical mode.
	/// Bug Error 2: "Parameter count mismatch" when using factory.Save().
	/// </summary>
	[Fact]
	public async Task LogicalMode_Factory_Save_Insert()
	{
		var scopes = ClientServerContainers.Scopes();
		var factory = scopes.local.ServiceProvider.GetRequiredService<ILogicalModeEntityFactory>();

		var entity = new LogicalModeEntity { IsNew = true };
		var result = await factory.Save(entity);

		Assert.NotNull(result);
		Assert.True(result.InsertCalled);
	}

	/// <summary>
	/// Tests factory.Save() for Update in Logical mode.
	/// </summary>
	[Fact]
	public async Task LogicalMode_Factory_Save_Update()
	{
		var scopes = ClientServerContainers.Scopes();
		var factory = scopes.local.ServiceProvider.GetRequiredService<ILogicalModeEntityFactory>();

		var entity = new LogicalModeEntity { IsNew = false, IsDeleted = false };
		var result = await factory.Save(entity);

		Assert.NotNull(result);
		Assert.True(result.UpdateCalled);
	}

	/// <summary>
	/// Tests factory.Save() for Delete in Logical mode.
	/// </summary>
	[Fact]
	public async Task LogicalMode_Factory_Save_Delete()
	{
		var scopes = ClientServerContainers.Scopes();
		var factory = scopes.local.ServiceProvider.GetRequiredService<ILogicalModeEntityFactory>();

		var entity = new LogicalModeEntity { IsNew = false, IsDeleted = true };
		var result = await factory.Save(entity);

		Assert.NotNull(result);
		Assert.True(result.DeleteCalled);
	}

	/// <summary>
	/// Tests factory.Save() with [Service] parameters in Logical mode.
	/// </summary>
	[Fact]
	public async Task LogicalMode_Factory_Save_WithService()
	{
		var scopes = ClientServerContainers.Scopes();
		var factory = scopes.local.ServiceProvider.GetRequiredService<ILogicalModeEntityWithServiceFactory>();

		var entity = new LogicalModeEntityWithService { IsNew = true };
		var result = await factory.Save(entity);

		Assert.NotNull(result);
		Assert.True(result.InsertCalled);
	}

	#endregion

	#region Comparison Tests (Logical vs Server mode)

	/// <summary>
	/// Compares IFactorySave behavior between Logical and Server modes.
	/// Both should work identically for save operations.
	/// </summary>
	[Theory]
	[InlineData(true, false, false, "Insert")]   // IsNew = true
	[InlineData(false, false, false, "Update")]  // IsNew = false, IsDeleted = false
	[InlineData(false, true, false, "Delete")]   // IsDeleted = true
	public async Task LogicalMode_MatchesServerMode_IFactorySave(bool isNew, bool isDeleted, bool _, string expectedOperation)
	{
		var scopes = ClientServerContainers.Scopes();

		// Logical mode (local container)
		var logicalFactorySave = scopes.local.ServiceProvider.GetRequiredService<IFactorySave<LogicalModeEntity>>();
		var logicalEntity = new LogicalModeEntity { IsNew = isNew, IsDeleted = isDeleted };
		var logicalResult = await logicalFactorySave.Save(logicalEntity);

		// Server mode
		var serverFactorySave = scopes.server.ServiceProvider.GetRequiredService<IFactorySave<LogicalModeEntity>>();
		var serverEntity = new LogicalModeEntity { IsNew = isNew, IsDeleted = isDeleted };
		var serverResult = await serverFactorySave.Save(serverEntity);

		// Both should succeed
		Assert.NotNull(logicalResult);
		Assert.NotNull(serverResult);

		// Verify correct operation was called
		var logicalTyped = (LogicalModeEntity)logicalResult;
		var serverTyped = (LogicalModeEntity)serverResult;

		switch (expectedOperation)
		{
			case "Insert":
				Assert.True(logicalTyped.InsertCalled);
				Assert.True(serverTyped.InsertCalled);
				break;
			case "Update":
				Assert.True(logicalTyped.UpdateCalled);
				Assert.True(serverTyped.UpdateCalled);
				break;
			case "Delete":
				Assert.True(logicalTyped.DeleteCalled);
				Assert.True(serverTyped.DeleteCalled);
				break;
		}
	}

	#endregion
}
