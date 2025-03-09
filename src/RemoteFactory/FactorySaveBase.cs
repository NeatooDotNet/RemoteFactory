using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory;


public abstract class FactorySaveBase<T> : FactoryBase, IFactorySave<T>
	 where T : IFactorySaveMeta
{

	Task<IFactorySaveMeta?> IFactorySave<T>.Save(T target)
	{
		throw new NotImplementedException("Save not implemented");
	}
}
