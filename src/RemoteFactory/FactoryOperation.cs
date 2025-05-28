
#if NETSTANDARD
namespace Neatoo.RemoteFactory.FactoryGenerator;
#else
namespace Neatoo.RemoteFactory;
#endif

public enum FactoryOperation
{
	None = 0,
	Execute = AuthorizeFactoryOperation.Read,
	Create = AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Read,
	Fetch = AuthorizeFactoryOperation.Fetch | AuthorizeFactoryOperation.Read,
	Insert = AuthorizeFactoryOperation.Insert | AuthorizeFactoryOperation.Write,
	Update = AuthorizeFactoryOperation.Update | AuthorizeFactoryOperation.Write,
	Delete = AuthorizeFactoryOperation.Delete | AuthorizeFactoryOperation.Write
}

