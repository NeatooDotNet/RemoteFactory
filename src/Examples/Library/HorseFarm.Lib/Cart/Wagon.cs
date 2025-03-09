using HorseFarm.Lib.Horse;
using Neatoo.RemoteFactory;

namespace HorseFarm.Lib.Cart;

public interface IWagon : ICart
{

}

[Factory]
internal class Wagon : Cart<Wagon, IHeavyHorse>, IWagon
{

    protected override CartType CartType => CartType.Wagon;
}
