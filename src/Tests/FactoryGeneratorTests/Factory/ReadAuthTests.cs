using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;


public class ReadAuthTests
{
	public class ReadAuthTask : ReadAuth
	{

		[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
		public Task<bool> CanAnyBoolTask()
		{
			this.CanAnyCalled++;
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
		public Task<bool> CanAnyBoolFalseTask(int? p)
		{
			this.CanAnyCalled++;
			if (p == 10)
			{
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
		public Task<string> CanAnyStringTask()
		{
			this.CanAnyCalled++;
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
		public Task<string> CanAnyStringFalseTask(int? p)
		{
			this.CanAnyCalled++;
			if (p == 20)
			{
				return Task.FromResult("Fail");
			}
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public Task<bool> CanReadBoolTask()
		{
			this.CanReadCalled++;
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public Task<bool> CanReadBoolFalseTask(int? p)
		{
			this.CanReadCalled++;
			if (p == 10)
			{
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public Task<string> CanReadStringTask()
		{
			this.CanReadCalled++;
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public Task<string> CanReadStringFalseTask(int? p)
		{
			this.CanReadCalled++;
			if (p == 20)
			{
				return Task.FromResult("Fail");
			}
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
		public Task<bool> CanCreateBoolTask()
		{
			this.CanCreateCalled++;
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
		public Task<bool> CanCreateBoolFalseTask(int? p)
		{
			this.CanCreateCalled++;
			if (p == 10)
			{
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
		public Task<string> CanCreateStringTask()
		{
			this.CanCreateCalled++;
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
		public Task<string> CanCreateStringFalseTask(int? p)
		{
			this.CanCreateCalled++;
			if (p == 20)
			{
				return Task.FromResult("Fail");
			}
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
		public Task<bool> CanFetchBoolTask()
		{
			this.CanFetchCalled++;
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
		public Task<bool> CanFetchBoolFalseTask(int? p)
		{
			this.CanFetchCalled++;
			if (p == 10)
			{
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
		public Task<string> CanFetchStringTask()
		{
			this.CanFetchCalled++;
			return Task.FromResult(string.Empty);
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
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

	public class ReadAuth
	{
		public int CanAnyCalled { get; set; }

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public bool CanAnyBool()
		{
			this.CanAnyCalled++;
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public bool CanAnyBoolFalse(int? p)
		{
			this.CanAnyCalled++;
			if (p == 10)
			{
				return false;
			}
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public string? CanAnyString()
		{
			this.CanAnyCalled++;
			return string.Empty;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public string? CanAnyStringFalse(int? p)
		{
			this.CanAnyCalled++;
			if (p == 20)
			{
				return "Fail";
			}
			return string.Empty;
		}

		public int CanReadCalled { get; set; }

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public bool CanReadBool()
		{
			this.CanReadCalled++;
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public bool CanReadBoolFalse(int? p)
		{
			this.CanReadCalled++;
			if (p == 10)
			{
				return false;
			}
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public string? CanReadString()
		{
			this.CanReadCalled++;
			return string.Empty;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
		public string? CanReadStringFalse(int? p)
		{
			this.CanReadCalled++;
			if (p == 20)
			{
				return "Fail";
			}
			return string.Empty;
		}

		public int CanCreateCalled { get; set; }

		[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
		public bool CanCreateBool()
		{
			this.CanCreateCalled++;
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
		public bool CanCreateBoolFalse(int? p)
		{
			this.CanCreateCalled++;
			if (p == 10)
			{
				return false;
			}
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
		public string? CanCreateString()
		{
			this.CanCreateCalled++;
			return string.Empty;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
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

		[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
		public bool CanFetchBool()
		{
			this.CanFetchCalled++;
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
		public bool CanFetchBoolFalse(int? p)
		{
			this.CanFetchCalled++;
			if (p == 10)
			{
				return false;
			}
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
		public string? CanFetchString()
		{
			this.CanFetchCalled++;
			return string.Empty;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
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
	[AuthorizeFactory<ReadAuth>]
	public class ReadAuthObject : ReadTests.ReadParamObject
	{

	}

	[Factory]
	[AuthorizeFactory<ReadAuthTask>]
	public class ReadAuthTaskObject : ReadTests.ReadParamObject
	{

	}

	private IServiceScope clientScope;

	public ReadAuthTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
	}

	[Fact]
	public async Task ReadAuthTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadAuthObjectFactory>();
		var authorized = this.clientScope.ServiceProvider.GetRequiredService<ReadAuth>();

		// Reflection Approved
		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch") || m.Name.StartsWith("Can") || m.Name.StartsWith("Try")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			var expect = 2;
			// Exclude CancellationToken when checking for meaningful parameters
			var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
			if (meaningfulParams.FirstOrDefault()?.ParameterType == typeof(int?))
			{
				result = method.Invoke(readFactory, [1, default(CancellationToken)]);
				expect = 4;
			}
			else
			{
				result = method.Invoke(readFactory, [default(CancellationToken)]);
			}

			if (result is Task<ReadAuthObject?> task)
			{
				Assert.Contains("Task", methodName);
				if (!methodName.Contains("False"))
				{
					Assert.NotNull(await task);
				}
				else
				{
					Assert.Null(await task);
				}
			}
			else if (result is Task<Authorized<ReadAuthObject>> authTask)
			{
				Assert.Contains("Task", methodName);

				if (!methodName.Contains("False"))
				{
					Assert.NotNull((await authTask).Result);
				}
				else
				{
					Assert.Null((await authTask).Result);
				}
			}
			else if (result is Authorized<ReadAuthObject> auth)
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
			else if (result is ReadAuthObject success)
			{
				Assert.DoesNotContain("False", methodName);
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

			Assert.Equal(expect, authorized.CanReadCalled);
			Assert.Equal(expect, authorized.CanAnyCalled);

			authorized.CanCreateCalled = 0;
			authorized.CanFetchCalled = 0;
			authorized.CanReadCalled = 0;
			authorized.CanAnyCalled = 0;
		}
	}


	[Fact]
	public async Task ReadAuthTaskTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadAuthTaskObjectFactory>();
		var authorized = this.clientScope.ServiceProvider.GetRequiredService<ReadAuthTask>();

		// Reflection Approved
		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch") || m.Name.StartsWith("Can") || m.Name.StartsWith("Try")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			// Exclude CancellationToken from meaningful parameter check
			var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();
			var expect = 2;
			if (meaningfulParams.FirstOrDefault()?.ParameterType == typeof(int?))
			{
				result = method.Invoke(readFactory, new object[] { 1, default(CancellationToken) });
				expect = 4;
			}
			else
			{
				result = method.Invoke(readFactory, new object[] { default(CancellationToken) });
			}

			if (result is Task<ReadAuthTaskObject?> task)
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
			else if (result is Task<Authorized<ReadAuthTaskObject>> authTask)
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
			else if (result is Authorized<ReadAuthTaskObject> auth)
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

			Assert.Equal(expect, authorized.CanReadCalled);
			Assert.Equal(expect, authorized.CanAnyCalled);

			authorized.CanCreateCalled = 0;
			authorized.CanFetchCalled = 0;
			authorized.CanReadCalled = 0;
			authorized.CanAnyCalled = 0;
		}
	}


	[Fact]
	public async Task ReadAuthBoolFailTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadAuthObjectFactory>();
		var authorized = this.clientScope.ServiceProvider.GetRequiredService<ReadAuth>();

		// Reflection Approved
		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch") || m.Name.StartsWith("Can") || m.Name.StartsWith("Try")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			// Exclude CancellationToken when checking for meaningful parameters
			var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
			if (meaningfulParams.FirstOrDefault()?.ParameterType == typeof(int?))
			{
				result = method.Invoke(readFactory, new object[] { 10, default(CancellationToken) }); // Fail
			}
			else
			{
				continue;
			}

			if (result is Task<ReadAuthObject?> task)
			{
				Assert.Null(await task);
				Assert.True(methodName.StartsWith("Create") || methodName.Contains("Fetch"));
			}
			else if (result is Task<Authorized<ReadAuthObject>> authTask)
			{
				var auth = await authTask;
				Assert.False(auth.HasAccess);
				Assert.Null(auth.Result);
				Assert.Null(auth.Message);
				Assert.StartsWith("Try", methodName);
			}
			else if (result is Authorized<ReadAuthObject> authDataMapper)
			{
				Assert.False(authDataMapper.HasAccess);
				Assert.Null(authDataMapper.Result);
				Assert.Null(authDataMapper.Message);
				Assert.True(methodName.StartsWith("Can") || methodName.StartsWith("Try"));
			}
			else if (result is Authorized auth_)
			{
				Assert.False(auth_.HasAccess);
				Assert.StartsWith("Can", methodName);
			}
			else if (result == null)
			{
				Assert.True(methodName.StartsWith("Create") || methodName.Contains("Fetch"));
			}

		}
	}

	[Fact]
	public async Task ReadAuthStringFailTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadAuthObjectFactory>();
		var authorized = this.clientScope.ServiceProvider.GetRequiredService<ReadAuth>();

		var methods = readFactory.GetType().GetMethods().Where(m => m.Name == "CanCreateVoid").ToList(); // m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch") || m.Name.StartsWith("Can") || m.Name.StartsWith("Try")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			// Exclude CancellationToken when checking for meaningful parameters
			var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
			if (meaningfulParams.FirstOrDefault()?.ParameterType == typeof(int?))
			{
				result = method.Invoke(readFactory, new object[] { 20, default(CancellationToken) }); // Fail
			}
			else
			{
				continue;
			}

			if (result is Task<ReadAuthObject?> task)
			{
				Assert.Null(await task);
				Assert.True(methodName.StartsWith("Create") || methodName.Contains("Fetch"));
			}
			else if (result is Task<Authorized<ReadAuthObject>> authTask)
			{
				var auth = await authTask;
				Assert.False(auth.HasAccess);
				Assert.Null(auth.Result);
				Assert.Equal("Fail", auth.Message);
				Assert.StartsWith("Try", methodName);
			}
			else if (result is Authorized<ReadAuthObject> authDataMapper)
			{
				Assert.False(authDataMapper.HasAccess);
				Assert.Null(authDataMapper.Result);
				Assert.Equal("Fail", authDataMapper.Message);
				Assert.True(methodName.StartsWith("Can") || methodName.StartsWith("Try"));
			}
			else if (result is Authorized auth_)
			{
				Assert.False(auth_.HasAccess);
				Assert.Equal("Fail", auth_.Message);
				Assert.StartsWith("Can", methodName);
			}
			else if (result == null)
			{
				Assert.True(methodName.StartsWith("Create") || methodName.Contains("Fetch"));
			}

		}
	}
}

