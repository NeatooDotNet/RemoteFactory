using Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper;
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.SpecificSenarios;
public class BugNoCanCreateFetch : FactoryTestBase<IBugNoCanCreateFetchObjFactory>
{

   public class BugNoCanCreateFetchAuth
	{
		[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
		public bool CanAccess()
		{
			return true;
		}
	}

	[Factory]
	[AuthorizeFactory<BugNoCanCreateFetchAuth>()]
	public class BugNoCanCreateFetchObj : IFactorySaveMeta
	{
	  public bool IsDeleted => throw new NotImplementedException();

	  public bool IsNew => throw new NotImplementedException();

	  [Create]
		public void Create()
		{

		}

		[Insert]
		public void Insert()
		{
		}
	}


	[Fact]
	public void BugNoCanCreateFetchTest()
	{
		// In an effort to not have CanInsert was missing CanCreate
		var result = this.factory.CanCreate();
		Assert.True(result, "CanCreate should be true");
	}

}
