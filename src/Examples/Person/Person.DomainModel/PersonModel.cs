using Neatoo.RemoteFactory;
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

internal partial class PersonModel : IPersonModel
{
	[Create]
	public PersonModel()
	{
		this.Created = DateTime.Now;
		this.Modified = DateTime.Now;
	}

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

}
