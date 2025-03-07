using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory;

public interface IFactoryEditBase<in T>
{
	Task<ISaveMetaProperties?> Save(T target);
}

public abstract class FactoryEditBase<T> : FactoryBase, IFactoryEditBase<T>
	 where T : ISaveMetaProperties
{

	Task<ISaveMetaProperties?> IFactoryEditBase<T>.Save(T target)
	{
		throw new NotImplementedException("Save not implemented");
	}
}
