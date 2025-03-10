using HorseFarm.DomainModel.Horse;

namespace Blazor.HorseFarm;

public class HorseDragDrop
{
	public IHorse? Horse { get; set; }

	public event EventHandler? OnDrop;

	public void Dropped()
	{
		this.Horse = null;
		this.OnDrop?.Invoke(this, EventArgs.Empty);
	}
}
