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

	// DEPRECATED: Reflection-based tests removed
	// Write authorization tests are now in RemoteFactory.IntegrationTests.Combinations.AuthorizationTests
}
