using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

 public class ExecuteTests
{
   private readonly IServiceScope scope;

   public ExecuteTests()
	{
		this.scope = ClientServerContainers.Scopes().client;
	}

	[Factory]
	public static class ExecuteStatic
	{
		public delegate Task<int> RunOnServer(string message);

		[Execute<RunOnServer>]
		public static Task<int> DoExecute(string message)
		{
			Assert.Equal("Hello", message);
			return Task.FromResult(1);
		}
	}

	[Fact]
	public async Task ExecuteTest_DoExecute()
	{
		var del = this.scope.ServiceProvider.GetRequiredService<ExecuteStatic.RunOnServer>();
		var result = await del("Hello");
		Assert.Equal(1, result);
	}
}

