using HorseFarm.Ef;
using HorseFarm.Lib.Horse;
using Neatoo.RemoteFactory;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace HorseFarm.Lib.Cart;


public interface ICart : INotifyCollectionChanged
{
	internal int? Id { get; }
	string Name { get; set; }
	int NumberOfHorses { get; set; }
	IEnumerable<IHorse> Horses { get; }
	bool CanAddHorse(IHorse horse);
	internal void RemoveHorse(IHorse horse);
	internal void AddHorse(IHorse horse);
	internal IHorseCollection HorseList { get; }
}


internal class Cart<C, H> : CustomBase, ICart
	 where C : Cart<C, H>
	 where H : IHorse
{
	event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
	{
		add => this.HorseList.CollectionChanged += value;

		remove => this.HorseList.CollectionChanged -= value;
	}

	[Required]
	public string Name { get; set { field = value; this.OnPropertyChanged(); } } = null!;

	[Required]
	public int NumberOfHorses { get; set { field = value; this.OnPropertyChanged(); } }

	public IHorseCollection HorseList { get; set { field = value; this.OnPropertyChanged(); } } = null!;

	internal IEnumerable<IHorse> Horses => this.HorseList.Cast<IHorse>();

	IEnumerable<IHorse> ICart.Horses => this.HorseList.Cast<IHorse>();


	public void RemoveHorse(IHorse horse)
	{
		this.HorseList.RemoveHorse(horse);
	}

	public void AddHorse(IHorse horse)
	{
		if (horse is H h)
		{
			this.HorseList.Add(h);
		}
		else
		{
			throw new ArgumentException($"Horse {horse.GetType().FullName} is not of type {typeof(H).FullName}");
		}
	}

	public bool CanAddHorse(IHorse horse)
	{
		if (horse is H && this.HorseList.Count < this.NumberOfHorses)
		{
			return true;
		}
		return false;
	}

	protected virtual CartType CartType => throw new NotImplementedException();

	[Create]
	public void Create([Service] IHorseCollectionFactory horseFactory)
	{
		this.HorseList = horseFactory.Create();
		this.NumberOfHorses = 1;
	}

#if !CLIENT

	[Fetch]
	public void Fetch(CartEntity cart, [Service] IHorseCollectionFactory horseFactory)
	{
		this.Id = cart.Id;
		this.Name = cart.Name;
		this.NumberOfHorses = cart.NumberOfHorses;
		this.HorseList = horseFactory.Fetch(cart.Horses);
	}

	[Insert]
	internal void Insert(HorseFarmEntity horseBarn, [Service] IHorseCollectionFactory horseFactory)
	{
		var cart = new CartEntity
		{
			Name = this.Name,
			CartType = (int)this.CartType,
			NumberOfHorses = this.NumberOfHorses,
			HorseFarm = horseBarn
		};
		cart.PropertyChanged += this.HandleIdPropertyChanged;

		horseBarn.Carts.Add(cart);

		horseFactory.Save(this.HorseList, cart);
	}

	[Update]
	internal void Update(HorseFarmEntity horseBarn, [Service] IHorseCollectionFactory horseFactory)
	{
		var cart = horseBarn.Carts.First(c => c.Id == this.Id);

		Debug.Assert(cart.CartType == cart.CartType, "CartType mismatch");

		cart.Name = this.Name;
		cart.NumberOfHorses = this.NumberOfHorses;
		horseFactory.Save(this.HorseList, cart);
	}

#endif
}
