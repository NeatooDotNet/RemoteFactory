using HorseFarm.Lib.Cart;
using HorseFarm.Lib.Horse;
using Neatoo.RemoteFactory;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using HorseFarm.Ef;

namespace HorseFarm.Lib;

public interface IHorseFarm
{
    internal int? Id { get; }
    IPasture Pasture { get; }
    INotifyCollectionChanged Carts { get; }
    IEnumerable<IHorse> Horses { get; }
    IHorse AddNewHorse(IHorseCriteria horseCriteria);
    IRacingChariot AddRacingChariot();
    IWagon AddWagon();
    void MoveHorseToCart(IHorse horse, ICart cart);
    void MoveHorseToPasture(IHorse horse);
}

[Factory]
internal class HorseFarm : CustomBase, IHorseFarm
{
    private readonly ILightHorseFactory lightHorseFactory;
    private readonly IHeavyHorseFactory heavyHorseFactory;
    private readonly IRacingChariotFactory racingChariotFactory;
    private readonly IWagonFactory wagonFactory;

    public HorseFarm(ILightHorseFactory lightHorseFactory,
                        IHeavyHorseFactory heavyHorseFactory,
                        IRacingChariotFactory racingChariotFactory,
                        IWagonFactory wagonFactory)
    {
        this.lightHorseFactory = lightHorseFactory;
        this.heavyHorseFactory = heavyHorseFactory;
        this.racingChariotFactory = racingChariotFactory;
        this.wagonFactory = wagonFactory;
    }

	public IPasture Pasture { get; set { field = value; this.OnPropertyChanged(); } }
	public ICartCollection Carts { get; set { field = value; this.OnPropertyChanged(); } }
	public IEnumerable<IHorse> Horses => this.Carts.SelectMany(c => c.Horses).Union(this.Pasture.HorseList);

    INotifyCollectionChanged IHorseFarm.Carts => this.Carts;

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

        if (IHorse.IsLightHorse(horseCriteria.Breed))
        {
            horse = this.lightHorseFactory.Create(horseCriteria);
        }
        else if (IHorse.IsHeavyHorse(horseCriteria.Breed))
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
	  this.Pasture.RemoveHorse(horse);
	  this.Carts.RemoveHorse(horse);
        cart.AddHorse(horse);
    }

    public void MoveHorseToPasture(IHorse horse)
    {
	  this.Carts.RemoveHorse(horse);

        if (!this.Pasture.HorseList.Contains(horse))
        {
		 this.Pasture.HorseList.Add(horse);
        }
    }

    [Create]
    public void Create([Service] IPastureFactory pastureFactory, [Service] ICartCollectionFactory cartCollectionFactory)
    {
        this.Pasture = pastureFactory.Create();
        this.Carts = cartCollectionFactory.Create();
    }

#if !CLIENT

    [Remote]
    [Fetch]
    public async Task Fetch([Service] IHorseFarmContext horseBarnContext,
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
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IHorseFarmContext horseBarnContext,
                            [Service] IPastureFactory pastureFactory,
                            [Service] ICartCollectionFactory cartFactory)
    {
        var horseBarn = new HorseFarmEntity();

        horseBarn.PropertyChanged += HandleIdPropertyChanged;

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
        if (this.Pasture.IsModified)
        {
            pastureFactory.Save(this.Pasture, horseBarn);
        }

        if (this.Carts.IsModified)
        {
            cartFactory.Save(this.Carts, horseBarn);
        }

        await horseBarnContext.SaveChangesAsync();
    }

#endif

#if CLIENT

    [Fetch]
    [Remote]
    public void Fetch()
    {
    }

    [Update]
    [Remote]
    public void Update()
    {
    }
#endif

}
