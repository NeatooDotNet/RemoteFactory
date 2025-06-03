using Neatoo.RemoteFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.AspNetCore.TestClientLibrary;

public interface IClientLibraryObj : IFactorySaveMeta
{
	public string Name { get; }
}

[Factory]
internal class ClientLibraryObj : IClientLibraryObj
{
	public string Name { get; protected set; } = null!;

   public bool IsDeleted => false;

   public bool IsNew => false;

	[Create]
	public void LocalCreate(string name)
	{
		this.Name = name;
	}
}
