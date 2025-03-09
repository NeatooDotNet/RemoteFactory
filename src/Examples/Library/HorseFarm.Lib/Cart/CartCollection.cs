using HorseFarm.Ef;
using HorseFarm.Lib.Horse;
using Neatoo.RemoteFactory;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace HorseFarm.Lib.Cart;

public interface ICartCollection : ICollection<ICart>, INotifyCollectionChanged
{
	internal void RemoveHorse(IHorse horse);
}

[Factory]
internal class CartCollection : ObservableCollection<ICart>, ICartCollection
{
	public CartCollection() : base() { }

	public void RemoveHorse(IHorse horse)
	{
		foreach (var c in this)
		{
			c.RemoveHorse(horse);
		}
	}

	[Create]
	public static void Create() { }

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
