using Neatoo;
using Neatoo.RemoteFactory;

namespace HorseFarm.Lib.Horse;

public interface ILightHorse : IHorse
{

}

[Factory]
internal class LightHorse : Horse<LightHorse>, ILightHorse
{

    [Create]
    public override void Create(IHorseCriteria criteria)
    {
        if (!IHorse.IsLightHorse(criteria.Breed))
        {
            throw new Exception($"Incorrect Breed: {criteria.Breed}");
        }

        base.Create(criteria);
    }
}
