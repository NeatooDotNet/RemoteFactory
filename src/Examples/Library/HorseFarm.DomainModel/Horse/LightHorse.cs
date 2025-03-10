using Neatoo;
using Neatoo.RemoteFactory;

namespace HorseFarm.DomainModel.Horse;

public interface ILightHorse : IHorse
{

}

[Factory]
internal sealed class LightHorse : Horse<LightHorse>, ILightHorse
{

	public LightHorse()	{	}

	[Create]
	public LightHorse(IHorseCriteria criteria) : base(criteria) 
	{

		if (!IHorse.IsLightHorse(criteria.Breed!.Value))
		{
			throw new Exception($"Incorrect Breed: {criteria.Breed}");
		}
	}
}
