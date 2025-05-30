using Microsoft.AspNetCore.Mvc.Testing;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore.TestLibrary;

namespace RemoteFactory.AspNetCore.Tests;

public class ContainerFixture : WebApplicationFactory<Program>, IDisposable
{
   private readonly ServiceProvider serviceProvider;

	public ContainerFixture()
	{
		var serviceCollection = new ServiceCollection();

		serviceCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(AspAuthorizeTestObj).Assembly);
		serviceCollection.AddKeyedScoped(Neatoo.RemoteFactory.RemoteFactoryServices.HttpClientKey, (sp, key) =>
		{
			return this.CreateClient();
		});

		this.serviceProvider = serviceCollection.BuildServiceProvider();
		
	}

	public IServiceScope CreateScope => this.serviceProvider.CreateScope();

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}
}
