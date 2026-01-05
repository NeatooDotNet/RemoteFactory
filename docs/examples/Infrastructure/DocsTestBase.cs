using Microsoft.Extensions.DependencyInjection;

namespace Neatoo.RemoteFactory.DocsExamples.Infrastructure;

/// <summary>
/// Base class for documentation example tests.
/// Provides client/server container scopes and the factory under test.
/// Clears mock data stores before each test for isolation.
/// </summary>
public abstract class DocsTestBase<TFactory> : IDisposable
	where TFactory : notnull
{
	protected IServiceScope clientScope;
	protected IServiceScope serverScope;
	protected TFactory factory;

	protected DocsTestBase()
	{
		// Clear all mock stores before each test
		InMemoryPersonContext.ClearStore();

		var scopes = DocsContainers.Scopes();
		this.clientScope = scopes.client;
		this.serverScope = scopes.server;
		this.factory = this.clientScope.ServiceProvider.GetRequiredService<TFactory>();
	}

	public void Dispose()
	{
		clientScope?.Dispose();
		serverScope?.Dispose();
	}
}
