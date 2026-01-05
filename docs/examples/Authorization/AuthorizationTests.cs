using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.DocsExamples.Infrastructure;

namespace Neatoo.RemoteFactory.DocsExamples.Authorization;

#region docs-auth-interface
/// <summary>
/// Authorization interface for domain model access control.
/// Methods are marked with [AuthorizeFactory] to indicate which operations they control.
/// </summary>
public interface IAuthorizedModelAuth
{
	/// <summary>
	/// Check for all read and write operations.
	/// </summary>
	[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
	bool CanAccess();

	/// <summary>
	/// Check specifically for Create operations.
	/// </summary>
	[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
	bool CanCreate();

	/// <summary>
	/// Check specifically for Fetch operations.
	/// </summary>
	[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
	bool CanFetch();

	/// <summary>
	/// Check specifically for Delete operations.
	/// </summary>
	[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
	bool CanDelete();
}
#endregion docs-auth-interface

#region docs-auth-implementation
/// <summary>
/// Implementation of authorization rules.
/// Injected services are resolved from DI.
/// </summary>
public class AuthorizedModelAuth : IAuthorizedModelAuth
{
	private readonly ICurrentUserForAuth _user;

	public AuthorizedModelAuth(ICurrentUserForAuth user)
	{
		_user = user;
	}

	public bool CanAccess() => _user.IsAuthenticated;
	public bool CanCreate() => _user.HasRole("Creator");
	public bool CanFetch() => true;  // Anyone can read
	public bool CanDelete() => _user.HasRole("Admin");
}

/// <summary>
/// Current user interface for authorization.
/// </summary>
public interface ICurrentUserForAuth
{
	bool IsAuthenticated { get; }
	bool HasRole(string role);
}

/// <summary>
/// Mock current user that can be configured for testing.
/// </summary>
public class MockCurrentUserForAuth : ICurrentUserForAuth
{
	public bool IsAuthenticated { get; set; } = true;
	public HashSet<string> Roles { get; set; } = new() { "User" };

	public bool HasRole(string role) => Roles.Contains(role);
}
#endregion docs-auth-implementation

#region docs-auth-model
/// <summary>
/// Domain model with authorization using [AuthorizeFactory<T>].
/// </summary>
public interface IAuthorizedModel : IFactorySaveMeta
{
	int Id { get; set; }
	string? Name { get; set; }
	new bool IsNew { get; set; }
	new bool IsDeleted { get; set; }
}

[Factory]
[AuthorizeFactory<IAuthorizedModelAuth>]
public class AuthorizedModel : IAuthorizedModel
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public bool IsNew { get; set; } = true;
	public bool IsDeleted { get; set; }

	[Create]
	public AuthorizedModel() { }

	[Remote]
	[Fetch]
	public Task<bool> Fetch(int id)
	{
		if (id <= 0) return Task.FromResult(false);
		Id = id;
		Name = $"Item {id}";
		IsNew = false;
		return Task.FromResult(true);
	}

	[Remote]
	[Insert]
	[Update]
	public Task Save()
	{
		if (IsNew)
		{
			Id = new Random().Next(1000, 9999);
		}
		IsNew = false;
		return Task.CompletedTask;
	}

	[Remote]
	[Delete]
	public Task Delete()
	{
		return Task.CompletedTask;
	}
}
#endregion docs-auth-model

#region docs-auth-denied
/// <summary>
/// Authorization that denies write access but allows reads.
/// Useful for testing denial handling.
/// </summary>
public interface IDeniedModelAuth
{
	/// <summary>
	/// Allow read operations (Create, Fetch).
	/// </summary>
	[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
	bool CanRead();

	/// <summary>
	/// Deny all write operations (Insert, Update, Delete).
	/// </summary>
	[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
	bool CanWrite();
}

public class DeniedModelAuth : IDeniedModelAuth
{
	public bool CanRead() => true;   // Allow creating objects
	public bool CanWrite() => false;  // Deny saving
}

public interface IDeniedModel : IFactorySaveMeta
{
	int Id { get; set; }
	new bool IsNew { get; set; }
	new bool IsDeleted { get; set; }
}

[Factory]
[AuthorizeFactory<IDeniedModelAuth>]
public class DeniedModel : IDeniedModel
{
	public int Id { get; set; }
	public bool IsNew { get; set; } = true;
	public bool IsDeleted { get; set; }

	[Create]
	public DeniedModel() { }

	[Remote]
	[Insert]
	[Update]
	public Task Save() => Task.CompletedTask;

	[Remote]
	[Delete]
	public Task Delete() => Task.CompletedTask;
}
#endregion docs-auth-denied

/// <summary>
/// Tests for authorization documentation examples.
/// </summary>
public class AuthorizationTests : DocsTestBase<IAuthorizedModelFactory>
{
	private MockCurrentUserForAuth GetMockUser()
	{
		// Get mock user from both scopes and configure them the same way
		var clientUser = clientScope.ServiceProvider.GetRequiredService<ICurrentUserForAuth>() as MockCurrentUserForAuth;
		var serverUser = serverScope.ServiceProvider.GetRequiredService<ICurrentUserForAuth>() as MockCurrentUserForAuth;
		// Configure both - Can* runs on client, actual methods run on server
		return clientUser!;
	}

	private void SetupBothUsers(Action<MockCurrentUserForAuth> configure)
	{
		var clientUser = clientScope.ServiceProvider.GetRequiredService<ICurrentUserForAuth>() as MockCurrentUserForAuth;
		var serverUser = serverScope.ServiceProvider.GetRequiredService<ICurrentUserForAuth>() as MockCurrentUserForAuth;
		configure(clientUser!);
		configure(serverUser!);
	}

	[Fact]
	public void CanCreate_WithCreatorRole_ReturnsTrue()
	{
		// Configure mock user with Creator role on both scopes
		SetupBothUsers(user => user.Roles.Add("Creator"));

		// CanCreate checks authorization without executing
		var canCreate = factory.CanCreate();

		Assert.True(canCreate.HasAccess);
	}

	[Fact]
	public void CanCreate_WithoutCreatorRole_ReturnsFalse()
	{
		// Mock user doesn't have Creator role
		SetupBothUsers(user =>
		{
			user.Roles.Clear();
			user.Roles.Add("User");
		});

		var canCreate = factory.CanCreate();

		Assert.False(canCreate.HasAccess);
	}

	[Fact]
	public void CanFetch_AlwaysReturnsTrue()
	{
		// Fetch is allowed for everyone
		var canFetch = factory.CanFetch();

		Assert.True(canFetch.HasAccess);
	}

	[Fact]
	public void CanSave_WhenAuthenticated_ReturnsTrue()
	{
		// CanSave checks Insert/Update/Delete authorization
		var canSave = factory.CanSave();

		Assert.True(canSave.HasAccess);
	}

	[Fact]
	public async Task Fetch_WhenAuthorized_ReturnsResult()
	{
		var result = await factory.Fetch(42);

		Assert.NotNull(result);
		Assert.Equal(42, result.Id);
	}

	[Fact]
	public async Task TrySave_WhenAuthorized_ReturnsResultWithAccess()
	{
		// Setup: add Creator role on both client and server
		SetupBothUsers(user => user.Roles.Add("Creator"));

		var model = factory.Create();
		Assert.NotNull(model);
		model.Name = "Test";

		// TrySave returns Authorized<T> for checking success
		var result = await factory.TrySave(model);

		Assert.True(result.HasAccess);
		Assert.NotNull(result.Result);
		Assert.True(result.Result!.Id > 0);
	}
}

/// <summary>
/// Tests for authorization denial handling.
/// DeniedModel allows Read (Create) but denies Write (Save).
/// </summary>
public class AuthorizationDenialTests : DocsTestBase<IDeniedModelFactory>
{
	[Fact]
	public void CanCreate_WhenAllowed_ReturnsTrue()
	{
		// Create is a Read operation and allowed
		var canCreate = factory.CanCreate();

		Assert.True(canCreate.HasAccess);
	}

	[Fact]
	public void CanSave_WhenDenied_ReturnsFalse()
	{
		// Save is a Write operation and denied
		var canSave = factory.CanSave();

		Assert.False(canSave.HasAccess);
	}

	[Fact]
	public async Task TrySave_WhenDenied_ReturnsNoAccess()
	{
		// Create works (Read allowed)
		var model = factory.Create();
		Assert.NotNull(model);

		// TrySave doesn't throw - returns result to check
		var result = await factory.TrySave(model);

		Assert.False(result.HasAccess);
		Assert.Null(result.Result);
	}

	[Fact]
	public async Task Save_WhenDenied_ThrowsNotAuthorizedException()
	{
		// Create works (Read allowed)
		var model = factory.Create();
		Assert.NotNull(model);

		// Save throws NotAuthorizedException when denied
		await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.Save(model));
	}
}
