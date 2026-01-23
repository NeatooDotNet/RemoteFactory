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

	// DEPRECATED: Reflection-based tests removed
	// Authorization tests are now in RemoteFactory.IntegrationTests.Combinations.AuthorizationTests
}

