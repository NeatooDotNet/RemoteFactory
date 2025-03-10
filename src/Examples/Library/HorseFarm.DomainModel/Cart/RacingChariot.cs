using HorseFarm.DomainModel.Horse;
using Neatoo.RemoteFactory;

namespace HorseFarm.DomainModel.Cart;


public interface IRacingChariot : ICart
{

}

[Factory]
internal sealed class RacingChariot : Cart<RacingChariot, ILightHorse>, IRacingChariot
{
    protected override CartType CartType => CartType.RacingChariot;
}
