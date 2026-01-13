using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for remote write operations (Insert, Update, Delete) with [AuthorizeFactory] authorization.
/// This addresses GAP-002 from the test plan: Remote Write Operations with Authorization - completely untested.
///
/// Security-critical tests verifying:
/// - Authorization is checked on SERVER side for remote write operations
/// - Auth returning false prevents the operation even when called remotely
/// - Remote auth methods with [Remote] attribute execute on server
/// - CanInsert, CanUpdate, CanDelete work correctly across client/server boundary
/// - TrySave with remote authorization
/// </summary>
public class RemoteWriteAuthTests
{
	#region Authorization Classes

	/// <summary>
	/// Synchronous authorization class for remote write operations.
	/// All methods marked with [Remote] to ensure they execute on server.
	/// Parameterless methods are always called; parameterized methods only called
	/// when the operation has matching parameters.
	/// </summary>
	public class RemoteWriteAuth
	{
		// Tracking properties - on server side
		public int CanWriteCalled { get; set; }
		public int CanInsertCalled { get; set; }
		public int CanUpdateCalled { get; set; }
		public int CanDeleteCalled { get; set; }

		// Write authorization - parameterless (always called)
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public bool CanWriteBool()
		{
			this.CanWriteCalled++;
			return true;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public string? CanWriteString()
		{
			this.CanWriteCalled++;
			return string.Empty;
		}

		// Write authorization - parameterized (p=10 causes bool fail, p=20 causes string fail)
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public bool CanWriteBoolFail(int? p)
		{
			this.CanWriteCalled++;
			return p != 10;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public string? CanWriteStringFail(int? p)
		{
			this.CanWriteCalled++;
			return p == 20 ? "RemoteWriteDenied" : string.Empty;
		}

		// Insert-specific authorization
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public bool CanInsertBool()
		{
			this.CanInsertCalled++;
			return true;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public string? CanInsertString()
		{
			this.CanInsertCalled++;
			return string.Empty;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public bool CanInsertBoolFail(int? p)
		{
			this.CanInsertCalled++;
			return p != 10;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public string? CanInsertStringFail(int? p)
		{
			this.CanInsertCalled++;
			return p == 20 ? "RemoteInsertDenied" : string.Empty;
		}

		// Update-specific authorization
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public bool CanUpdateBool()
		{
			this.CanUpdateCalled++;
			return true;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public string? CanUpdateString()
		{
			this.CanUpdateCalled++;
			return string.Empty;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public bool CanUpdateBoolFail(int? p)
		{
			this.CanUpdateCalled++;
			return p != 10;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public string? CanUpdateStringFail(int? p)
		{
			this.CanUpdateCalled++;
			return p == 20 ? "RemoteUpdateDenied" : string.Empty;
		}

		// Delete-specific authorization
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public bool CanDeleteBool()
		{
			this.CanDeleteCalled++;
			return true;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public string? CanDeleteString()
		{
			this.CanDeleteCalled++;
			return string.Empty;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public bool CanDeleteBoolFail(int? p)
		{
			this.CanDeleteCalled++;
			return p != 10;
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public string? CanDeleteStringFail(int? p)
		{
			this.CanDeleteCalled++;
			return p == 20 ? "RemoteDeleteDenied" : string.Empty;
		}

		public void Reset()
		{
			this.CanWriteCalled = 0;
			this.CanInsertCalled = 0;
			this.CanUpdateCalled = 0;
			this.CanDeleteCalled = 0;
		}
	}

	/// <summary>
	/// Async authorization class for remote write operations.
	/// Tests Task<bool> and Task<string> return types with [Remote].
	/// </summary>
	public class RemoteWriteAuthAsync : RemoteWriteAuth
	{
		public int CanWriteAsyncCalled { get; set; }
		public int CanInsertAsyncCalled { get; set; }
		public int CanUpdateAsyncCalled { get; set; }
		public int CanDeleteAsyncCalled { get; set; }

		// Async Write authorization - parameterless
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public Task<bool> CanWriteBoolAsync()
		{
			this.CanWriteAsyncCalled++;
			return Task.FromResult(true);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public Task<string> CanWriteStringAsync()
		{
			this.CanWriteAsyncCalled++;
			return Task.FromResult(string.Empty);
		}

		// Async Write authorization - parameterized
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public Task<bool> CanWriteBoolFailAsync(int? p)
		{
			this.CanWriteAsyncCalled++;
			return Task.FromResult(p != 10);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public Task<string> CanWriteStringFailAsync(int? p)
		{
			this.CanWriteAsyncCalled++;
			return Task.FromResult(p == 20 ? "RemoteWriteAsyncDenied" : string.Empty);
		}

		// Async Insert authorization
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public Task<bool> CanInsertBoolAsync()
		{
			this.CanInsertAsyncCalled++;
			return Task.FromResult(true);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public Task<string> CanInsertStringAsync()
		{
			this.CanInsertAsyncCalled++;
			return Task.FromResult(string.Empty);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public Task<bool> CanInsertBoolFailAsync(int? p)
		{
			this.CanInsertAsyncCalled++;
			return Task.FromResult(p != 10);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public Task<string> CanInsertStringFailAsync(int? p)
		{
			this.CanInsertAsyncCalled++;
			return Task.FromResult(p == 20 ? "RemoteInsertAsyncDenied" : string.Empty);
		}

		// Async Update authorization
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public Task<bool> CanUpdateBoolAsync()
		{
			this.CanUpdateAsyncCalled++;
			return Task.FromResult(true);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public Task<string> CanUpdateStringAsync()
		{
			this.CanUpdateAsyncCalled++;
			return Task.FromResult(string.Empty);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public Task<bool> CanUpdateBoolFailAsync(int? p)
		{
			this.CanUpdateAsyncCalled++;
			return Task.FromResult(p != 10);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public Task<string> CanUpdateStringFailAsync(int? p)
		{
			this.CanUpdateAsyncCalled++;
			return Task.FromResult(p == 20 ? "RemoteUpdateAsyncDenied" : string.Empty);
		}

		// Async Delete authorization
		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public Task<bool> CanDeleteBoolAsync()
		{
			this.CanDeleteAsyncCalled++;
			return Task.FromResult(true);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public Task<string> CanDeleteStringAsync()
		{
			this.CanDeleteAsyncCalled++;
			return Task.FromResult(string.Empty);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public Task<bool> CanDeleteBoolFailAsync(int? p)
		{
			this.CanDeleteAsyncCalled++;
			return Task.FromResult(p != 10);
		}

		[Remote]
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public Task<string> CanDeleteStringFailAsync(int? p)
		{
			this.CanDeleteAsyncCalled++;
			return Task.FromResult(p == 20 ? "RemoteDeleteAsyncDenied" : string.Empty);
		}

		public new void Reset()
		{
			base.Reset();
			this.CanWriteAsyncCalled = 0;
			this.CanInsertAsyncCalled = 0;
			this.CanUpdateAsyncCalled = 0;
			this.CanDeleteAsyncCalled = 0;
		}
	}

	#endregion

	#region Domain Objects

	/// <summary>
	/// Domain object with remote write operations and synchronous remote authorization.
	/// All write methods have int? param to allow testing auth failure scenarios.
	/// </summary>
	[Factory]
	[AuthorizeFactory<RemoteWriteAuth>]
	public class RemoteWriteAuthObject : IFactorySaveMeta
	{
		public bool IsDeleted { get; set; }
		public bool IsNew { get; set; }

		// Self-tracking properties to verify method calls (will be set on server)
		public bool InsertCalled { get; set; }
		public bool UpdateCalled { get; set; }
		public bool DeleteCalled { get; set; }

		// Insert methods with [Remote] - execute on server
		[Insert]
		[Remote]
		public void InsertVoid(int? param)
		{
			this.InsertCalled = true;
		}

		[Insert]
		[Remote]
		public bool InsertBool(int? param)
		{
			this.InsertCalled = true;
			return true;
		}

		[Insert]
		[Remote]
		public Task InsertTask(int? param)
		{
			this.InsertCalled = true;
			return Task.CompletedTask;
		}

		[Insert]
		[Remote]
		public Task<bool> InsertTaskBool(int? param)
		{
			this.InsertCalled = true;
			return Task.FromResult(true);
		}

		// Update methods with [Remote]
		[Update]
		[Remote]
		public void UpdateVoid(int? param)
		{
			this.UpdateCalled = true;
		}

		[Update]
		[Remote]
		public bool UpdateBool(int? param)
		{
			this.UpdateCalled = true;
			return true;
		}

		[Update]
		[Remote]
		public Task UpdateTask(int? param)
		{
			this.UpdateCalled = true;
			return Task.CompletedTask;
		}

		[Update]
		[Remote]
		public Task<bool> UpdateTaskBool(int? param)
		{
			this.UpdateCalled = true;
			return Task.FromResult(true);
		}

		// Delete methods with [Remote]
		[Delete]
		[Remote]
		public void DeleteVoid(int? param)
		{
			this.DeleteCalled = true;
		}

		[Delete]
		[Remote]
		public bool DeleteBool(int? param)
		{
			this.DeleteCalled = true;
			return true;
		}

		[Delete]
		[Remote]
		public Task DeleteTask(int? param)
		{
			this.DeleteCalled = true;
			return Task.CompletedTask;
		}

		[Delete]
		[Remote]
		public Task<bool> DeleteTaskBool(int? param)
		{
			this.DeleteCalled = true;
			return Task.FromResult(true);
		}
	}

	/// <summary>
	/// Domain object with remote write operations and async remote authorization.
	/// All write methods have int? param.
	/// </summary>
	[Factory]
	[AuthorizeFactory<RemoteWriteAuthAsync>]
	public class RemoteWriteAuthAsyncObject : IFactorySaveMeta
	{
		public bool IsDeleted { get; set; }
		public bool IsNew { get; set; }

		public bool InsertCalled { get; set; }
		public bool UpdateCalled { get; set; }
		public bool DeleteCalled { get; set; }

		[Insert]
		[Remote]
		public void InsertVoid(int? param)
		{
			this.InsertCalled = true;
		}

		[Insert]
		[Remote]
		public Task InsertTask(int? param)
		{
			this.InsertCalled = true;
			return Task.CompletedTask;
		}

		[Update]
		[Remote]
		public void UpdateVoid(int? param)
		{
			this.UpdateCalled = true;
		}

		[Update]
		[Remote]
		public Task UpdateTask(int? param)
		{
			this.UpdateCalled = true;
			return Task.CompletedTask;
		}

		[Delete]
		[Remote]
		public void DeleteVoid(int? param)
		{
			this.DeleteCalled = true;
		}

		[Delete]
		[Remote]
		public Task DeleteTask(int? param)
		{
			this.DeleteCalled = true;
			return Task.CompletedTask;
		}
	}

	#endregion

	#region Test Setup

	private readonly IServiceScope clientScope;
	private readonly IServiceScope serverScope;

	public RemoteWriteAuthTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
		this.serverScope = scopes.server;
	}

	#endregion

	#region Remote Authorization Tests - Success Path

	/// <summary>
	/// Test that remote Save methods work correctly when authorization passes.
	/// Authorization should be checked on server side.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuth_SaveMethodsWork_WhenAuthorized()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthObjectFactory>();
		// Get auth from SERVER scope - remote auth runs on server
		var serverAuth = this.serverScope.ServiceProvider.GetRequiredService<RemoteWriteAuth>();

		// Reflection Approved
		var methods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Save") && !m.Name.Contains("Try") && !m.Name.Contains("Can"))
			.ToList();

		Assert.NotEmpty(methods);

		foreach (var method in methods)
		{
			serverAuth.Reset();
			var methodName = method.Name;

			// Test Insert (IsNew = true)
			var insertObj = new RemoteWriteAuthObject { IsNew = true };
			var insertResult = await InvokeSaveMethod(method, factory, insertObj);

			if (insertResult != null)
			{
				Assert.True(insertResult.InsertCalled, $"Insert should have been called for {methodName}");
			}

			// Verify auth was checked on SERVER
			Assert.True(serverAuth.CanWriteCalled > 0, $"Write auth should be checked on server for Insert in {methodName}");
			Assert.True(serverAuth.CanInsertCalled > 0, $"Insert auth should be checked on server for {methodName}");

			serverAuth.Reset();

			// Test Update (IsNew = false, IsDeleted = false)
			var updateObj = new RemoteWriteAuthObject { IsNew = false, IsDeleted = false };
			var updateResult = await InvokeSaveMethod(method, factory, updateObj);

			if (updateResult != null)
			{
				Assert.True(updateResult.UpdateCalled, $"Update should have been called for {methodName}");
			}

			Assert.True(serverAuth.CanWriteCalled > 0, $"Write auth should be checked on server for Update in {methodName}");
			Assert.True(serverAuth.CanUpdateCalled > 0, $"Update auth should be checked on server for {methodName}");

			serverAuth.Reset();

			// Test Delete (IsDeleted = true)
			var deleteObj = new RemoteWriteAuthObject { IsDeleted = true };
			var deleteResult = await InvokeSaveMethod(method, factory, deleteObj);

			if (deleteResult != null)
			{
				Assert.True(deleteResult.DeleteCalled, $"Delete should have been called for {methodName}");
			}

			Assert.True(serverAuth.CanWriteCalled > 0, $"Write auth should be checked on server for Delete in {methodName}");
			Assert.True(serverAuth.CanDeleteCalled > 0, $"Delete auth should be checked on server for {methodName}");
		}
	}

	#endregion

	#region Remote Authorization Tests - Failure Path

	/// <summary>
	/// Test that remote authorization failure (bool returning false) prevents the operation.
	/// The failure should occur on server side but be reported to client via NotAuthorizedException.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuth_SaveFails_WhenRemoteBoolAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthObjectFactory>();
		var serverAuth = this.serverScope.ServiceProvider.GetRequiredService<RemoteWriteAuth>();

		// Find methods with int? parameter (these check p=10 for failure)
		// Exclude CancellationToken from meaningful parameter count
		var methods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Save") &&
			            !m.Name.Contains("Try") &&
			            !m.Name.Contains("Can"))
			.Where(m =>
			{
				var meaningful = m.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();
				return meaningful.Length == 2 && meaningful[1].ParameterType == typeof(int?);
			})
			.ToList();

		Assert.NotEmpty(methods);

		foreach (var method in methods)
		{
			serverAuth.Reset();
			var methodName = method.Name;

			// Test Insert with auth failure (p=10) - auth checked on server, throws NotAuthorizedException
			var insertObj = new RemoteWriteAuthObject { IsNew = true };
			await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
			{
				await InvokeSaveMethodWithParam(method, factory, insertObj, 10);
			});

			// The original object should NOT have InsertCalled set because auth failed on server
		}
	}

	/// <summary>
	/// Test that remote authorization failure (string returning message) prevents the operation.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuth_SaveFails_WhenRemoteStringAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthObjectFactory>();
		var serverAuth = this.serverScope.ServiceProvider.GetRequiredService<RemoteWriteAuth>();

		// Find methods with int? parameter
		// Exclude CancellationToken from meaningful parameter count
		var methods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Save") &&
			            !m.Name.Contains("Try") &&
			            !m.Name.Contains("Can"))
			.Where(m =>
			{
				var meaningful = m.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();
				return meaningful.Length == 2 && meaningful[1].ParameterType == typeof(int?);
			})
			.ToList();

		Assert.NotEmpty(methods);

		foreach (var method in methods)
		{
			serverAuth.Reset();
			var methodName = method.Name;

			// Test Update with auth failure (p=20) - string auth failure, throws NotAuthorizedException with message
			var updateObj = new RemoteWriteAuthObject { IsNew = false, IsDeleted = false };
			var ex = await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
			{
				await InvokeSaveMethodWithParam(method, factory, updateObj, 20);
			});

			// Verify error message contains "Denied"
			Assert.Contains("Denied", ex.Message);
		}
	}

	#endregion

	#region Remote Can Methods Tests

	/// <summary>
	/// Test remote CanInsert, CanUpdate, CanDelete methods.
	/// These should check authorization on server side.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuth_CanMethods_CheckAuthorizationOnServer()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthObjectFactory>();
		var serverAuth = this.serverScope.ServiceProvider.GetRequiredService<RemoteWriteAuth>();

		// Find Can methods
		var canMethods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Can"))
			.ToList();

		Assert.NotEmpty(canMethods);

		foreach (var method in canMethods)
		{
			serverAuth.Reset();
			var methodName = method.Name;

			object? result;
			// Exclude CancellationToken from meaningful parameter count
			var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
			if (meaningfulParams.Any())
			{
				result = method.Invoke(factory, new object[] { 1, default(CancellationToken) }); // Pass valid param
			}
			else
			{
				result = method.Invoke(factory, new object[] { default(CancellationToken) });
			}

			if (result is Task<Authorized> authTask)
			{
				var authorized = await authTask;
				Assert.True(authorized.HasAccess, $"{methodName} should return HasAccess=true for valid params");
			}
			else if (result is Authorized authorized)
			{
				Assert.True(authorized.HasAccess, $"{methodName} should return HasAccess=true for valid params");
			}

			// Auth should have been checked on SERVER
			Assert.True(serverAuth.CanWriteCalled > 0, $"Write auth should be checked on server for {methodName}");
		}
	}

	/// <summary>
	/// Test remote Can methods with authorization failure.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuth_CanMethods_ReturnFalse_WhenRemoteAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthObjectFactory>();

		// Find Can methods with int? parameter
		// Exclude CancellationToken from meaningful parameter count
		var canMethods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Can"))
			.Where(m =>
			{
				var meaningful = m.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();
				return meaningful.Length == 1 && meaningful[0].ParameterType == typeof(int?);
			})
			.ToList();

		Assert.NotEmpty(canMethods);

		foreach (var method in canMethods)
		{
			var methodName = method.Name;

			// Test with p=10 to trigger bool failure on server
			var result = method.Invoke(factory, new object[] { 10, default(CancellationToken) });

			if (result is Task<Authorized> authTask)
			{
				var authorized = await authTask;
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=10");
			}
			else if (result is Authorized authorized)
			{
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=10");
			}
		}
	}

	#endregion

	#region Remote TrySave Tests

	/// <summary>
	/// Test remote TrySave methods with successful authorization.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuth_TrySaveMethods_ReturnAuthorizedResult()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthObjectFactory>();
		var serverAuth = this.serverScope.ServiceProvider.GetRequiredService<RemoteWriteAuth>();

		// Find TrySave methods
		var tryMethods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("TrySave"))
			.ToList();

		Assert.NotEmpty(tryMethods);

		foreach (var method in tryMethods)
		{
			serverAuth.Reset();
			var methodName = method.Name;

			// Test successful authorization
			var obj = new RemoteWriteAuthObject { IsNew = true };
			// Exclude CancellationToken from meaningful parameter count
			var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
			object?[] parameters = meaningfulParams.Count == 2
				? new object?[] { obj, 1, default(CancellationToken) }
				: new object?[] { obj, default(CancellationToken) };

			var result = method.Invoke(factory, parameters);

			if (result is Task<Authorized<RemoteWriteAuthObject>> authTask)
			{
				var authorized = await authTask;
				Assert.True(authorized.HasAccess, $"{methodName} should return HasAccess=true");
				Assert.NotNull(authorized.Result);
			}
			else if (result is Authorized<RemoteWriteAuthObject> authorized)
			{
				Assert.True(authorized.HasAccess, $"{methodName} should return HasAccess=true");
				Assert.NotNull(authorized.Result);
			}
		}
	}

	/// <summary>
	/// Test remote TrySave with authorization failure.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuth_TrySaveMethods_ReturnFailure_WhenRemoteAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthObjectFactory>();

		// Find TrySave methods with int? parameter
		// Exclude CancellationToken from meaningful parameter count
		var tryMethods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("TrySave"))
			.Where(m =>
			{
				var meaningful = m.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();
				return meaningful.Length == 2 && meaningful[1].ParameterType == typeof(int?);
			})
			.ToList();

		Assert.NotEmpty(tryMethods);

		foreach (var method in tryMethods)
		{
			var methodName = method.Name;

			// Test with p=10 to trigger bool failure on server
			var obj = new RemoteWriteAuthObject { IsNew = true };
			var result = method.Invoke(factory, new object?[] { obj, 10, default(CancellationToken) });

			if (result is Task<Authorized<RemoteWriteAuthObject>> authTask)
			{
				var authorized = await authTask;
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=10");
				Assert.Null(authorized.Result);
			}
			else if (result is Authorized<RemoteWriteAuthObject> authorized)
			{
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=10");
				Assert.Null(authorized.Result);
			}
		}
	}

	/// <summary>
	/// Test remote TrySave with string auth failure - should include error message.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuth_TrySaveMethods_IncludeMessage_WhenRemoteStringAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthObjectFactory>();

		// Find TrySave methods with int? parameter
		// Exclude CancellationToken from meaningful parameter count
		var tryMethods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("TrySave"))
			.Where(m =>
			{
				var meaningful = m.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();
				return meaningful.Length == 2 && meaningful[1].ParameterType == typeof(int?);
			})
			.ToList();

		Assert.NotEmpty(tryMethods);

		foreach (var method in tryMethods)
		{
			var methodName = method.Name;

			// Test with p=20 to trigger string failure on server
			var obj = new RemoteWriteAuthObject { IsNew = true };
			var result = method.Invoke(factory, new object?[] { obj, 20, default(CancellationToken) });

			if (result is Task<Authorized<RemoteWriteAuthObject>> authTask)
			{
				var authorized = await authTask;
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=20");
				Assert.NotNull(authorized.Message);
				Assert.Contains("Denied", authorized.Message);
			}
			else if (result is Authorized<RemoteWriteAuthObject> authorized)
			{
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=20");
				Assert.NotNull(authorized.Message);
				Assert.Contains("Denied", authorized.Message);
			}
		}
	}

	#endregion

	#region Async Remote Authorization Tests

	/// <summary>
	/// Test remote write operations with async authorization methods.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuthAsync_SaveMethodsWork_WhenAuthorized()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthAsyncObjectFactory>();
		var serverAuth = this.serverScope.ServiceProvider.GetRequiredService<RemoteWriteAuthAsync>();

		// Reflection Approved
		var methods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Save") && !m.Name.Contains("Try") && !m.Name.Contains("Can"))
			.ToList();

		Assert.NotEmpty(methods);

		foreach (var method in methods)
		{
			serverAuth.Reset();
			var methodName = method.Name;

			// Test Insert with async remote auth
			var insertObj = new RemoteWriteAuthAsyncObject { IsNew = true };
			var result = await InvokeSaveMethodAsync(method, factory, insertObj);

			if (result != null)
			{
				Assert.True(result.InsertCalled, $"Insert should have been called for {methodName}");
			}

			// Verify async auth was called on server
			Assert.True(serverAuth.CanWriteAsyncCalled > 0 || serverAuth.CanWriteCalled > 0,
				$"Write auth should be checked on server for {methodName}");
			Assert.True(serverAuth.CanInsertAsyncCalled > 0 || serverAuth.CanInsertCalled > 0,
				$"Insert auth should be checked on server for {methodName}");
		}
	}

	/// <summary>
	/// Test remote async authorization failure.
	/// Save methods throw NotAuthorizedException when auth fails.
	/// </summary>
	[Fact]
	public async Task RemoteWriteAuthAsync_SaveFails_WhenAsyncBoolAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteAuthAsyncObjectFactory>();

		// Find methods with int? parameter
		// Exclude CancellationToken from meaningful parameter count
		var methods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Save") &&
			            !m.Name.Contains("Try") &&
			            !m.Name.Contains("Can"))
			.Where(m =>
			{
				var meaningful = m.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();
				return meaningful.Length == 2 && meaningful[1].ParameterType == typeof(int?);
			})
			.ToList();

		Assert.NotEmpty(methods);

		foreach (var method in methods)
		{
			// Test with p=10 to trigger bool failure on server - throws NotAuthorizedException
			var obj = new RemoteWriteAuthAsyncObject { IsNew = true };
			await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
			{
				await InvokeSaveMethodWithParamAsync(method, factory, obj, 10);
			});
		}
	}

	#endregion

	#region Helper Methods

	private static async Task<RemoteWriteAuthObject?> InvokeSaveMethod(
		System.Reflection.MethodInfo method,
		IRemoteWriteAuthObjectFactory factory,
		RemoteWriteAuthObject obj)
	{
		object? result;
		// Exclude CancellationToken from meaningful parameter count
		var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
		if (meaningfulParams.Count == 2)
		{
			result = method.Invoke(factory, new object?[] { obj, 1, default(CancellationToken) }); // Use valid param
		}
		else
		{
			result = method.Invoke(factory, new object?[] { obj, default(CancellationToken) });
		}

		return await ExtractResult(result);
	}

	private static async Task<RemoteWriteAuthObject?> InvokeSaveMethodWithParam(
		System.Reflection.MethodInfo method,
		IRemoteWriteAuthObjectFactory factory,
		RemoteWriteAuthObject obj,
		int? param)
	{
		var result = method.Invoke(factory, new object?[] { obj, param, default(CancellationToken) });
		return await ExtractResult(result);
	}

	private static async Task<RemoteWriteAuthObject?> ExtractResult(object? result)
	{
		if (result is Task<RemoteWriteAuthObject?> taskNullable)
		{
			return await taskNullable;
		}
		else if (result is Task<RemoteWriteAuthObject> task)
		{
			return await task;
		}
		else if (result is RemoteWriteAuthObject obj)
		{
			return obj;
		}
		return null;
	}

	private static async Task<RemoteWriteAuthAsyncObject?> InvokeSaveMethodAsync(
		System.Reflection.MethodInfo method,
		IRemoteWriteAuthAsyncObjectFactory factory,
		RemoteWriteAuthAsyncObject obj)
	{
		object? result;
		// Exclude CancellationToken from meaningful parameter count
		var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
		if (meaningfulParams.Count == 2)
		{
			result = method.Invoke(factory, new object?[] { obj, 1, default(CancellationToken) });
		}
		else
		{
			result = method.Invoke(factory, new object?[] { obj, default(CancellationToken) });
		}

		return await ExtractResultAsync(result);
	}

	private static async Task<RemoteWriteAuthAsyncObject?> InvokeSaveMethodWithParamAsync(
		System.Reflection.MethodInfo method,
		IRemoteWriteAuthAsyncObjectFactory factory,
		RemoteWriteAuthAsyncObject obj,
		int? param)
	{
		var result = method.Invoke(factory, new object?[] { obj, param, default(CancellationToken) });
		return await ExtractResultAsync(result);
	}

	private static async Task<RemoteWriteAuthAsyncObject?> ExtractResultAsync(object? result)
	{
		if (result is Task<RemoteWriteAuthAsyncObject?> taskNullable)
		{
			return await taskNullable;
		}
		else if (result is Task<RemoteWriteAuthAsyncObject> task)
		{
			return await task;
		}
		else if (result is RemoteWriteAuthAsyncObject obj)
		{
			return obj;
		}
		return null;
	}

	#endregion
}
