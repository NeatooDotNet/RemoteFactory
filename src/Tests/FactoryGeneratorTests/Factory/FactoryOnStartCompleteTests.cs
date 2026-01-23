using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Neatoo.RemoteFactory.FactoryGeneratorTests.Factory.ReadTests;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

public class FactoryOnStartCompleteTests : FactoryTestBase<IFactoryOnStartCompleteObjFactory>
{
	[Factory]
	public class FactoryOnStartCompleteObj : ReadObject, IFactoryOnStart, IFactoryOnStartAsync, IFactoryOnComplete, IFactoryOnCompleteAsync
	{
		[Create]
		[Fetch]
		public FactoryOnStartCompleteObj()
		{
			this.StartCalled = true;
		}

		public bool StartCalled { get; set; }
		public bool CompleteCalled { get; set; }
		public bool StartAsyncCalled { get; set; }
		public bool CompleteAsyncCalled { get; set; }

		public void FactoryStart(FactoryOperation factoryOperation)
		{
			this.StartCalled = true;
		}

		public void FactoryComplete(FactoryOperation factoryOperation)
		{
			this.CompleteCalled = true;
		}
		public Task FactoryStartAsync(FactoryOperation factoryOperation)
		{
			this.StartAsyncCalled = true;
			return Task.CompletedTask;
		}

		public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
		{
			this.CompleteAsyncCalled = true;
			return Task.CompletedTask;
		}
	}

	// DEPRECATED: Reflection-based tests removed
	// Factory lifecycle tests are now in RemoteFactory.IntegrationTests
}
