using HorseFarm.DomainModel.Horse;
using Neatoo.RemoteFactory;

namespace HorseFarm.DomainModel.Cart;

public interface IWagon : ICart, ICustomBase
{

}

[Factory]
internal sealed class Wagon : Cart<Wagon, IHeavyHorse>, IWagon
{

    protected override CartType CartType => CartType.Wagon;
}
