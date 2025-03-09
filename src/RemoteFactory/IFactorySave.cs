using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory;

 public interface IFactorySave<T>
	where T : IFactorySaveMeta
 {
	Task<IFactorySaveMeta?> Save(T entity);

}
