using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.SpecificSenarios;

public class HasBaseClassFactoryAttributeTestsBase
{
	[Factory]
	public class BaseClass
	{
		public int BaseProperty { get; set; }
	}

	[Factory]
	public partial class DerivedClass : BaseClass
	{
		public int DerivedProperty { get; set; }
	}
}

public class HasBaseClassFactoryAttributeTests
{

	[Fact]
	public void DerivedClass_ShouldHaveFactoryGeneratedCode()
	{
		// Just ensure it compiles
		DerivedClassFactory? factory = null;
		Assert.Null(factory);
	}
}
