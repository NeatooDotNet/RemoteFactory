using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.SpecificSenarios;

/// <summary>
/// When there is no delete method, the Save doesn't need to be nullable
/// </summary>
[Factory]
public partial class SaveNotNullableNoDeleteObj : IFactorySaveMeta
{
	[Create]
	public SaveNotNullableNoDeleteObj()
	{

	}

	public bool InsertCalled { get; private set; }
	public bool IsDeleted => false;
	public bool IsNew => true;

	[Insert]
	public void Insert()
	{
		this.InsertCalled = true;
	}

	[Update]
	public void Update()
	{
	}
}

public class SaveNotNullableNoDeleteTests : FactoryTestBase<ISaveNotNullableNoDeleteObjFactory>
{
	[Fact]
	public void SaveNotNullableNoDelete_Insert()
	{
		var obj = this.factory.Create();
		Assert.False(obj.InsertCalled);
		obj = this.factory.Save(obj);
		Assert.True(obj.InsertCalled);
	}
}
