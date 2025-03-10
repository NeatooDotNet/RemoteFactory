using HorseFarm.DomainModel.Horse;
using HorseFarm.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace HorseFarm.DomainModel.IntegrationTests;

public class HorseFarmTests : IAsyncLifetime
{
	private IServiceScope clientScope = null!;
	private IServiceScope serverScope = null!;
	private IHorseFarmFactory horseFarmFactory = null!;
	private IHorseFarmContext horseFarmContext = null!;
	private IHorseCriteriaFactory horseCriteriaFactory = null!;
	private IDbContextTransaction transaction = null!;

	public async Task InitializeAsync()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client; // Important that these two are paired
		this.serverScope = scopes.server;
		this.horseFarmFactory = this.clientScope.ServiceProvider.GetRequiredService<IHorseFarmFactory>();
		this.horseFarmContext = this.serverScope.ServiceProvider.GetRequiredService<IHorseFarmContext>();
		this.horseCriteriaFactory = this.clientScope.ServiceProvider.GetRequiredService<IHorseCriteriaFactory>();

		await this.horseFarmContext.Horses.ExecuteDeleteAsync();
		await this.horseFarmContext.Carts.ExecuteDeleteAsync();
		await this.horseFarmContext.Pastures.ExecuteDeleteAsync();
		await this.horseFarmContext.HorseFarms.ExecuteDeleteAsync();

		this.transaction = await ((HorseFarmContext) this.horseFarmContext).Database.BeginTransactionAsync();
	}
	public async Task DisposeAsync()
	{
		await this.transaction.RollbackAsync();
		this.clientScope.Dispose();
	}

	private async Task<IHorseFarm> CreateAndSaveHorseFarm()
	{
		var horseFarm = this.horseFarmFactory.Create();

		void AddCartToHorseFarm()
		{
			var criteria = this.horseCriteriaFactory.Create();

			criteria.Name = "Heavy Horse A";
			criteria.Breed = Breed.Clydesdale;
			criteria.BirthDay = DateOnly.FromDateTime(DateTime.Now);

			var heavyHorse = (IHeavyHorse)horseFarm.AddNewHorse(criteria);

			criteria.Name = "Light Horse B";
			criteria.Breed = Breed.Thoroughbred;

			var lightHorse = (ILightHorse)horseFarm.AddNewHorse(criteria);

			var racingChariot = horseFarm.AddRacingChariot();
			var wagon = horseFarm.AddWagon();

			racingChariot.Name = "Racing Chariot";
			wagon.Name = "Wagon";
			wagon.NumberOfHorses = 2;

			// Key Point: Cannot add a ILightHorse to the wagon 
			horseFarm.MoveHorseToCart(heavyHorse, wagon);

			criteria.Name = "Heavy Horse B";
			criteria.Breed = Breed.Clydesdale;

			heavyHorse = (IHeavyHorse)horseFarm.AddNewHorse(criteria);

			horseFarm.MoveHorseToCart(heavyHorse, wagon);
		}

		AddCartToHorseFarm();

		horseFarm = (IHorseFarm)(await this.horseFarmFactory.Save(horseFarm))!;

		return horseFarm;
	}

	[Fact]
	public async Task HorseFarm_CreateAndSave()
	{
		var horseFarm = await this.CreateAndSaveHorseFarm();

		var horseFarmContext = this.serverScope.ServiceProvider.GetRequiredService<IHorseFarmContext>();

		var horses = await horseFarmContext.Horses.ToListAsync();

		Assert.Equal(3, horses.Count);
		Assert.Equivalent(horseFarm.Horses.Select(h => h.Id).ToList(), horses.Select(h => h.Id).ToList());

		
		var carts = await horseFarmContext.Carts.ToListAsync();
		Assert.Equivalent(horseFarm.Carts.Select(c => c.Id).ToList(), carts.Select(c => c.Id).ToList());

		var pasture = await horseFarmContext.Pastures.ToListAsync();
		Assert.Equal(pasture.Single().Id, horseFarm.Pasture.Id);

	}

	[Fact]
	public async Task HorseFarm_FetchAndSave()
	{
		var horseFarm = await this.CreateAndSaveHorseFarm();

		horseFarm = await this.horseFarmFactory.Fetch();

		Assert.False(horseFarm!.IsNew);

		var criteria = this.horseCriteriaFactory.Create();

		criteria.Name = "Heavy Horse C";
		criteria.Breed = Breed.Clydesdale;
		criteria.BirthDay = DateOnly.FromDateTime(DateTime.Now);

		var heavyHorse = (IHeavyHorse)horseFarm.AddNewHorse(criteria);

		criteria.Name = "Light Horse C";
		criteria.Breed = Breed.Thoroughbred;
		criteria.BirthDay = DateOnly.FromDateTime(DateTime.Now);

		var lightHorse = (ILightHorse)horseFarm.AddNewHorse(criteria);

		horseFarm = await this.horseFarmFactory.Save(horseFarm);



	}
}