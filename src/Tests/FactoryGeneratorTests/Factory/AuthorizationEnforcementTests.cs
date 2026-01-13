using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests that verify authorization is ENFORCED (not just checked) for both
/// class-based and interface-based factories. These tests exist specifically
/// to catch regressions where authorization code generation is accidentally removed.
///
/// Both factory types should throw NotAuthorizedException when authorization fails.
/// </summary>

#region Authorization Class

/// <summary>
/// Authorization class that can be configured to allow or deny access.
/// </summary>
public class EnforcementTestAuth
{
	public static bool ShouldAllow { get; set; } = true;

	[AuthorizeFactory(AuthorizeFactoryOperation.Execute | AuthorizeFactoryOperation.Read)]
	public bool CanExecute()
	{
		return ShouldAllow;
	}
}

#endregion

#region Class-Based Factory Target

[Factory]
[AuthorizeFactory<EnforcementTestAuth>]
public class ClassBasedAuthTarget
{
	public string Value { get; }

	[Fetch]
	public ClassBasedAuthTarget()
	{
		Value = "Fetched";
	}
}

#endregion

#region Interface-Based Factory Target

[Factory]
[AuthorizeFactory<EnforcementTestAuth>]
public interface IInterfaceBasedAuthTarget
{
	Task<string> GetValue();
}

/// <summary>
/// Named to match IInterfaceBasedAuthTarget for RegisterMatchingName pattern.
/// </summary>
public class InterfaceBasedAuthTarget : IInterfaceBasedAuthTarget
{
	public Task<string> GetValue()
	{
		return Task.FromResult("FromInterface");
	}
}

#endregion

#region Tests

public class AuthorizationEnforcementTests : IDisposable
{
	private readonly IServiceScope clientScope;
	private readonly IClassBasedAuthTargetFactory classFactory;
	private readonly IInterfaceBasedAuthTargetFactory interfaceFactory;

	public AuthorizationEnforcementTests()
	{
		var (client, _, _) = ClientServerContainers.Scopes();
		this.clientScope = client;
		this.classFactory = clientScope.ServiceProvider.GetRequiredService<IClassBasedAuthTargetFactory>();
		this.interfaceFactory = clientScope.ServiceProvider.GetRequiredService<IInterfaceBasedAuthTargetFactory>();

		// Reset to allowed state
		EnforcementTestAuth.ShouldAllow = true;
	}

	public void Dispose()
	{
		// Reset to allowed state for other tests
		EnforcementTestAuth.ShouldAllow = true;
		clientScope.Dispose();
	}

	#region Class-Based Factory Tests

	[Fact]
	public void ClassFactory_WhenAuthorized_ReturnsResult()
	{
		EnforcementTestAuth.ShouldAllow = true;

		var result = classFactory.Fetch();

		Assert.NotNull(result);
		Assert.Equal("Fetched", result.Value);
	}

	[Fact]
	public void ClassFactory_WhenNotAuthorized_ReturnsNull()
	{
		EnforcementTestAuth.ShouldAllow = false;

		// Class-based factories return null when auth fails (via Authorized<T>.Result)
		var result = classFactory.Fetch();

		Assert.Null(result);
	}

	[Fact]
	public void ClassFactory_CanMethod_ReflectsAuthState()
	{
		EnforcementTestAuth.ShouldAllow = true;
		Assert.True(classFactory.CanFetch().HasAccess);

		EnforcementTestAuth.ShouldAllow = false;
		Assert.False(classFactory.CanFetch().HasAccess);
	}

	#endregion

	#region Interface-Based Factory Tests

	[Fact]
	public async Task InterfaceFactory_WhenAuthorized_ReturnsResult()
	{
		EnforcementTestAuth.ShouldAllow = true;

		var result = await interfaceFactory.GetValue();

		Assert.Equal("FromInterface", result);
	}

	[Fact]
	public async Task InterfaceFactory_WhenNotAuthorized_ThrowsNotAuthorizedException()
	{
		EnforcementTestAuth.ShouldAllow = false;

		// Interface-based factories throw NotAuthorizedException when auth fails
		await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
		{
			await interfaceFactory.GetValue();
		});
	}

	[Fact]
	public void InterfaceFactory_CanMethod_ReflectsAuthState()
	{
		EnforcementTestAuth.ShouldAllow = true;
		Assert.True(interfaceFactory.CanGetValue().HasAccess);

		EnforcementTestAuth.ShouldAllow = false;
		Assert.False(interfaceFactory.CanGetValue().HasAccess);
	}

	#endregion

	#region Side-by-Side Comparison Tests

	/// <summary>
	/// This test verifies that BOTH factory types enforce authorization.
	/// If either factory type stops enforcing authorization (like the regression
	/// where InterfaceFactoryMethod.LocalMethod stopped including auth checks),
	/// this test will fail.
	/// </summary>
	[Fact]
	public async Task BothFactoryTypes_EnforceAuthorization_WhenDenied()
	{
		EnforcementTestAuth.ShouldAllow = false;

		// Class-based: returns null when auth fails
		var classResult = classFactory.Fetch();
		Assert.Null(classResult);

		// Interface-based: throws NotAuthorizedException when auth fails
		var interfaceException = await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
		{
			await interfaceFactory.GetValue();
		});
		Assert.NotNull(interfaceException);
	}

	/// <summary>
	/// Verifies that both factory types succeed when authorization passes.
	/// </summary>
	[Fact]
	public async Task BothFactoryTypes_Succeed_WhenAuthorized()
	{
		EnforcementTestAuth.ShouldAllow = true;

		// Class-based: returns result
		var classResult = classFactory.Fetch();
		Assert.NotNull(classResult);
		Assert.Equal("Fetched", classResult.Value);

		// Interface-based: returns result
		var interfaceResult = await interfaceFactory.GetValue();
		Assert.Equal("FromInterface", interfaceResult);
	}

	#endregion
}

#endregion
