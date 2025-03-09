using Neatoo.RemoteFactory;
using HorseFarm.Ef;
using Microsoft.EntityFrameworkCore;


namespace HorseFarm.Lib.Horse;


public interface IHorse
{
	internal int? Id { get; }
	string Name { get; set; }
	DateOnly BirthDate { get; set; }
	Breed Breed { get; }

	private static IEnumerable<Breed> LightHorses = [Horse.Breed.QuarterHorse, Horse.Breed.Thoroughbred, Horse.Breed.Mustang];

	private static IEnumerable<Breed> HeavyHorses = [Horse.Breed.Clydesdale, Horse.Breed.Shire];

	internal static bool IsLightHorse(Breed breed)
	{
		return LightHorses.Contains(breed);
	}

	internal static bool IsHeavyHorse(Breed breed)
	{
		return HeavyHorses.Contains(breed);
	}
}

internal class Horse<H> : CustomBase, IHorse
	 where H : Horse<H>
{
	public string Name { get; set { field = value; this.OnPropertyChanged(); } } = null!;

	public DateOnly BirthDate { get; set { field = value; this.OnPropertyChanged(); } }

	public Breed Breed { get; set { field = value; this.OnPropertyChanged(); } }

	public virtual void Create(IHorseCriteria horseCriteria)
	{
		this.Breed = horseCriteria.Breed;
		this.BirthDate = horseCriteria.BirthDay!.Value;
		this.Name = horseCriteria.Name!;
	}

#if !CLIENT

	[Fetch]
	internal void Fetch(HorseEntity horse)
	{
		this.Id = horse.Id;
		this.BirthDate = horse.BirthDate;
		this.Breed = (Breed) horse.Breed;
		this.Name = horse.Name;
	}

	HorseEntity? horse;

	[Insert]
	internal void Insert(PastureEntity pasture)
	{
	  this.horse = new HorseEntity
	  {
		 BirthDate = this.BirthDate,
		 Breed = (int) this.Breed,
		 Name = this.Name
	  };

	  pasture.Horses.Add(this.horse);

	  this.horse.PropertyChanged += this.HandleIdPropertyChanged;
	}

	[Insert]
	internal void Insert(CartEntity cart)
	{
	  var horse = new HorseEntity
	  {
		 BirthDate = this.BirthDate,
		 Breed = (int) this.Breed,
		 Name = this.Name
	  };

	  cart.Horses.Add(horse);

		horse.PropertyChanged += this.HandleIdPropertyChanged;
	}

	[Update]
	internal async Task Update(PastureEntity pasture, [Service] IHorseFarmContext horseBarnContext)
	{
		var horse = await horseBarnContext.Horses.SingleAsync(h => h.Id == this.Id);

		horse.Cart?.Horses.Remove(horse);

		horse.BirthDate = this.BirthDate;
		horse.Breed = (int)this.Breed;
		horse.Name = this.Name;
		pasture.Horses.Add(horse);
	}

	[Update]
	internal async Task Update(CartEntity cart, [Service] IHorseFarmContext horseBarnContext)
	{
		var horse = await horseBarnContext.Horses.SingleAsync(h => h.Id == this.Id);

		horse.Pasture?.Horses.Remove(horse);
		horse.BirthDate = this.BirthDate;
		horse.Breed = (int) this.Breed;
		horse.Name = this.Name;
		cart.Horses.Add(horse);
	}

	[Delete]
	internal void Delete(CartEntity cart)
	{
		var horse = cart.Horses.First(h => h.Id == this.Id);
		cart.Horses.Remove(horse);
	}

	[Delete]
	internal void Delete(PastureEntity pasture)
	{
		var horse = pasture.Horses.First(h => h.Id == this.Id);
		pasture.Horses.Remove(horse);
	}

#endif
}
