using Neatoo.RemoteFactory;
using System.ComponentModel.DataAnnotations;

namespace HorseFarm.Lib.Horse;

public interface IHorseCriteria
{
    string? Name { get; set; }
    DateOnly? BirthDay { get; set; }
    Breed Breed { get; set; }
}

[Factory]
internal class HorseCriteria : CustomBase, IHorseCriteria
{

    [Required]
    public string? Name { get; set { field = value; this.OnPropertyChanged(); } } = null!;

	[Required]
    public Breed Breed { get => Getter<Breed>(); set => Setter(value); }

    [Required]
    public DateOnly? BirthDay { get => Getter<DateOnly?>(); set => Setter(value); }

    private IReadOnlyCollection<string> HorseNames { get; set; } = [];

    [Fetch]
    public void Fetch()
    {
    }

    [Fetch]
    public void Fetch(IEnumerable<string> horseNames)
    {
	  this.HorseNames = horseNames.ToList();
    }

}
