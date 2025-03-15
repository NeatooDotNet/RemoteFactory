using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory;


public abstract class FactorySaveBase<T> : FactoryBase<T>, IFactorySave<T>
	 where T : IFactorySaveMeta
{
   protected FactorySaveBase(IFactoryCore<T> factoryCore) : base(factoryCore)
   {
   }

   Task<IFactorySaveMeta?> IFactorySave<T>.Save(T target)
	{
		throw new NotImplementedException("Save not implemented");
	}
}
