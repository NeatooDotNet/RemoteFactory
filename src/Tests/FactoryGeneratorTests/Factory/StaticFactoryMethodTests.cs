using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

 public class StaticFactoryMethodTests
 {

	private IServiceScope clientScope;

	public StaticFactoryMethodTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
	}

	[Factory]
	public class StaticFactoryCreateObject
	{
		public StaticFactoryCreateObject()
		{
			this.NullablePropertiesAreOk = "NullablePropertiesAreOk";
		}
		public string NullablePropertiesAreOk { get; set; }
		public bool UsedStaticMethod { get; set; } = false;

		[Create]
		public static StaticFactoryCreateObject Create()
		{
		 var result = new StaticFactoryCreateObject
		 {
			UsedStaticMethod = true
		 };
		 return result;
		}
	}

	[Fact]
	public void StaticFactoryCreateObjectTest_StaticFactoryCreateObject()
	{
		var factory = this.clientScope.GetRequiredService<IStaticFactoryCreateObjectFactory>();
		var obj = factory.Create();
		Assert.NotNull(obj);
		Assert.True(obj.UsedStaticMethod);
	}

	[Factory]
	public class StaticFactoryAsyncFetchObject
	{
		public StaticFactoryAsyncFetchObject()
		{
			this.NullablePropertiesAreOk = "NullablePropertiesAreOk";
		}
		public string NullablePropertiesAreOk { get; set; }
		public bool UsedStaticMethod { get; set; } = false;

		[Fetch]
		public static Task<StaticFactoryAsyncFetchObject> Fetch()
		{
			var result = new StaticFactoryAsyncFetchObject
			{
				UsedStaticMethod = true
			};
			return Task.FromResult(result);
		}
	}

	[Fact]
   public async Task StaticFactoryCreateObjectTest_StaticFactoryAsyncFetchObject()
   {
		var factory = this.clientScope.GetRequiredService<IStaticFactoryAsyncFetchObjectFactory>();
		var obj = await factory.Fetch();
		Assert.NotNull(obj);
		Assert.True(obj.UsedStaticMethod);
	}

	[Factory]
	public class StaticFactoryAsyncFetchParamObject
	{
		public StaticFactoryAsyncFetchParamObject()
		{
			this.NullablePropertiesAreOk = "NullablePropertiesAreOk";
		}
		public string NullablePropertiesAreOk { get; set; }
		public bool UsedStaticMethod { get; set; } = false;

		[Fetch]
		public static Task<StaticFactoryAsyncFetchParamObject> Fetch(int? a, [Service] IService service)
		{
			var result = new StaticFactoryAsyncFetchParamObject
			{
				UsedStaticMethod = true
			};
			return Task.FromResult(result);
		}
	}

	[Fact]
	public async Task StaticFactoryCreateObjectTest_StaticFactoryAsyncFetchParamObject()
	{
		var factory = this.clientScope.GetRequiredService<IStaticFactoryAsyncFetchParamObjectFactory>();
		var obj = await factory.Fetch(1);
		Assert.NotNull(obj);
		Assert.True(obj.UsedStaticMethod);
	}

	public class AuthorizeStaticFactoryAsyncFetchParamAuthObject
	{
		[Authorize(AuthorizeOperation.Fetch)]
		public bool CanFetch(int? a)
		{
			if(a == 20)
			{
				return false;
			}
			return true;
		}
	}

	[Factory]
	[Authorize<AuthorizeStaticFactoryAsyncFetchParamAuthObject>]
	public class StaticFactoryAsyncFetchParamAuthObject
	{
		public StaticFactoryAsyncFetchParamAuthObject()
		{
			this.NullablePropertiesAreOk = "NullablePropertiesAreOk";
		}
		public string NullablePropertiesAreOk { get; set; }
		public bool UsedStaticMethod { get; set; } = false;

		[Fetch]
		public static Task<StaticFactoryAsyncFetchParamAuthObject> Fetch(int? a, [Service] IService service)
		{
			Assert.NotNull(service);

			var result = new StaticFactoryAsyncFetchParamAuthObject
			{
				UsedStaticMethod = true
			};
			return Task.FromResult(result);
		}
	}

	[Fact]
	public async Task StaticFactoryCreateObjectTest_StaticFactoryAsyncFetchParamAuthObject()
	{
		var factory = this.clientScope.GetRequiredService<IStaticFactoryAsyncFetchParamAuthObjectFactory>();
		var obj = await factory.Fetch(1);
		Assert.NotNull(obj);
		Assert.True(obj.UsedStaticMethod);
	}

	[Fact]
	public async Task StaticFactoryCreateObjectTest_StaticFactoryAsyncFetchParamAuthObject_CanFetch()
	{
		var factory = this.clientScope.GetRequiredService<IStaticFactoryAsyncFetchParamAuthObjectFactory>();
		var obj = await factory.Fetch(10);
		Assert.NotNull(obj);
		Assert.True(obj.UsedStaticMethod);
	}

	[Fact]
	public async Task StaticFactoryCreateObjectTest_StaticFactoryAsyncFetchParamAuthObject_CanFetch_Fail()
	{
		var factory = this.clientScope.GetRequiredService<IStaticFactoryAsyncFetchParamAuthObjectFactory>();
		var obj = await factory.Fetch(20);
		Assert.Null(obj);
	}


	[Factory]
	public class StaticFactoryAsyncFetchNullable
	{
		public StaticFactoryAsyncFetchNullable()
		{
			this.NullablePropertiesAreOk = "NullablePropertiesAreOk";
		}
		public string NullablePropertiesAreOk { get; set; }
		public bool UsedStaticMethod { get; set; } = false;

		[Fetch]
		public static Task<StaticFactoryAsyncFetchNullable?> Fetch(int? a, [Service] IService service)
		{
			Assert.NotNull(service);

			if (a == 20)
			{
				return Task.FromResult<StaticFactoryAsyncFetchNullable?>(null);
			}

			var result = new StaticFactoryAsyncFetchNullable
			{
				UsedStaticMethod = true
			};
			return Task.FromResult<StaticFactoryAsyncFetchNullable?>(result);
		}
	}

	[Fact]
	public async Task StaticFactoryCreateObjectTest_StaticFactoryAsyncFetchNullable_Fetch()
	{
		var factory = this.clientScope.GetRequiredService<IStaticFactoryAsyncFetchParamAuthObjectFactory>();
		var obj = await factory.Fetch(10);
		Assert.NotNull(obj);
		Assert.True(obj.UsedStaticMethod);
	}

	[Fact]
	public async Task StaticFactoryCreateObjectTest_StaticFactoryAsyncFetchNullable_Fetch_Null()
	{
		var factory = this.clientScope.GetRequiredService<IStaticFactoryAsyncFetchParamAuthObjectFactory>();
		var obj = await factory.Fetch(20);
		Assert.Null(obj);
	}
}
