using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.AspNetCore;

namespace RemoteFactory.AspNetCore.Tests;

public class AspAuthorizeTests : IClassFixture<WebApplicationFactory<Program>>
{
	private readonly WebApplicationFactory<Program> _factory;

	public AspAuthorizeTests(WebApplicationFactory<Program> factory)
	{
		this._factory = factory;
	}

	[Fact]
	public async Task Start()
	{
		using var client = this._factory.CreateClient();

		using var response = await client.GetAsync("/");
	}
}
