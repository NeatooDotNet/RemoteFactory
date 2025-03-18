using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper;

public partial interface IPersonObject
{
	public string FirstName { get; }
}




[Factory]
internal partial class PersonObject : EditBase<PersonObject>, IPersonObject
{
	public PersonObject() : base()
	{
		this.FirstName = "John";
		this.LastName = "Doe";
	}

	public string FirstName { get; set; }
	public string LastName { get; set; }

}

public abstract class EditBase<T>
{
	protected EditBase()
	{
	}
}