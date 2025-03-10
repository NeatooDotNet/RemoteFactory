using HorseFarm.Ef;
using HorseFarm.DomainModel.Horse;
using Neatoo.RemoteFactory;
using System.Collections.Specialized;

namespace HorseFarm.DomainModel;

public interface IPasture : ICustomBase
{
	internal IHorseCollection HorseList { get; }
	public INotifyCollectionChanged Horses { get; }
	internal void RemoveHorse(IHorse horse);
}

[Factory]
internal sealed class Pasture : CustomBase, IPasture
{
	public IHorseCollection HorseList { get; set { field = value; this.OnPropertyChanged(); } } = null!;

	public INotifyCollectionChanged Horses => this.HorseList;

	public void RemoveHorse(IHorse horse)
	{
		this.HorseList.RemoveHorse(horse);
	}

	[Create]
	public void Create([Service] IHorseCollectionFactory horseListFactory)
	{
		this.HorseList = horseListFactory.Create();
	}

#if !CLIENT

	[Fetch]
	public void Fetch(PastureEntity pasture, [Service] IHorseCollectionFactory horseListFactory)
	{
		this.Id = pasture.Id;
		this.HorseList = horseListFactory.Fetch(pasture.Horses);
	}

	[Insert]
	public void Insert(HorseFarmEntity horseBarn, [Service] IHorseCollectionFactory horseListFactory)
	{
		var pasture = new PastureEntity();
		pasture.PropertyChanged += this.HandleIdPropertyChanged;
		horseBarn.Pasture = pasture;
		horseListFactory.Save(this.HorseList, pasture);
	}

	[Update]
	public void Update(HorseFarmEntity horseBarn, [Service] IHorseCollectionFactory horseListFactory)
	{
		var pasture = horseBarn.Pasture;
		horseListFactory.Save(this.HorseList, pasture);
	}

#endif

}
