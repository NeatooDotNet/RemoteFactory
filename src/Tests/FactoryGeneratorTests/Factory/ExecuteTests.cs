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
		public delegate Task<string> RunOnServer(string message);

		[Execute<RunOnServer>]
		public static Task<string> DoExecute(string message)
		{
			Assert.Equal("Hello", message);
			return Task.FromResult("hello");
		}
	}


	[Fact]
	public async Task ExecuteTest_DoExecute()
	{
		var del = this.scope.ServiceProvider.GetRequiredService<ExecuteStatic.RunOnServer>();
		var result = await del("Hello");
		Assert.Equal("hello", result);
	}

	[Factory]
	public static class ExecuteNullableStatic
	{
		public delegate Task<string?> RunOnServer(string message);

		[Execute<RunOnServer>]
		public static Task<string?> DoExecute(string message)
		{
			Assert.Equal("Hello", message);
			return Task.FromResult(default(string));
		}
	}


	[Fact]
	public async Task ExecuteTest_Nullable()
	{
		var del = this.scope.ServiceProvider.GetRequiredService<ExecuteNullableStatic.RunOnServer>();
		var result = await del("Hello");
		Assert.Null(result);
	}
}

