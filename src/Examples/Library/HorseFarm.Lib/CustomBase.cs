using HorseFarm.Ef;
using Neatoo.RemoteFactory;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HorseFarm.Lib;

internal interface ICustomEditBase<T>
{
	int? Id { get; }
}

internal abstract class CustomBase : INotifyPropertyChanged, IFactorySaveMeta
{
	private int? _id;

	public int? Id
	{
		get => this._id;
		set
		{
			this._id = value;
			this.OnPropertyChanged();
		}
	}

   public bool IsDeleted { get; set; }
   public bool IsNew { get; set; }

	public event PropertyChangedEventHandler? PropertyChanged;

	public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

#if !CLIENT

	/// <summary>
	/// Get the Id from the EF model entity once it is saved
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	protected void HandleIdPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		Debug.Assert(sender as IdPropertyChangedBase != null, "Unexpected null");

		if (sender is IdPropertyChangedBase id && e.PropertyName == nameof(IdPropertyChangedBase.Id))
		{
			// If the normal setting is used sets to IsModified = true
			// TODO: Anyway to not have to define <int?> ??
			this.Id = ((IdPropertyChangedBase)sender).Id;
			id.PropertyChanged -= this.HandleIdPropertyChanged;
		}
	}

#endif

}
