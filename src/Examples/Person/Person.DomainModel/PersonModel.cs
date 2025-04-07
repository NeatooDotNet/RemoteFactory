using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Neatoo.RemoteFactory;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Person.DomainModel;

public interface IPersonModel : INotifyPropertyChanged, IFactorySaveMeta
{
	string? FirstName { get; set; }
	string? LastName { get; set; }
	string? Email { get; set; }
	string? Phone { get; set; }
	string? Notes { get; set; }
	DateTime Created { get; }
	DateTime Modified { get; }
	new bool IsDeleted { get; set; }
}

[Factory]
[Authorize<IPersonModelAuth>]
internal partial class PersonModel : IPersonModel
{
	[Create]
	public PersonModel()
	{
		this.Id = 1;
		this.Created = DateTime.Now;
		this.Modified = DateTime.Now;
	}

	public int Id { get; set; }

	[Required(ErrorMessage = "First Name is required")]
	public string? FirstName { get; set { field = value; this.OnPropertyChanged(); } }

	[Required(ErrorMessage = "Last Name is required")]
	public string? LastName { get; set { field = value; this.OnPropertyChanged(); } }
	public string? Email { get; set { field = value; this.OnPropertyChanged(); } }
	public string? Phone { get; set { field = value; this.OnPropertyChanged(); } }
	public string? Notes { get; set { field = value; this.OnPropertyChanged(); } }
	public DateTime Created { get; set { field = value; this.OnPropertyChanged(); } }
	public DateTime Modified { get; set { field = value; this.OnPropertyChanged(); } }
	public bool IsDeleted { get; set; }
	public bool IsNew { get; set; } = true;

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public partial void MapFrom(PersonEntity personEntity);
	public partial void MapTo(PersonEntity personEntity);

	[Remote]
	[Fetch]
	public async Task<bool> Fetch([Service] IPersonContext personContext)
	{
		var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1);
		if (personEntity == null)
		{
			return false;
		}
		this.MapFrom(personEntity);
		this.IsNew = false;
		return true;
	}

	[Remote]
	[Update]
	[Insert]
	public async Task Upsert([Service] IPersonContext personContext)
	{
		var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == this.Id);
		if(personEntity == null)
		{
			personEntity = new PersonEntity();
			personContext.Persons.Add(personEntity);
		}
		this.MapTo(personEntity);
		await personContext.SaveChangesAsync();
	}

	[Remote]
	[Delete]
	public async Task Delete([Service] IPersonContext personContext)
	{
		var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == this.Id);

		if (personEntity != null)
		{
			personContext.Persons.Remove(personEntity);
			await personContext.SaveChangesAsync();
		}
	}
}
