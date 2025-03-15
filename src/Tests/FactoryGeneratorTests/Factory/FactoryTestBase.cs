using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

 public abstract class FactoryTestBase<TFactory>
	where TFactory : notnull
 {

	protected IServiceScope clientScope;
	protected IServiceScope serverScope;
	protected TFactory factory;

	public FactoryTestBase()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
		this.serverScope = scopes.server;
		this.factory = this.clientScope.ServiceProvider.GetRequiredService<TFactory>();
	}
}
