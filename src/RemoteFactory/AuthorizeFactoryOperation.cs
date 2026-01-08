#if NETSTANDARD
namespace Neatoo.RemoteFactory.FactoryGenerator;
#else
namespace Neatoo.RemoteFactory;
#endif

[Flags]
public enum AuthorizeFactoryOperation
{
	Create = 1,
	Fetch = 2,
	Insert = 4,
	Update = 8,
	Delete = 16,
	Read = 64,
	Write = 128,
	Execute = 256,
	Event = 512
}

