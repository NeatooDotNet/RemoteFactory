using HorseFarm.Ef;
using HorseFarm.DomainModel.Horse;
using Neatoo.RemoteFactory;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace HorseFarm.DomainModel.Cart;

public interface ICartCollection : ICollection<ICart>, INotifyCollectionChanged, IFactorySaveMeta
{
	internal void RemoveHorse(IHorse horse);
}

[Factory]
internal sealed class CartCollection : ObservableCollection<ICart>, ICartCollection
{

	[Create]
	public CartCollection() { }

	public bool IsDeleted => false;
	public bool IsNew => false;

	public void RemoveHorse(IHorse horse)
	{
		foreach (var c in this)
		{
			c.RemoveHorse(horse);
		}
	}

#if !CLIENT

	[Fetch]
	public void Fetch(ICollection<CartEntity> carts, [Service] RacingChariotFactory racingChariotFactory, [Service] WagonFactory wagonFactory)
	{
		foreach (var cart in carts)
		{
			if (cart.CartType == (int)CartType.RacingChariot)
			{
				this.Add(racingChariotFactory.Fetch(cart));
			}
			else if (cart.CartType == (int)CartType.Wagon)
			{
				this.Add(wagonFactory.Fetch(cart));
			}
		}
	}

	[Update]
	public void Update(HorseFarmEntity horseBarn, [Service] RacingChariotFactory racingChariotFactory, [Service] WagonFactory wagonFactory)
	{
		void SaveCart(ICart cart)
		{
			if (cart is RacingChariot racingChariot)
			{
				racingChariotFactory.Save(racingChariot, horseBarn);
			}
			else if (cart is Wagon wagon)
			{
				wagonFactory.Save(wagon, horseBarn);
			}
		}

		foreach (var cart in this)
		{
			SaveCart(cart);
		}
	}

#endif
}
