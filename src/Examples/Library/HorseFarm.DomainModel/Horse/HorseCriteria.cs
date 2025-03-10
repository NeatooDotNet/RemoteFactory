using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace HorseFarm.DomainModel.Horse;

public interface IHorseCriteria
{
	string? Name { get; set; }
	DateOnly? BirthDay { get; set; }
	Breed? Breed { get; set; }
}

[Factory]
internal sealed class HorseCriteria : CustomBase, IHorseCriteria
{

	[Create]
	public HorseCriteria()
	{
	}

	[Required]
	public string? Name { get; set { field = value; this.OnPropertyChanged(); } } = null;

	[Required]
	public Breed? Breed { get; set { field = value; this.OnPropertyChanged(); } }

	[Required]
	public DateOnly? BirthDay { get; set { field = value; this.OnPropertyChanged(); } } = null;


}
