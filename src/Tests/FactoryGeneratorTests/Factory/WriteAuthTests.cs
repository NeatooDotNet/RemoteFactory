using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for write operations (Insert, Update, Delete) with [AuthorizeFactory] authorization.
/// This addresses GAP-001 from the test plan: Write Operations with Authorization - completely untested.
///
/// Security-critical tests verifying:
/// - Authorization is checked before Insert/Update/Delete operations
/// - Auth returning false prevents the operation
/// - Auth with parameters matching operation parameters
/// - Multiple auth methods for same operation type
/// - AuthorizeFactoryOperation.Write covers all write ops (Insert, Update, Delete)
/// - CanInsert, CanUpdate, CanDelete, CanSave method generation
/// - TrySave with partial authorization failure
/// </summary>
public class WriteAuthTests
{
	#region Authorization Classes

	/// <summary>
	/// Synchronous authorization class for write operations.
	/// Tests bool and string return types for authorization.
	/// Parameterless methods are always called; parameterized methods only called
	/// when the operation has matching parameters.
	/// </summary>
	public class WriteAuth
	{
		// Tracking properties to verify auth methods are called
		public int CanWriteCalled { get; set; }
		public int CanInsertCalled { get; set; }
		public int CanUpdateCalled { get; set; }
		public int CanDeleteCalled { get; set; }

		// Write authorization - applies to Insert, Update, and Delete
		// Parameterless - always called
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public bool CanWriteBool()
		{
			this.CanWriteCalled++;
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public string? CanWriteString()
		{
			this.CanWriteCalled++;
			return string.Empty; // Empty string means authorized
		}

		// Parameterized - called when operation has int? param
		// p == 10 causes bool failure, p == 20 causes string failure
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public bool CanWriteBoolFail(int? p)
		{
			this.CanWriteCalled++;
			return p != 10;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public string? CanWriteStringFail(int? p)
		{
			this.CanWriteCalled++;
			return p == 20 ? "WriteDenied" : string.Empty;
		}

		// Insert-specific authorization
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public bool CanInsertBool()
		{
			this.CanInsertCalled++;
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public string? CanInsertString()
		{
			this.CanInsertCalled++;
			return string.Empty;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public bool CanInsertBoolFail(int? p)
		{
			this.CanInsertCalled++;
			return p != 10;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public string? CanInsertStringFail(int? p)
		{
			this.CanInsertCalled++;
			return p == 20 ? "InsertDenied" : string.Empty;
		}

		// Update-specific authorization
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public bool CanUpdateBool()
		{
			this.CanUpdateCalled++;
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public string? CanUpdateString()
		{
			this.CanUpdateCalled++;
			return string.Empty;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public bool CanUpdateBoolFail(int? p)
		{
			this.CanUpdateCalled++;
			return p != 10;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public string? CanUpdateStringFail(int? p)
		{
			this.CanUpdateCalled++;
			return p == 20 ? "UpdateDenied" : string.Empty;
		}

		// Delete-specific authorization
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public bool CanDeleteBool()
		{
			this.CanDeleteCalled++;
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public string? CanDeleteString()
		{
			this.CanDeleteCalled++;
			return string.Empty;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public bool CanDeleteBoolFail(int? p)
		{
			this.CanDeleteCalled++;
			return p != 10;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public string? CanDeleteStringFail(int? p)
		{
			this.CanDeleteCalled++;
			return p == 20 ? "DeleteDenied" : string.Empty;
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
	/// Async authorization class for write operations.
	/// Tests Task<bool> and Task<string> return types.
	/// </summary>
	public class WriteAuthAsync : WriteAuth
	{
		public int CanWriteAsyncCalled { get; set; }
		public int CanInsertAsyncCalled { get; set; }
		public int CanUpdateAsyncCalled { get; set; }
		public int CanDeleteAsyncCalled { get; set; }

		// Async Write authorization - parameterless
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public Task<bool> CanWriteBoolAsync()
		{
			this.CanWriteAsyncCalled++;
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public Task<string> CanWriteStringAsync()
		{
			this.CanWriteAsyncCalled++;
			return Task.FromResult(string.Empty);
		}

		// Async Write authorization - parameterized
		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public Task<bool> CanWriteBoolFailAsync(int? p)
		{
			this.CanWriteAsyncCalled++;
			return Task.FromResult(p != 10);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public Task<string> CanWriteStringFailAsync(int? p)
		{
			this.CanWriteAsyncCalled++;
			return Task.FromResult(p == 20 ? "WriteAsyncDenied" : string.Empty);
		}

		// Async Insert authorization
		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public Task<bool> CanInsertBoolAsync()
		{
			this.CanInsertAsyncCalled++;
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public Task<string> CanInsertStringAsync()
		{
			this.CanInsertAsyncCalled++;
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public Task<bool> CanInsertBoolFailAsync(int? p)
		{
			this.CanInsertAsyncCalled++;
			return Task.FromResult(p != 10);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
		public Task<string> CanInsertStringFailAsync(int? p)
		{
			this.CanInsertAsyncCalled++;
			return Task.FromResult(p == 20 ? "InsertAsyncDenied" : string.Empty);
		}

		// Async Update authorization
		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public Task<bool> CanUpdateBoolAsync()
		{
			this.CanUpdateAsyncCalled++;
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public Task<string> CanUpdateStringAsync()
		{
			this.CanUpdateAsyncCalled++;
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public Task<bool> CanUpdateBoolFailAsync(int? p)
		{
			this.CanUpdateAsyncCalled++;
			return Task.FromResult(p != 10);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
		public Task<string> CanUpdateStringFailAsync(int? p)
		{
			this.CanUpdateAsyncCalled++;
			return Task.FromResult(p == 20 ? "UpdateAsyncDenied" : string.Empty);
		}

		// Async Delete authorization
		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public Task<bool> CanDeleteBoolAsync()
		{
			this.CanDeleteAsyncCalled++;
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public Task<string> CanDeleteStringAsync()
		{
			this.CanDeleteAsyncCalled++;
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public Task<bool> CanDeleteBoolFailAsync(int? p)
		{
			this.CanDeleteAsyncCalled++;
			return Task.FromResult(p != 10);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
		public Task<string> CanDeleteStringFailAsync(int? p)
		{
			this.CanDeleteAsyncCalled++;
			return Task.FromResult(p == 20 ? "DeleteAsyncDenied" : string.Empty);
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
	/// Domain object with synchronous authorization for write operations.
	/// Uses only parameterized methods to test the auth with matching params.
	/// </summary>
	[Factory]
	[AuthorizeFactory<WriteAuth>]
	public class WriteAuthObject : IFactorySaveMeta
	{
		public bool IsDeleted { get; set; }
		public bool IsNew { get; set; }

		// Self-tracking properties to verify method calls
		public bool InsertCalled { get; set; }
		public bool UpdateCalled { get; set; }
		public bool DeleteCalled { get; set; }

		// All write methods have int? param to allow testing auth failure scenarios
		[Insert]
		public void InsertVoid(int? param)
		{
			this.InsertCalled = true;
		}

		[Insert]
		public bool InsertBool(int? param)
		{
			this.InsertCalled = true;
			return true;
		}

		[Insert]
		public Task InsertTask(int? param)
		{
			this.InsertCalled = true;
			return Task.CompletedTask;
		}

		[Insert]
		public Task<bool> InsertTaskBool(int? param)
		{
			this.InsertCalled = true;
			return Task.FromResult(true);
		}

		[Update]
		public void UpdateVoid(int? param)
		{
			this.UpdateCalled = true;
		}

		[Update]
		public bool UpdateBool(int? param)
		{
			this.UpdateCalled = true;
			return true;
		}

		[Update]
		public Task UpdateTask(int? param)
		{
			this.UpdateCalled = true;
			return Task.CompletedTask;
		}

		[Update]
		public Task<bool> UpdateTaskBool(int? param)
		{
			this.UpdateCalled = true;
			return Task.FromResult(true);
		}

		[Delete]
		public void DeleteVoid(int? param)
		{
			this.DeleteCalled = true;
		}

		[Delete]
		public bool DeleteBool(int? param)
		{
			this.DeleteCalled = true;
			return true;
		}

		[Delete]
		public Task DeleteTask(int? param)
		{
			this.DeleteCalled = true;
			return Task.CompletedTask;
		}

		[Delete]
		public Task<bool> DeleteTaskBool(int? param)
		{
			this.DeleteCalled = true;
			return Task.FromResult(true);
		}
	}

	/// <summary>
	/// Domain object with async authorization for write operations.
	/// Uses only parameterized methods.
	/// </summary>
	[Factory]
	[AuthorizeFactory<WriteAuthAsync>]
	public class WriteAuthAsyncObject : IFactorySaveMeta
	{
		public bool IsDeleted { get; set; }
		public bool IsNew { get; set; }

		public bool InsertCalled { get; set; }
		public bool UpdateCalled { get; set; }
		public bool DeleteCalled { get; set; }

		[Insert]
		public void InsertVoid(int? param)
		{
			this.InsertCalled = true;
		}

		[Insert]
		public Task InsertTask(int? param)
		{
			this.InsertCalled = true;
			return Task.CompletedTask;
		}

		[Update]
		public void UpdateVoid(int? param)
		{
			this.UpdateCalled = true;
		}

		[Update]
		public Task UpdateTask(int? param)
		{
			this.UpdateCalled = true;
			return Task.CompletedTask;
		}

		[Delete]
		public void DeleteVoid(int? param)
		{
			this.DeleteCalled = true;
		}

		[Delete]
		public Task DeleteTask(int? param)
		{
			this.DeleteCalled = true;
			return Task.CompletedTask;
		}
	}

	#endregion

	#region Test Setup

	private readonly IServiceScope clientScope;

	public WriteAuthTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
	}

	#endregion

	#region Synchronous Authorization Tests

	/// <summary>
	/// Test that all Save methods are generated and work correctly with authorization passing.
	/// Tests Insert (IsNew=true), Update (IsNew=false, IsDeleted=false), and Delete (IsDeleted=true) routing.
	/// </summary>
	[Fact]
	public async Task WriteAuth_SaveMethodsWork_WhenAuthorized()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuth>();

		// Reflection Approved
		var methods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Save") && !m.Name.Contains("Try") && !m.Name.Contains("Can"))
			.ToList();

		Assert.NotEmpty(methods);

		foreach (var method in methods)
		{
			auth.Reset();
			var methodName = method.Name;

			// Test Insert (IsNew = true)
			var insertObj = new WriteAuthObject { IsNew = true };
			var insertResult = await InvokeSaveMethod(method, factory, insertObj);

			if (insertResult != null)
			{
				Assert.True(insertResult.InsertCalled, $"Insert should have been called for {methodName}");
			}

			// Verify Write and Insert auth were checked
			Assert.True(auth.CanWriteCalled > 0, $"Write auth should be checked for Insert in {methodName}");
			Assert.True(auth.CanInsertCalled > 0, $"Insert auth should be checked for {methodName}");

			auth.Reset();

			// Test Update (IsNew = false, IsDeleted = false)
			var updateObj = new WriteAuthObject { IsNew = false, IsDeleted = false };
			var updateResult = await InvokeSaveMethod(method, factory, updateObj);

			if (updateResult != null)
			{
				Assert.True(updateResult.UpdateCalled, $"Update should have been called for {methodName}");
			}

			// Verify Write and Update auth were checked
			Assert.True(auth.CanWriteCalled > 0, $"Write auth should be checked for Update in {methodName}");
			Assert.True(auth.CanUpdateCalled > 0, $"Update auth should be checked for {methodName}");

			auth.Reset();

			// Test Delete (IsDeleted = true)
			var deleteObj = new WriteAuthObject { IsDeleted = true };
			var deleteResult = await InvokeSaveMethod(method, factory, deleteObj);

			if (deleteResult != null)
			{
				Assert.True(deleteResult.DeleteCalled, $"Delete should have been called for {methodName}");
			}

			// Verify Write and Delete auth were checked
			Assert.True(auth.CanWriteCalled > 0, $"Write auth should be checked for Delete in {methodName}");
			Assert.True(auth.CanDeleteCalled > 0, $"Delete auth should be checked for {methodName}");
		}
	}

	/// <summary>
	/// Test that authorization failure (bool returning false) prevents the operation.
	/// Uses p=10 to trigger bool authorization failure.
	/// Save methods throw NotAuthorizedException when auth fails.
	/// </summary>
	[Fact]
	public async Task WriteAuth_SaveFails_WhenBoolAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuth>();

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
			auth.Reset();
			var methodName = method.Name;

			// Test Insert with auth failure (p=10) - should throw NotAuthorizedException
			var insertObj = new WriteAuthObject { IsNew = true };
			var ex = await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
			{
				await InvokeSaveMethodWithParam(method, factory, insertObj, 10);
			});

			// Verify Insert was NOT called because auth failed
			Assert.False(insertObj.InsertCalled, $"Insert should NOT be called when auth fails for {methodName}");
		}
	}

	/// <summary>
	/// Test that authorization failure (string returning message) prevents the operation.
	/// Uses p=20 to trigger string authorization failure.
	/// Save methods throw NotAuthorizedException with the error message.
	/// </summary>
	[Fact]
	public async Task WriteAuth_SaveFails_WhenStringAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuth>();

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
			auth.Reset();
			var methodName = method.Name;

			// Test Update with auth failure (p=20) - should throw NotAuthorizedException with message
			var updateObj = new WriteAuthObject { IsNew = false, IsDeleted = false };
			var ex = await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
			{
				await InvokeSaveMethodWithParam(method, factory, updateObj, 20);
			});

			// Verify Update was NOT called because auth failed
			Assert.False(updateObj.UpdateCalled, $"Update should NOT be called when auth fails for {methodName}");
			// Verify error message is included
			Assert.Contains("Denied", ex.Message);
		}
	}

	/// <summary>
	/// Test CanInsert, CanUpdate, CanDelete, CanSave method generation.
	/// These methods should check authorization without performing the operation.
	/// </summary>
	[Fact]
	public async Task WriteAuth_CanMethods_CheckAuthorizationOnly()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuth>();

		// Find Can methods
		var canMethods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Can"))
			.ToList();

		Assert.NotEmpty(canMethods);

		foreach (var method in canMethods)
		{
			auth.Reset();
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

			// Can methods should return Authorized or Task<Authorized>
			if (result is Task<Authorized> authTask)
			{
				var authorized = await authTask;
				Assert.True(authorized.HasAccess, $"{methodName} should return HasAccess=true for valid params");
			}
			else if (result is Authorized authorized)
			{
				Assert.True(authorized.HasAccess, $"{methodName} should return HasAccess=true for valid params");
			}
			else
			{
				Assert.Fail($"Unexpected return type from {methodName}");
			}

			// Auth methods should have been called
			Assert.True(auth.CanWriteCalled > 0, $"Write auth should be checked for {methodName}");
		}
	}

	/// <summary>
	/// Test CanInsert/Update/Delete with auth failure.
	/// </summary>
	[Fact]
	public async Task WriteAuth_CanMethods_ReturnFalse_WhenAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuth>();

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
			auth.Reset();
			var methodName = method.Name;

			// Test with p=10 to trigger bool failure
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

	/// <summary>
	/// Test TrySave methods that return Authorized<T> with authorization result.
	/// </summary>
	[Fact]
	public async Task WriteAuth_TrySaveMethods_ReturnAuthorizedResult()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuth>();

		// Find TrySave methods
		var tryMethods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("TrySave"))
			.ToList();

		Assert.NotEmpty(tryMethods);

		foreach (var method in tryMethods)
		{
			auth.Reset();
			var methodName = method.Name;

			// Test successful authorization
			var obj = new WriteAuthObject { IsNew = true };
			// Exclude CancellationToken from meaningful parameter count
			var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
			object?[] parameters = meaningfulParams.Count == 2
				? new object?[] { obj, 1, default(CancellationToken) }
				: new object?[] { obj, default(CancellationToken) };

			var result = method.Invoke(factory, parameters);

			if (result is Task<Authorized<WriteAuthObject>> authTask)
			{
				var authorized = await authTask;
				Assert.True(authorized.HasAccess, $"{methodName} should return HasAccess=true");
				Assert.NotNull(authorized.Result);
			}
			else if (result is Authorized<WriteAuthObject> authorized)
			{
				Assert.True(authorized.HasAccess, $"{methodName} should return HasAccess=true");
				Assert.NotNull(authorized.Result);
			}
		}
	}

	/// <summary>
	/// Test TrySave with authorization failure - should return HasAccess=false.
	/// </summary>
	[Fact]
	public async Task WriteAuth_TrySaveMethods_ReturnFailure_WhenAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuth>();

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
			auth.Reset();
			var methodName = method.Name;

			// Test with p=10 to trigger bool failure
			var obj = new WriteAuthObject { IsNew = true };
			var result = method.Invoke(factory, new object?[] { obj, 10, default(CancellationToken) });

			if (result is Task<Authorized<WriteAuthObject>> authTask)
			{
				var authorized = await authTask;
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=10");
				Assert.Null(authorized.Result);
			}
			else if (result is Authorized<WriteAuthObject> authorized)
			{
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=10");
				Assert.Null(authorized.Result);
			}
		}
	}

	/// <summary>
	/// Test TrySave with string auth failure - should include error message.
	/// </summary>
	[Fact]
	public async Task WriteAuth_TrySaveMethods_IncludeMessage_WhenStringAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuth>();

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
			auth.Reset();
			var methodName = method.Name;

			// Test with p=20 to trigger string failure
			var obj = new WriteAuthObject { IsNew = true };
			var result = method.Invoke(factory, new object?[] { obj, 20, default(CancellationToken) });

			if (result is Task<Authorized<WriteAuthObject>> authTask)
			{
				var authorized = await authTask;
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=20");
				Assert.NotNull(authorized.Message);
				Assert.Contains("Denied", authorized.Message);
			}
			else if (result is Authorized<WriteAuthObject> authorized)
			{
				Assert.False(authorized.HasAccess, $"{methodName} should return HasAccess=false for p=20");
				Assert.NotNull(authorized.Message);
				Assert.Contains("Denied", authorized.Message);
			}
		}
	}

	#endregion

	#region Async Authorization Tests

	/// <summary>
	/// Test that async authorization methods (Task<bool>, Task<string>) work correctly.
	/// </summary>
	[Fact]
	public async Task WriteAuthAsync_SaveMethodsWork_WhenAuthorized()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthAsyncObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuthAsync>();

		// Reflection Approved
		var methods = factory.GetType().GetMethods()
			.Where(m => m.Name.StartsWith("Save") && !m.Name.Contains("Try") && !m.Name.Contains("Can"))
			.ToList();

		Assert.NotEmpty(methods);

		foreach (var method in methods)
		{
			auth.Reset();
			var methodName = method.Name;

			// Test Insert with async auth
			var insertObj = new WriteAuthAsyncObject { IsNew = true };
			var result = await InvokeSaveMethodAsync(method, factory, insertObj);

			if (result != null)
			{
				Assert.True(result.InsertCalled, $"Insert should have been called for {methodName}");
			}

			// Verify async auth methods were called
			Assert.True(auth.CanWriteAsyncCalled > 0 || auth.CanWriteCalled > 0,
				$"Write auth should be checked for {methodName}");
			Assert.True(auth.CanInsertAsyncCalled > 0 || auth.CanInsertCalled > 0,
				$"Insert auth should be checked for {methodName}");
		}
	}

	/// <summary>
	/// Test async authorization failure.
	/// Save methods throw NotAuthorizedException when auth fails.
	/// </summary>
	[Fact]
	public async Task WriteAuthAsync_SaveFails_WhenAsyncBoolAuthFails()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<IWriteAuthAsyncObjectFactory>();
		var auth = this.clientScope.ServiceProvider.GetRequiredService<WriteAuthAsync>();

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
			auth.Reset();

			// Test with p=10 to trigger bool failure - should throw NotAuthorizedException
			var obj = new WriteAuthAsyncObject { IsNew = true };
			await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
			{
				await InvokeSaveMethodWithParamAsync(method, factory, obj, 10);
			});

			// Verify Insert was NOT called because auth failed
			Assert.False(obj.InsertCalled, $"Insert should NOT be called when async auth fails for {method.Name}");
		}
	}

	#endregion

	#region Helper Methods

	private static async Task<WriteAuthObject?> InvokeSaveMethod(
		System.Reflection.MethodInfo method,
		IWriteAuthObjectFactory factory,
		WriteAuthObject obj)
	{
		object? result;
		try
		{
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
		}
		catch (System.Reflection.TargetInvocationException ex)
		{
			// Unwrap the inner exception thrown during reflection invoke
			if (ex.InnerException != null)
			{
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			}
			throw;
		}

		return await ExtractResult(result);
	}

	private static async Task<WriteAuthObject?> InvokeSaveMethodWithParam(
		System.Reflection.MethodInfo method,
		IWriteAuthObjectFactory factory,
		WriteAuthObject obj,
		int? param)
	{
		object? result;
		try
		{
			result = method.Invoke(factory, new object?[] { obj, param, default(CancellationToken) });
		}
		catch (System.Reflection.TargetInvocationException ex)
		{
			// Unwrap the inner exception thrown during reflection invoke
			if (ex.InnerException != null)
			{
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			}
			throw;
		}

		return await ExtractResult(result);
	}

	private static async Task<WriteAuthObject?> ExtractResult(object? result)
	{
		if (result is Task<WriteAuthObject?> taskNullable)
		{
			return await taskNullable;
		}
		else if (result is Task<WriteAuthObject> task)
		{
			return await task;
		}
		else if (result is WriteAuthObject obj)
		{
			return obj;
		}
		return null;
	}

	private static async Task<WriteAuthAsyncObject?> InvokeSaveMethodAsync(
		System.Reflection.MethodInfo method,
		IWriteAuthAsyncObjectFactory factory,
		WriteAuthAsyncObject obj)
	{
		object? result;
		try
		{
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
		}
		catch (System.Reflection.TargetInvocationException ex)
		{
			// Unwrap the inner exception thrown during reflection invoke
			if (ex.InnerException != null)
			{
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			}
			throw;
		}

		return await ExtractResultAsync(result);
	}

	private static async Task<WriteAuthAsyncObject?> InvokeSaveMethodWithParamAsync(
		System.Reflection.MethodInfo method,
		IWriteAuthAsyncObjectFactory factory,
		WriteAuthAsyncObject obj,
		int? param)
	{
		object? result;
		try
		{
			result = method.Invoke(factory, new object?[] { obj, param, default(CancellationToken) });
		}
		catch (System.Reflection.TargetInvocationException ex)
		{
			// Unwrap the inner exception thrown during reflection invoke
			if (ex.InnerException != null)
			{
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			}
			throw;
		}

		return await ExtractResultAsync(result);
	}

	private static async Task<WriteAuthAsyncObject?> ExtractResultAsync(object? result)
	{
		if (result is Task<WriteAuthAsyncObject?> taskNullable)
		{
			return await taskNullable;
		}
		else if (result is Task<WriteAuthAsyncObject> task)
		{
			return await task;
		}
		else if (result is WriteAuthAsyncObject obj)
		{
			return obj;
		}
		return null;
	}

	#endregion
}
