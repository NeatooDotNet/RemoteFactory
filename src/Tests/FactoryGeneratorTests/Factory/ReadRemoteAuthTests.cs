using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;


public class ReadRemoteAuthTests
{

	public class ReadRemoteAuthTask : ReadRemoteAuth
	{
		[Remote]
		[Authorize(AuthorizeOperation.Read)]
		public Task<bool> CanReadRemoteBoolTask()
		{
			this.CanReadRemoteCalled++;
			return Task.FromResult(true);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Read)]
		public Task<bool> CanReadRemoteBoolFalseTask(int? p)
		{
			this.CanReadRemoteCalled++;
			if (p == 10)
			{
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Read)]
		public Task<string> CanReadRemoteStringTask()
		{
			this.CanReadRemoteCalled++;
			return Task.FromResult(string.Empty);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Read)]
		public Task<string> CanReadRemoteStringFalseTask(int? p)
		{
			this.CanReadRemoteCalled++;
			if (p == 20)
			{
				return Task.FromResult("Fail");
			}
			return Task.FromResult(string.Empty);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Create)]
		public Task<bool> CanCreateBoolTask()
		{
			this.CanCreateCalled++;
			return Task.FromResult(true);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Create)]
		public Task<bool> CanCreateBoolFalseTask(int? p)
		{
			this.CanCreateCalled++;
			if (p == 10)
			{
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Create)]
		public Task<string> CanCreateStringTask()
		{
			this.CanCreateCalled++;
			return Task.FromResult(string.Empty);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Create)]
		public Task<string> CanCreateStringFalseTask(int? p)
		{
			this.CanCreateCalled++;
			if (p == 20)
			{
				return Task.FromResult("Fail");
			}
			return Task.FromResult(string.Empty);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Fetch)]
		public Task<bool> CanFetchBoolTask()
		{
			this.CanFetchCalled++;
			return Task.FromResult(true);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Fetch)]
		public Task<bool> CanFetchBoolFalseTask(int? p)
		{
			this.CanFetchCalled++;
			if (p == 10)
			{
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Fetch)]
		public Task<string> CanFetchStringTask()
		{
			this.CanFetchCalled++;
			return Task.FromResult(string.Empty);
		}

		[Remote]
		[Authorize(AuthorizeOperation.Fetch)]
		public Task<string> CanFetchStringFalseTask(int? p)
		{
			this.CanFetchCalled++;
			if (p == 20)
			{
				return Task.FromResult("Fail");
			}
			return Task.FromResult(string.Empty);
		}
	}

	public class ReadRemoteAuth
	{
		public int CanReadRemoteCalled { get; set; }

		[Remote]
		[Authorize(AuthorizeOperation.Read)]
		public bool CanReadRemoteBool()
		{
			this.CanReadRemoteCalled++;
			return true;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Read)]
		public bool CanReadRemoteBoolFalse(int? p)
		{
			this.CanReadRemoteCalled++;
			if (p == 10)
			{
				return false;
			}
			return true;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Read)]
		public string? CanReadRemoteString()
		{
			this.CanReadRemoteCalled++;
			return string.Empty;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Read)]
		public string? CanReadRemoteStringFalse(int? p)
		{
			this.CanReadRemoteCalled++;
			if (p == 20)
			{
				return "Fail";
			}
			return string.Empty;
		}

		public int CanCreateCalled { get; set; }

		[Remote]
		[Authorize(AuthorizeOperation.Create)]
		public bool CanCreateBool()
		{
			this.CanCreateCalled++;
			return true;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Create)]
		public bool CanCreateBoolFalse(int? p)
		{
			this.CanCreateCalled++;
			if (p == 10)
			{
				return false;
			}
			return true;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Create)]
		public string? CanCreateString()
		{
			this.CanCreateCalled++;
			return string.Empty;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Create)]
		public string? CanCreateStringFalse(int? p)
		{
			this.CanCreateCalled++;
			if (p == 20)
			{
				return "Fail";
			}
			return string.Empty;
		}

		public int CanFetchCalled { get; set; }

		[Remote]
		[Authorize(AuthorizeOperation.Fetch)]
		public bool CanFetchBool()
		{
			this.CanFetchCalled++;
			return true;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Fetch)]
		public bool CanFetchBoolFalse(int? p)
		{
			this.CanFetchCalled++;
			if (p == 10)
			{
				return false;
			}
			return true;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Fetch)]
		public string? CanFetchString()
		{
			this.CanFetchCalled++;
			return string.Empty;
		}

		[Remote]
		[Authorize(AuthorizeOperation.Fetch)]
		public string? CanFetchStringFalse(int? p)
		{
			this.CanFetchCalled++;
			if (p == 20)
			{
				return "Fail";
			}
			return string.Empty;
		}
	}

	[Factory]
	[Authorize<ReadRemoteAuth>]
	public class ReadRemoteAuthObj : ReadTests.ReadParamObject
	{

	}

	[Factory]
	[Authorize<ReadRemoteAuthTask>]
	public class ReadRemoteAuthTaskObj : ReadTests.ReadParamObject
	{

	}

	private IServiceScope clientScope;
	private IServiceScope serverScope;

	public ReadRemoteAuthTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
		this.serverScope = scopes.server;
	}

	[Fact]
	public async Task ReadRemoteAuthTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadRemoteAuthObjFactory>();
		var authorized = this.serverScope.ServiceProvider.GetRequiredService<ReadRemoteAuth>();

		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch") || m.Name.StartsWith("Can") || m.Name.StartsWith("Try")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			var expect = 2;
			if (method.GetParameters().FirstOrDefault()?.ParameterType == typeof(int?))
			{
				result = method.Invoke(readFactory, new object[] { 1 });
				expect = 4;
			}
			else
			{
				result = method.Invoke(readFactory, null);
			}

			if (result is Task<ReadRemoteAuthObj?> task)
			{
				if (!methodName.Contains("False"))
				{
					Assert.NotNull(await task);
				}
				else
				{
					Assert.Null(await task);
				}
			}
			else if (result is Task<Authorized<ReadRemoteAuthObj>> authTask)
			{
				if (!methodName.Contains("False"))
				{
					Assert.NotNull((await authTask).Result);
				}
				else
				{
					Assert.Null((await authTask).Result);
				}
			}
			else if (result is Authorized<ReadRemoteAuthObj> auth)
			{
				Assert.True(auth.HasAccess);

				if (auth.Result == null)
				{
					Assert.True(methodName.StartsWith("Can") || methodName.Contains("False"));
				}
				else
				{
					Assert.StartsWith("Try", methodName);
				}
			}
			else if (result is Task<Authorized> canTask)
			{
				Assert.StartsWith("Can", methodName);
				Assert.True((await canTask).HasAccess);
			}
			else
			{
				Assert.Fail();
			}


			if (methodName.Contains("Create"))
			{
				Assert.Equal(expect, authorized.CanCreateCalled);
			}
			else
			{
				Assert.Equal(expect, authorized.CanFetchCalled);
			}

			Assert.Equal(expect, authorized.CanReadRemoteCalled);

			authorized.CanCreateCalled = 0;
			authorized.CanFetchCalled = 0;
			authorized.CanReadRemoteCalled = 0;
		}
	}


	[Fact]
	public async Task ReadRemoteAuthTaskTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadRemoteAuthTaskObjFactory>();
		var authorized = this.serverScope.ServiceProvider.GetRequiredService<ReadRemoteAuthTask>();

		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch") || m.Name.StartsWith("Can") || m.Name.StartsWith("Try")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			var expect = 2;
			if (method.GetParameters().FirstOrDefault()?.ParameterType == typeof(int?))
			{
				result = method.Invoke(readFactory, new object[] { 1 });
				expect = 4;
			}
			else
			{
				result = method.Invoke(readFactory, null);
			}

			if (result is Task<ReadRemoteAuthTaskObj?> task)
			{
				if (!methodName.Contains("False"))
				{
					Assert.NotNull(await task);
				}
				else
				{
					Assert.Null(await task);
				}
			}
			else if (result is Task<Authorized<ReadRemoteAuthTaskObj>> authTask)
			{
				if (!methodName.Contains("False"))
				{
					Assert.NotNull((await authTask).Result);
				}
				else
				{
					Assert.Null((await authTask).Result);
				}
			}
			else if (result is Authorized<ReadRemoteAuthTaskObj> auth)
			{
				Assert.True(auth.HasAccess);

				if (auth.Result == null)
				{
					Assert.True(methodName.StartsWith("Can") || methodName.Contains("False"));
				}
				else
				{
					Assert.StartsWith("Try", methodName);
				}
			}
			else if (result is Task<Authorized> canTask)
			{
				Assert.StartsWith("Can", methodName);
				Assert.True((await canTask).HasAccess);
			}
			else if (result is Authorized can)
			{
				Assert.StartsWith("Can", methodName);
				Assert.True(can.HasAccess);
			}
			else if (result is Task voidTask)
			{
				await voidTask;
			}
			else
			{
				Assert.Contains("False", methodName);
			}


			if (methodName.Contains("Create"))
			{
				Assert.Equal(expect, authorized.CanCreateCalled);
			}
			else
			{
				Assert.Equal(expect, authorized.CanFetchCalled);
			}

			Assert.Equal(expect, authorized.CanReadRemoteCalled);

			authorized.CanCreateCalled = 0;
			authorized.CanFetchCalled = 0;
			authorized.CanReadRemoteCalled = 0;
		}
	}

	[Fact]
	public async Task ReadRemoteAuthBoolFailTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadRemoteAuthObjFactory>();
		var authorized = this.serverScope.ServiceProvider.GetRequiredService<ReadRemoteAuth>();

		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch") || m.Name.StartsWith("Can") || m.Name.StartsWith("Try")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			if (method.GetParameters().FirstOrDefault()?.ParameterType == typeof(int?))
			{
				result = method.Invoke(readFactory, new object[] { 10 }); // Fail
			}
			else
			{
				continue;
			}

			if (result is Task<ReadRemoteAuthObj?> task)
			{
				Assert.Null(await task);
			}
			else if (result is Task<Authorized<ReadRemoteAuthObj>> authTask)
			{
				var auth = await authTask;
				Assert.False(auth.HasAccess);
				Assert.Null(auth.Result);
				Assert.Null(auth.Message);
			}
			else if (result is Authorized<ReadRemoteAuthObj> authObj)
			{
				Assert.False(authObj.HasAccess);
				Assert.Null(authObj.Result);
				Assert.Null(authObj.Message);
			}
			else if (result is Task<Authorized> authorizedTask)
			{
				var a = await authorizedTask;
				Assert.False(a.HasAccess);
				Assert.Null(a.Message);
			}
			else
			{
				Assert.Fail();
			}

		}
	}

	[Fact]
	public async Task ReadRemoteAuthStringFailTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadRemoteAuthObjFactory>();
		var authorized = this.serverScope.ServiceProvider.GetRequiredService<ReadRemoteAuth>();

		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch") || m.Name.StartsWith("Can") || m.Name.StartsWith("Try")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			if (method.GetParameters().FirstOrDefault()?.ParameterType == typeof(int?))
			{
				result = method.Invoke(readFactory, new object[] { 20 }); // Fail
			}
			else
			{
				continue;
			}

			if (result is Task<ReadRemoteAuthObj?> task)
			{
				Assert.Null(await task);
			}
			else if (result is Task<Authorized<ReadRemoteAuthObj>> authTask)
			{
				var auth = await authTask;
				Assert.False(auth.HasAccess);
				Assert.Null(auth.Result);
				Assert.Equal("Fail", auth.Message);
			}
			else if (result is Authorized<ReadRemoteAuthObj> authObj)
			{
				Assert.False(authObj.HasAccess);
				Assert.Null(authObj.Result);
				Assert.Equal("Fail", authObj.Message);
			}
			else if (result is Task<Authorized> authorizedTask)
			{
				var a = await authorizedTask;
				Assert.False(a.HasAccess);
				Assert.Equal("Fail", a.Message);
			}
			else
			{
				Assert.Fail();
			}

		}
	}
}

