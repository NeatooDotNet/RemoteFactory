using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

public class NullableParameterTests
{

	[Factory]
	public class NullableParameter
	{
		public bool CreateCalled { get; set; } = false;

		[Create]
		public void Create(int? p)
		{
			Assert.Null(p);
			this.CreateCalled = true;
		}

		[Remote]
		[Create]
		public void CreateRemote(int? p)
		{
			Assert.Null(p);
			this.CreateCalled = true;
		}
	}

	private IServiceScope serverScope;
	private IServiceScope clientScope;

	public NullableParameterTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
		this.serverScope = scopes.server;
	}

	[Fact]
	public void NullableParameterTest()
	{
		var factory = this.serverScope.ServiceProvider.GetRequiredService<INullableParameterFactory>();
		var obj = factory.Create(null);
		Assert.True(obj.CreateCalled);
	}

	[Fact]
	public async Task NullableParameterTest_CreateRemote()
	{
		var factory = this.serverScope.ServiceProvider.GetRequiredService<INullableParameterFactory>();

		var obj = await factory.CreateRemote(null);
		Assert.True(obj.CreateCalled);
	}

	[Fact]
	public async Task NullableParameterTest_ClientFactory_CreateRemote()
	{
		var factory = this.clientScope.ServiceProvider.GetRequiredService<INullableParameterClientFactory>();

		var obj = await factory.CreateRemote(null);
		Assert.True(obj.CreateCalled);
	}
}
