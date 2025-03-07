
#if NETSTANDARD
namespace Neatoo.RemoteFactory.FactoryGenerator;
#else
namespace Neatoo.RemoteFactory;
#endif

public enum FactoryOperation
{
	None = 0,
	Execute = AuthorizeOperation.Read,
	Create = AuthorizeOperation.Create | AuthorizeOperation.Read,
	Fetch = AuthorizeOperation.Fetch | AuthorizeOperation.Read,
	Insert = AuthorizeOperation.Insert | AuthorizeOperation.Write,
	Update = AuthorizeOperation.Update | AuthorizeOperation.Write,
	Delete = AuthorizeOperation.Delete | AuthorizeOperation.Write
}

