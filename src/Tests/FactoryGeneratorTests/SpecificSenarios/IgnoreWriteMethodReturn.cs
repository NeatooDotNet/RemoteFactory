using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.SpecificSenarios;

public class IgnoreWriteMethodReturn : FactoryTestBase<IIgnoreMethodReturnObjFactory>
{
	// The Factory Methods can return whatever they want
	// If it's not bool, it will be ignored
	// Useful for unit testing purposes

	[Factory]
	public partial class IgnoreMethodReturnObj : IFactorySaveMeta
	{
		[Create]
		public IgnoreMethodReturnObj()
		{

		}

		public bool InsertCalled { get; private set; }
		public bool IsDeleted => false;
		public bool IsNew => true;

		[Insert]
		public int Insert()
		{
			this.InsertCalled = true;
			return 1; // Return allowed, but ignored
		}

		[Update]
		public string? Update()
		{
			return string.Empty;
		}
	}

	[Fact]
	public void IgnoreMethodReturn_Save()
	{
		var obj = this.factory.Create();
		Assert.False(obj.InsertCalled);
		obj = this.factory.Save(obj);
		Assert.True(obj.InsertCalled);
	}
}
