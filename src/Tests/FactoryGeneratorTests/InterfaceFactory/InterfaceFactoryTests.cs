using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.InterfaceFactory;

[Factory]
public interface  IExecuteMethods
{
	Task<bool> BoolMethod(bool a, string b);
	Task<List<string>> StringListMethod(List<string> a, int b);
}

// This should only exist on the server and be registered in the server's DI container
// with TestInterfaceFactory as the implementation of ITestInterfaceFactory
public class ExecuteMethods : IExecuteMethods
{
   private readonly IServerOnlyService serverOnlyService;

	// This constructor is just to show that the implementation can have dependencies
	// that are only available on the server side.
	public ExecuteMethods(IServerOnlyService serverOnlyService)
	{
	  this.serverOnlyService = serverOnlyService;
   }

	public Task<bool> BoolMethod(bool a, string b)
	{
		Assert.NotNull(this.serverOnlyService);
		return Task.FromResult(a);
	}

	public Task<List<string>> StringListMethod(List<string> a, int b)
	{
		Assert.NotNull(this.serverOnlyService); 
		return Task.FromResult(a);
	}
}

public class InterfaceFactoryTests : FactoryTestBase<IExecuteMethods>
{
	[Fact]
	public async Task InterfaceFactoryTests_BoolMethod()
	{
		var result = await base.factory.BoolMethod(true, "Keith");
	}

	[Fact]
	public async Task InterfaceFactoryTests_StringListMethodAsync()
	{
		var result = await base.factory.StringListMethod(new List<string> { "Keith", "Neatoo" }, 42);
		Assert.NotNull(result);
		Assert.Equal(2, result.Count);
		Assert.Contains("Keith", result);
		Assert.Contains("Neatoo", result);
	}
}
