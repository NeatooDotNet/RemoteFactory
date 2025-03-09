using HorseFarm.Lib.Horse;
using Neatoo.RemoteFactory;

namespace HorseFarm.Lib.Cart;


public interface IRacingChariot : ICart
{

}

[Factory]
internal class RacingChariot : Cart<RacingChariot, ILightHorse>, IRacingChariot
{
    protected override CartType CartType => CartType.RacingChariot;
}
