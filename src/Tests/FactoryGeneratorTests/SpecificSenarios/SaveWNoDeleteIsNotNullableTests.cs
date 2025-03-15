using Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.SpecificSenarios;

 public class SaveWNoDeleteIsNotNullableTests : FactoryTestBase<ISaveWNoDeleteIsNotNullableFactory>
{

	/// Save classes are Nullable for instances where there is a Delete of an IsNew item because it should return null
	/// But, if there is no Delete then don't have the Save be nullable

	[Factory]
	public class SaveWNoDeleteIsNotNullable : IFactorySaveMeta
	{
		[Create]
		public SaveWNoDeleteIsNotNullable()
		{
		}

		public bool IsDeleted => false;

		public bool IsNew => true;

	  [Insert]
		public void Insert() { }

		[Update]
		public void Update() { }

	}

	[Fact]
   [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0007:Use implicit type", Justification = "<Pending>")]
   public void SaveWNoDeleteIsNotNullable_NoNullableError()
	{
		SaveWNoDeleteIsNotNullable save = this.factory.Create();
		save = this.factory.Save(save);
	}

}
