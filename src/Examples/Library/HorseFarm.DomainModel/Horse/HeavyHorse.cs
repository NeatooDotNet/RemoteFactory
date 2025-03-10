
using Neatoo.RemoteFactory;

namespace HorseFarm.DomainModel.Horse;

public interface IHeavyHorse : IHorse
{
}

[Factory]
internal sealed class HeavyHorse : Horse<HeavyHorse>, IHeavyHorse
{
	public HeavyHorse() : base() { }

	[Create]
	public HeavyHorse(IHorseCriteria horseCriteria) : base(horseCriteria) 
	{
		if (!IHorse.IsHeavyHorse(horseCriteria.Breed!.Value))
		{
			throw new Exception($"Incorrect Breed: {horseCriteria.Breed.ToString()}");
		}
	}

}
