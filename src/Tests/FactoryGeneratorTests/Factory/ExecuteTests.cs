using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

[Factory]
public static partial class ExecuteStatic
{
	[Execute]
	private static Task<string> _RunOnServer(string message)
	{
		Assert.Equal("Hello", message);
		return Task.FromResult("hello");
	}
}

[Factory]
public static partial class ExecuteNullableStatic
{
	[Execute]
	private static Task<string?> _RunOnServer(string message)
	{
		Assert.Equal("Hello", message);
		return Task.FromResult(default(string));
	}
}

public class ExecuteTests
{
   private readonly IServiceScope scope;

   public ExecuteTests()
	{
		this.scope = ClientServerContainers.Scopes().client;
	}



	[Fact]
	public async Task ExecuteTest_DoExecute()
	{
		var del = this.scope.ServiceProvider.GetRequiredService<ExecuteStatic.RunOnServer>();
		var result = await del("Hello");
		Assert.Equal("hello", result);
	}



	[Fact]
	public async Task ExecuteTest_Nullable()
	{
		var del = this.scope.ServiceProvider.GetRequiredService<ExecuteNullableStatic.RunOnServer>();
		var result = await del("Hello");
		Assert.Null(result);
	}
}

