using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

[Factory]
public class FactoryCoreTarget
{
	[Create]
	public FactoryCoreTarget() { }
}

public class FactoryCoreForTarget : FactoryCore<FactoryCoreTarget>
{
	public bool Called { get; set; }
	public override FactoryCoreTarget DoFactoryMethodCall(FactoryOperation operation, Func<FactoryCoreTarget> factoryMethodCall)
	{
		this.Called = true;
		return base.DoFactoryMethodCall(operation, factoryMethodCall);
	}
}

 public class FactoryCoreTests : FactoryTestBase<IFactoryCoreTargetFactory>
 {

	[Fact]
	public void FactoryCore_Should_Call_DoFactoryMethodCall()
	{
		var factoryCoreTarget = this.factory.Create();
		var factoryCoreForTarget = (FactoryCoreForTarget) this.clientScope.GetRequiredService<IFactoryCore<FactoryCoreTarget>>();
		Assert.True(factoryCoreForTarget.Called);
	}
}
