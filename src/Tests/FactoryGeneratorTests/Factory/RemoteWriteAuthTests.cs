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

	// DEPRECATED: Reflection-based tests removed
	// Remote write authorization tests are now in RemoteFactory.IntegrationTests.Combinations.AuthorizationTests
}
