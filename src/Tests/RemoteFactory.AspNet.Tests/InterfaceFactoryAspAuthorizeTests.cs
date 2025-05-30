using Neatoo.RemoteFactory.AspNetCore.TestLibrary;

namespace RemoteFactory.AspNetCore.Tests;

public class InterfaceAspAuthorizeTests : IClassFixture<ContainerFixture>
{
	private readonly IInterfaceAuthorizeTestObjFactory interfaceObjFactory;
	private readonly IInterfaceAuthorizeTestObj interfaceObj;

	public InterfaceAspAuthorizeTests(ContainerFixture container)
	{
		this.interfaceObjFactory = container.CreateScope.ServiceProvider.GetRequiredService<IInterfaceAuthorizeTestObjFactory>();
		this.interfaceObj = container.CreateScope.ServiceProvider.GetRequiredService<IInterfaceAuthorizeTestObj>();
	}

	[Fact]
	public async Task InterfaceAspAuthorize_HasAspAccess()
	{
		var result = await this.interfaceObj.HasAspAccess(true);
	}

	[Fact]
	public async Task InterfaceAspAuthorize_HasAspAccess_NoNeatooAccess()
	{
		await Assert.ThrowsAsync<HttpRequestException>(() => this.interfaceObj.NoAspAccess(false));
	}

	[Fact]
	public async Task InterfaceAspAuthorize_NoAspAccess()
	{
		await Assert.ThrowsAsync<HttpRequestException>(() => this.interfaceObj.NoAspAccess(true));
	}

	[Fact]
	public async Task InterfaceAspAuthorize_Factory_HasAspAccess()
	{
		var result = await this.interfaceObjFactory.HasAspAccess(true);
	}

	[Fact]
	public async Task InterfaceAspAuthorize_Factory_HasAspAccess_NoNeatooAccess()
	{
		await Assert.ThrowsAsync<HttpRequestException>(() => this.interfaceObjFactory.NoAspAccess(false));
	}

	[Fact]
	public async Task InterfaceAspAuthorize_Factory_NoAspAccess()
	{
		await Assert.ThrowsAsync<HttpRequestException>(() => this.interfaceObjFactory.NoAspAccess(true));
	}

	[Fact]
	public async Task InterfaceAspAuthorize_Factory_CanHasAspAccess()
	{
		Assert.True(await this.interfaceObjFactory.CanHasAspAccess(true));
	}

	[Fact]
	public async Task InterfaceAspAuthorize_Factory_CanNotAuthorized()
	{
		Assert.False(await this.interfaceObjFactory.CanNoAspAccess(true));
	}
}
