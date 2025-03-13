using HorseFarm.DomainModel.Cart;
using HorseFarm.DomainModel.Horse;
using Neatoo.RemoteFactory;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using HorseFarm.Ef;
using System.Text.Json.Serialization;

namespace HorseFarm.DomainModel;

public interface IHorseFarm : ICustomBase
{
	IPasture Pasture { get; }
	ICartCollection Carts { get; }
	IEnumerable<IHorse> Horses { get; }
	IHorse AddNewHorse(IHorseCriteria horseCriteria);
	IRacingChariot AddRacingChariot();
	IWagon AddWagon();
	void MoveHorseToCart(IHorse horse, ICart cart);
	void MoveHorseToPasture(IHorse horse);
}

[Factory]
internal sealed class HorseFarm : CustomBase, IHorseFarm
{
	private readonly ILightHorseFactory lightHorseFactory;
	private readonly IHeavyHorseFactory heavyHorseFactory;
	private readonly IRacingChariotFactory racingChariotFactory;
	private readonly IWagonFactory wagonFactory;

	[Create]
	public HorseFarm([Service] ILightHorseFactory lightHorseFactory,
								[Service] IHeavyHorseFactory heavyHorseFactory,
								[Service] IRacingChariotFactory racingChariotFactory,
								[Service] IWagonFactory wagonFactory,
								[Service] IPastureFactory pastureFactory,
								[Service] ICartCollectionFactory cartCollectionFactory)
	{
		this.lightHorseFactory = lightHorseFactory;
		this.heavyHorseFactory = heavyHorseFactory;
		this.racingChariotFactory = racingChariotFactory;
		this.wagonFactory = wagonFactory;
		this.Pasture = pastureFactory.Create();
		this.Carts = cartCollectionFactory.Create();
	}

	public IPasture Pasture { get; set { field = value; this.OnPropertyChanged(); } }
	public ICartCollection Carts { get; set { field = value; this.OnPropertyChanged(); } }

	[JsonIgnore]
	public IEnumerable<IHorse> Horses => this.Carts.SelectMany(c => c.Horses).Union(this.Pasture.HorseList);

	public IRacingChariot AddRacingChariot()
	{
		var newCart = this.racingChariotFactory.Create();
		this.Carts.Add(newCart);
		return newCart;
	}

	public IWagon AddWagon()
	{
		var newCart = this.wagonFactory.Create();
		this.Carts.Add(newCart);
		return newCart;
	}

	public IHorse AddNewHorse(IHorseCriteria horseCriteria)
	{
		IHorse horse;

		if (IHorse.IsLightHorse(horseCriteria.Breed!.Value))
		{
			horse = this.lightHorseFactory.Create(horseCriteria);
		}
		else if (IHorse.IsHeavyHorse(horseCriteria.Breed!.Value))
		{
			horse = this.heavyHorseFactory.Create(horseCriteria);
		}
		else
		{
			throw new Exception($"Cannot create child horse for breed {horseCriteria.Breed}");
		}

		this.Pasture.HorseList.Add(horse);
		return horse;
	}

	public void MoveHorseToCart(IHorse horse, ICart cart)
	{
		if (cart.CanAddHorse(horse))
		{
			this.Pasture.RemoveHorse(horse);
			this.Carts.RemoveHorse(horse);
			cart.AddHorse(horse);
		}
	}

	public void MoveHorseToPasture(IHorse horse)
	{
		this.Carts.RemoveHorse(horse);

		if (!this.Pasture.HorseList.Contains(horse))
		{
			this.Pasture.HorseList.Add(horse);
		}
	}

	[Remote]
	[Fetch]
	public async Task<bool> Fetch([Service] IHorseFarmContext horseBarnContext,
									[Service] IPastureFactory pastureFactory,
									[Service] ICartCollectionFactory cartFactory)
	{

		var horseBarn = await horseBarnContext.HorseFarms.FirstOrDefaultAsync();
		if (horseBarn != null)
		{
			this.Id = horseBarn.Id;
			this.Pasture = pastureFactory.Fetch(horseBarn.Pasture);
			this.Carts = cartFactory.Fetch(horseBarn.Carts);
		}
		else
		{
			return false;
		}

		this.IsNew = false;

		return true;
	}

	[Remote]
	[Insert]
	public async Task Insert([Service] IHorseFarmContext horseBarnContext,
									[Service] IPastureFactory pastureFactory,
									[Service] ICartCollectionFactory cartFactory)
	{
		var horseBarn = new HorseFarmEntity();

		horseBarn.PropertyChanged += this.HandleIdPropertyChanged;

		pastureFactory.Save(this.Pasture, horseBarn);
		cartFactory.Save(this.Carts, horseBarn);

		horseBarnContext.HorseFarms.Add(horseBarn);

		await horseBarnContext.SaveChangesAsync();
	}

	[Remote]
	[Update]
	public async Task Update([Service] IHorseFarmContext horseBarnContext,
									[Service] IPastureFactory pastureFactory,
									[Service] ICartCollectionFactory cartFactory)
	{
		var horseBarn = await horseBarnContext.HorseFarms.FirstAsync(hb => hb.Id == this.Id);
		pastureFactory.Save(this.Pasture, horseBarn);
		cartFactory.Save(this.Carts, horseBarn);

		await horseBarnContext.SaveChangesAsync();
	}
}
