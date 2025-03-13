using HorseFarm.DomainModel.Horse;
using HorseFarm.Ef;
using Microsoft.EntityFrameworkCore;
using Rocks;
using System.Security.Cryptography.X509Certificates;

[assembly: Rock(typeof(IHorseFarmContext), BuildType.Create)]
[assembly: Rock(typeof(DbSet<>), BuildType.Make)]

namespace HorseFarm.DomainModel.UnitTests;

public class HorseUnitTests
{

	private sealed class HorseStandIn : Horse<HorseStandIn>
	{
		public HorseStandIn() : base() { }
		public HorseStandIn(IHorseCriteria horseCriteria) : base(horseCriteria) { }
	}

	[Fact]
	public void Horse_Construct()
	{
		// Arrange
		var horseCriteria = new HorseCriteria()
		{
			Name = "Triggered",
			Breed = Horse.Breed.QuarterHorse,
			BirthDay = new DateOnly(2021, 1, 1)
		};


		// Act
		var horse = new HorseStandIn(horseCriteria);

		// Assert
		Assert.Equal("Triggered", horse.Name);
		Assert.Equal(Horse.Breed.QuarterHorse, horse.Breed);
		Assert.Equal(new DateOnly(2021, 1, 1), horse.BirthDate);
	}

	[Fact]
	public void Horse_Fetch()
	{
		var horse = new HorseStandIn();

		horse.Fetch(new HorseEntity
		{
			Id = 1,
			Name = "Triggered",
			Breed = (int)Horse.Breed.QuarterHorse,
			BirthDate = new DateOnly(2021, 1, 1)
		});

		Assert.Equal(1, horse.Id);
		Assert.Equal("Triggered", horse.Name);
		Assert.Equal(Horse.Breed.QuarterHorse, horse.Breed);
		Assert.Equal(new DateOnly(2021, 1, 1), horse.BirthDate);
		Assert.False(horse.IsNew);
	}

	[Fact]
	public void Horse_Insert_ToPasture()
	{
		var horse = new HorseStandIn()
		{
			Name = "Triggered",
			Breed = Horse.Breed.QuarterHorse,
			BirthDate = new DateOnly(2021, 1, 1)
		};

		var pasture = new PastureEntity();
		var horses = new List<HorseEntity>();

		horse.Insert(pasture);

		var horseEntity = pasture.Horses.First();

		Assert.Equal("Triggered", horseEntity.Name);
		Assert.Equal(Horse.Breed.QuarterHorse, (Horse.Breed)horseEntity.Breed);
		Assert.Equal(new DateOnly(2021, 1, 1), horseEntity.BirthDate);

		horseEntity.Id = 111;
		Assert.Equal(111, horse.Id);
	}

	[Fact]
	public void Horse_Insert_ToCart()
	{
		var horse = new HorseStandIn()
		{
			Name = "Triggered",
			Breed = Horse.Breed.QuarterHorse,
			BirthDate = new DateOnly(2021, 1, 1)
		};

		var cart = new CartEntity();
		var horses = new List<HorseEntity>();

		horse.Insert(cart);

		var horseEntity = cart.Horses.First();

		Assert.Equal("Triggered", horseEntity.Name);
		Assert.Equal(Horse.Breed.QuarterHorse, (Horse.Breed)horseEntity.Breed);
		Assert.Equal(new DateOnly(2021, 1, 1), horseEntity.BirthDate);

		horseEntity.Id = 111;
		Assert.Equal(111, horse.Id);
	}

	[Fact]
	public async Task Horse_Update_ToPasture()
	{
		var horse = new HorseStandIn()
		{
			Id = 1,
			Name = "Triggered",
			Breed = Horse.Breed.QuarterHorse,
			BirthDate = new DateOnly(2021, 1, 1)
		};

		var horseBarn = new IHorseFarmContextCreateExpectations();
		var pasture = new PastureEntity();
		var horses = new List<HorseEntity>();
		var horseDbSet = new DbSetMakeExpectations<HorseEntity>();
		var horseEntity = new HorseEntity
		{
			Id = 1
		};

		horseDbSet.Instance().Add(horseEntity);

		horseBarn.Properties.Getters.Horses().ReturnValue(horseDbSet.Instance());
		await horse.Update(pasture, horseBarn.Instance());

		Assert.Equal("Triggered", horseEntity.Name);
		Assert.Equal(Horse.Breed.QuarterHorse, (Horse.Breed)horseEntity.Breed);
		Assert.Equal(new DateOnly(2021, 1, 1), horseEntity.BirthDate);

	}
}