using HorseFarm.Ef;
using HorseFarm.DomainModel.Cart;
using Neatoo.RemoteFactory;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace HorseFarm.DomainModel.Horse;

public interface IHorseCollection : ICollection<IHorse>, INotifyCollectionChanged, IFactorySaveMeta
{

	internal void RemoveHorse(IHorse horse);
}

[Factory]
internal sealed class HorseCollection : ObservableCollection<IHorse>, IHorseCollection
{
	public bool IsDeleted => false;

	public bool IsNew => false;

	[Create]
   public HorseCollection() : base()
	{
	}

	public void RemoveHorse(IHorse horse)
	{
		if (this.Contains(horse))
		{
			this.Remove(horse);
		}
	}


#if !CLIENT

	[Fetch]
	public void Fetch(ICollection<HorseEntity> horses,
											  [Service] ILightHorseFactory lightHorseFactory,
											  [Service] IHeavyHorseFactory heavyHorseFactory)
	{
		foreach (var horse in horses)
		{
			if (IHorse.IsLightHorse((Breed)horse.Breed))
			{
				var h = lightHorseFactory.Fetch(horse);
				this.Add(h);
			}
			else
			{
				var h = heavyHorseFactory.Fetch(horse);
				this.Add(h);
			}
		}
	}

	[Update]
	public void Update(CartEntity cart,
											  [Service] ILightHorseFactory lightHorseFactory,
											  [Service] IHeavyHorseFactory heavyHorseFactory)
	{
		foreach (var horse in this)
		{
			if (horse is ILightHorse h)
			{
				lightHorseFactory.Save(h, cart);
			}
			else if (horse is IHeavyHorse hh)
			{
				heavyHorseFactory.Save(hh, cart);
			}
		}
	}

	[Update]
	public void Update(PastureEntity pasture,
											  [Service] ILightHorseFactory lightHorseFactory,
											  [Service] IHeavyHorseFactory heavyHorseFactory)
	{
		void SaveHorse(IHorse horse)
		{
			if (horse is ILightHorse h)
			{
				lightHorseFactory.Save(h, pasture);
			}
			else if (horse is IHeavyHorse hh)
			{
				heavyHorseFactory.Save(hh, pasture);
			}
		}

		foreach (var horse in this)
		{
			SaveHorse(horse);
		}
	}

#endif
}
