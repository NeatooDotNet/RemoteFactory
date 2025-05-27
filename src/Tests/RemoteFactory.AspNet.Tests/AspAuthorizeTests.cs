using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore.TestLibrary;
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
		var serviceCollection = new ServiceCollection();

		serviceCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(AspAuthorizeTestObj).Assembly);
		serviceCollection.AddKeyedScoped(Neatoo.RemoteFactory.RemoteFactoryServices.HttpClientKey, (sp, key) =>
		{
			return this._factory.CreateClient();
		});

		var services = serviceCollection.BuildServiceProvider();

		var objFactory = services.GetRequiredService<IAspAuthorizeTestObjFactory>();

		var result = await objFactory.Create();

		var noAuthResult = await objFactory.CreateNoAuth();

	}
}
