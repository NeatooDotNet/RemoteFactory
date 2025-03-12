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

[Authorize(AuthorizeOperation.Read | AuthorizeOperation.Write)]	
public bool ReadWrite() {

}

[Authorize(AuthorizeOperation.Create)]	
public bool Create(int a) {

}

[Authorize(AuthorizeOperation.Fetch)]	
public bool Fetch(string b) {

}

[Authorize(AuthorizeOperation.Insert)]	
public bool Insert(string b) {

}

[Authorize(AuthorizeOperation.Update)]	
public bool Update(string b) {

}

}
";

		var source2 = @"

using Neatoo.RemoteFactory;

namespace NeatooLibrary {


[Factory]
[Authorize<AuthorizeObj>]
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

namespace NeatooLibrary {


[Factory]
internal class Obj {

	[Insert]
public Task Insert(){}
[Update]
	public Task Update(){}
	[Delete]
	public Task Delete(){}

}
";


		// Pass the source code to our helper and snapshot test the output
		TestHelper.Verify(source);
	}

}