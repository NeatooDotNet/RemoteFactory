using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests;

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

	private IServiceScope clientScope;
	private INullableParameterFactory factory;

	public NullableParameterTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
		this.factory = this.clientScope.ServiceProvider.GetRequiredService<INullableParameterFactory>();
	}

	[Fact]
	public void NullableParameterTest()
	{
		var obj = this.factory.Create(null);
		Assert.True(obj.CreateCalled);
	}

	[Fact]
	public async Task NullableParameterTest_CreateRemote()
	{
		var obj = await this.factory.CreateRemote(null);
		Assert.True(obj.CreateCalled);
	}
}
