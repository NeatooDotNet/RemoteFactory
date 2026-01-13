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

	[Fact]
	public async Task ReadFactoryTest()
	{
		var readFactory = this.factory;

		// Reflection Approved
		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.Contains("Create") || m.Name.Contains("Fetch")).ToList();

		foreach (var method in methods)
		{
			if (method.Name.Contains("False"))
			{
				// Null object created
				continue;
			}

			object? result;
			var methodName = method.Name;

			// Exclude CancellationToken from meaningful parameter count
			var meaningfulParams = method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToList();
			if (meaningfulParams.Any())
			{
				result = method.Invoke(readFactory, new object[] { 1, default(CancellationToken) });
			}
			else
			{
				result = method.Invoke(readFactory, new object[] { default(CancellationToken) });
			}

			FactoryOnStartCompleteObj? obj = null;

			if (result is Task<FactoryOnStartCompleteObj?> task)
			{
				obj = await task;
				Assert.True(obj!.StartAsyncCalled);
				Assert.True(obj!.CompleteAsyncCalled);
			}
			else if (result is FactoryOnStartCompleteObj r)
			{
				obj = r;
				Assert.False(obj!.StartAsyncCalled);
				Assert.False(obj!.CompleteAsyncCalled);
			}

			Assert.True(obj!.StartCalled);
			Assert.True(obj!.CompleteCalled);
		}
	}
}
