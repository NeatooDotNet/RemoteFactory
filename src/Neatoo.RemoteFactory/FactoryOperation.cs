using System;
using System.Collections.Generic;
using System.Text;

namespace Neatoo.RemoteFactory;

public enum FactoryOperation
{
	Execute = AuthorizeOperation.Read,
	Create = AuthorizeOperation.Create | AuthorizeOperation.Read,
	Fetch = AuthorizeOperation.Fetch | AuthorizeOperation.Read,
	Insert = AuthorizeOperation.Insert | AuthorizeOperation.Write,
	Update = AuthorizeOperation.Update | AuthorizeOperation.Write,
	Delete = AuthorizeOperation.Delete | AuthorizeOperation.Write
}
