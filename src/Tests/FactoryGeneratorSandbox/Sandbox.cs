using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace FactoryGeneratorSandbox;


public class Sandbox
{
   [Fact]
   public void Test1()
   {
		// The source code to test
		var source = @"
using Neatoo;
using Neatoo.RemoteFactory;

namespace NeatooLibrary {


internal class AuthorizeObj {

[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]	
public bool ReadWrite() {

}

[AuthorizeFactory(AuthorizeFactoryOperation.Create)]	
public bool Create(int a) {

}

[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]	
public bool Fetch(string b) {

}

[AuthorizeFactory(AuthorizeFactoryOperation.Insert)]	
public bool Insert(string b) {

}

[AuthorizeFactory(AuthorizeFactoryOperation.Update)]	
public bool Update(string b) {

}

}
";

		var source2 = @"

using Neatoo.RemoteFactory;

namespace NeatooLibrary {


[Factory]
[AuthorizeFactory<AuthorizeObj>]
internal class Obj {


[Create]
public void Create(int a, string b) {	

}

[Insert]
public void Insert(){
}

[Update]
public void Update(){

}

}
";

		// Pass the source code to our helper and snapshot test the output
		TestHelper.Verify(source, source2);
	}

	[Fact]
	public void StaticInheritance()
	{
		// The source code to test
		var source = @"
using Neatoo;
using Neatoo.RemoteFactory;

namespace NeatooLibrary {


internal class ObjAuth : Obj {

	[Create]
	public static ObjAuth CreateAuth() {	
		return new ObjAuth();
	}

}
";

		var source2 = @"

using Neatoo.RemoteFactory;

namespace NeatooLibrary {


[Factory]
internal class Obj {


[Create]
public static Obj Create() {	
	return new Obj();
}

}
";

		// Pass the source code to our helper and snapshot test the output
		TestHelper.Verify(source, source2);
	}



	[Fact]
	public void SaveWDelete()
	{
		// The source code to test
		var source = @"
using Neatoo;
using Neatoo.RemoteFactory;


//[assembly: FactoryHintNameLength(50)]

namespace NeatooLibrary;

[Factory]
internal class Obj {

[Insert]
[Remote]
public void Insert(){}

[Update]
[Remote]
public void Update(){}

[Delete]
[Remote]
public void Delete(){}


}
";


		// Pass the source code to our helper and snapshot test the output
		TestHelper.Verify(source);
	}

	[Fact]
	public void Test()
	{
		// The source code to test
		var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace FactoryGeneratorSandbox;

   public class BugNoCanCreateFetchAuth
	{
		[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
		public static bool CanAccess()
		{
			return true;
		}

		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
		public static bool CanWrite()
		{
			return true;
		}

	}

	[Factory]
	[AuthorizeFactory<BugNoCanCreateFetchAuth>()]
	public class BugNoCanCreateFetchObj
	{
		[Create]
		public void Create()
		{

		}

		[Insert]
		public void Insert()
		{
		}
	}

";

		TestHelper.Verify(source);
	}

	private sealed class AuthedClass
	{

		[AspAuthorize("TestPolicy", Roles = "Keith")]
		public static void AuthedMethod()
		{
		}
	}
	[Fact]
	public void AspAuthorizeFactory()
	{
		// The source code to test
		var source = @"
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace FactoryGeneratorSandbox;


	[Factory]
	public class AspAuthorizedObj
	{
		[Create]
		[AspAuthorize(""TestPolicy"", Roles = ""Keith"")]
		public void Create()
		{

		}
	}

";

		TestHelper.Verify(source);
	}


	[Fact]
	public void InterfaceFactory()
	{
		// The source code to test
		var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace FactoryGeneratorSandbox;

	public interface IObjAuth
	{
		[AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
		bool HasAccess();
	}

	[Factory]
	[AuthorizeFactory<IObjAuth>()]
	public interface IObj
	{

		[AspAuthorize(""TestPolicy"", Roles = ""Keith"")]
		Task<bool> MethodInt(int a, string b);

	}

";

		TestHelper.Verify(source);
	}

	[Fact]
	public void BaseClass()
	{
		// The source code to test
		var source = @"
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace FactoryGeneratorSandbox;

	public class ClientObj
	{
		[Create]
		public void Create()
		{
		}
	}


  [Factory<ClientObj>]
	public class ServerObj : ClientObj
	{
		public bool CreateCalled { get; set; } = false;

		[Create]
		public Task<bool?> Create(int? p)
		{
			Assert.Null(p);
			return true;
		}

	}


";

		TestHelper.Verify(source);
	}
}