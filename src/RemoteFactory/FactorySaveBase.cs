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

   Task<IFactorySaveMeta?> IFactorySave<T>.Save(T target, CancellationToken cancellationToken)
	{
		throw new NotImplementedException("Save not implemented");
	}

   // CA1033: CanSave default returns Authorized(true) when no authorization is configured.
   // Generated factory classes override this with an explicit interface implementation
   // that delegates to their concrete CanSave method.
#pragma warning disable CA1033
   Task<Authorized> IFactorySave<T>.CanSave(CancellationToken cancellationToken)
	{
		return Task.FromResult(new Authorized(true));
	}
#pragma warning restore CA1033
}
