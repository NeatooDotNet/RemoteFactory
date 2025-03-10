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

	[Fact]
	public async Task HorseFarm_FullRun()
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

			// Key: Cannot add an ILightHorse to the wagon 
			// No validation, no if statements
			horseFarm.MoveHorseToCart(heavyHorse, wagon);

			criteria.Name = "Heavy Horse B";
			criteria.Breed = Breed.Clydesdale;

			heavyHorse = (IHeavyHorse)horseFarm.AddNewHorse(criteria);

			horseFarm.MoveHorseToCart(heavyHorse, wagon);
		}

		AddCartToHorseFarm();

		horseFarm = (IHorseFarm) (await this.horseFarmFactory.Save(horseFarm))!;

		var horseFarmContext = this.serverScope.ServiceProvider.GetRequiredService<IHorseFarmContext>();

		var horses = await horseFarmContext.Horses.ToListAsync();

		Assert.Equal(3, horses.Count);
		Assert.Equivalent(horseFarm.Horses.Select(h => h.Id).ToList(), horses.Select(h => h.Id).ToList());

		
		var carts = await horseFarmContext.Carts.ToListAsync();
		Assert.Equivalent(horseFarm.Carts.Select(c => c.Id).ToList(), carts.Select(c => c.Id).ToList());

		var pasture = await horseFarmContext.Pastures.ToListAsync();
		Assert.Equal(pasture.Single().Id, horseFarm.Pasture.Id);

		//horseFarm = await this.horseFarmFactory.Fetch();

		//AddCartToHorseFarm();

		//foreach (var item in horseFarm.Horses)
		//{
		//	item.Name = Guid.NewGuid().ToString();
		//}

		//var horseNames = horseFarm.Horses.Select(h => h.Name).ToList();

		//// Mix of Inserts and Updates
		//horseFarm = (IHorseFarm) (await this.horseFarmFactory.Save(horseFarm))!;

		//Assert.Equivalent(horseNames, horseFarmContext.Horses.Select(h => h.Name).ToList());
		//Assert.Equivalent(horseFarm.Carts.Select(c => c.Id).ToList(), horseFarmContext.Carts.Select(c => c.Id).ToList());
		//Assert.Equal(horseFarm.Pasture.Id, horseFarmContext.Pastures.Single().Id);


		//horseFarm = await this.horseFarmFactory.Fetch();

	}


}