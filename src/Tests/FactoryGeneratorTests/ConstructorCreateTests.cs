using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests;

public class ConstructorCreateTests
{


	[Factory]
	public class ConstructorCreateObject
	{

		[Create]
		public ConstructorCreateObject()
		{
			this.NullablePropertiesAreOk = "NullablePropertiesAreOk";
		}

		public string NullablePropertiesAreOk { get; set; }
	}

	private IServiceScope clientScope;

	public ConstructorCreateTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
	}

	[Fact]
	public void ConstructorCreateObjectTest_ConstructorCreateObject()
	{
		var factory = this.clientScope.GetRequiredService<IConstructorCreateObjectFactory>();
		var obj = factory.Create();
		Assert.NotNull(obj);
	}


	[Factory]
	public class ConstructorCreateObjectDep
	{

		[Create]
		public ConstructorCreateObjectDep([Service] IService service)
		{
			Assert.NotNull(service);
			this.NullablePropertiesAreOk = "NullablePropertiesAreOk";
		}

		public string NullablePropertiesAreOk { get; set; }
	}

	[Fact]
	public void ConstructorCreateObjectTest_ConstructorCreateObjectDep()
	{
		var factory = this.clientScope.GetRequiredService<IConstructorCreateObjectDepFactory>();
		var obj = factory.Create();
		Assert.NotNull(obj);
	}

	[Factory]
	public class ConstructorCreateObjectParamsDep
	{

		[Create]
		public ConstructorCreateObjectParamsDep(Guid param, [Service] IService service)
		{
			Assert.NotNull(service);
			this.NullablePropertiesAreOk = "NullablePropertiesAreOk";
			this.Param = param;
		}

		public string NullablePropertiesAreOk { get; set; }
		public Guid Param { get; }
	}

	[Fact]
	public void ConstructorCreateObjectTest_ConstructorCreateObjectParamsDep()
	{
		var factory = this.clientScope.GetRequiredService<IConstructorCreateObjectParamsDepFactory>();
		var obj = factory.Create(Guid.NewGuid());
		Assert.NotNull(obj);
	}
}
