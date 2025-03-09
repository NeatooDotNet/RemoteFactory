
using Neatoo.RemoteFactory;

namespace HorseFarm.Lib.Horse;

public interface IHeavyHorse : IHorse
{
}

[Factory]
internal class HeavyHorse : Horse<HeavyHorse>, IHeavyHorse
{
 
    [Create]
    public override void Create(IHorseCriteria horseCriteria)
    {
        if (!IHorse.IsHeavyHorse(horseCriteria.Breed))
        {
            throw new Exception($"Incorrect Breed: {horseCriteria.Breed.ToString()}");
        }

        base.Create(horseCriteria);
    }
}
