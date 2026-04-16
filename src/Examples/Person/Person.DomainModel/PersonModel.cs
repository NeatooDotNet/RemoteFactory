using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Neatoo.RemoteFactory;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

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

	/// <summary>
	/// Marks this entity for deletion on the next Save. External callers can't flip
	/// <see cref="IFactorySaveMeta.IsDeleted"/> directly — this is the one domain-sanctioned path.
	/// </summary>
	void MarkDeleted();
}

[Factory]
[AuthorizeFactory<IPersonModelAuth>]
// Keep the public property getters visible after IL trimming. Without this, the trimmer
// downgrades `public bool IsNew { get; private set; }` to `private` (since no concrete-type
// call site reads the getter — all reads go via IPersonModel / IFactorySaveMeta), and STJ's
// reflection-based serializer then skips these properties outbound. [JsonInclude] alone isn't
// enough because it addresses the *accessor* visibility, not the *property* visibility.
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
internal class PersonModel : IPersonModel
{
	// [DynamicDependency] roots the property metadata (not just reflection visibility) so the
	// trimmer can't narrow the getter from public to private. Without this, IsNew / IsDeleted
	// silently drop out of the outbound JSON payload and Save routing breaks (everything
	// routes to Insert because IsNew defaults to true on the server's freshly constructed
	// instance). Must point at members by string because DynamicallyAccessedMembers on the
	// class preserves reflection metadata but doesn't prevent visibility narrowing.
	[DynamicDependency(nameof(IsNew))]
	[DynamicDependency(nameof(IsDeleted))]
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
	// [JsonInclude] lets System.Text.Json both read AND write private-setter properties.
	// Private setters keep external callers from flipping state ad-hoc; the serializer
	// still round-trips the flags across the client/server boundary.
	[JsonInclude]
	public bool IsDeleted { get; private set; }

	[JsonInclude]
	public bool IsNew { get; private set; } = true;

	public void MarkDeleted() => this.IsDeleted = true;

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	[Remote]
	[Fetch]
	internal async Task<bool> Fetch([Service] IPersonContext personContext, CancellationToken ct)
	{
		var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1, ct);
		if (personEntity == null)
		{
			return false;
		}
		this.FirstName = personEntity.FirstName;
		this.LastName = personEntity.LastName;
		this.Email = personEntity.Email;
		this.Phone = personEntity.Phone;
		this.Notes = personEntity.Notes;
		this.Created = personEntity.Created;
		this.Modified = personEntity.Modified;
		this.IsNew = false;
		return true;
	}

	[Remote]
	[Update]
	[Insert]
	internal async Task Upsert(
		[Service] IPersonContext personContext,
		[Service] IFactoryEvents factoryEvents,
		CancellationToken ct)
	{
		var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1, ct);
		bool isInsert = personEntity == null;
		if(personEntity == null)
		{
			personEntity = new PersonEntity();
			personContext.Persons.Add(personEntity);
		}
		personEntity.FirstName = this.FirstName ?? throw new InvalidOperationException("PersonModel.FirstName is required");
		personEntity.LastName = this.LastName ?? throw new InvalidOperationException("PersonModel.LastName is required");
		personEntity.Email = this.Email;
		personEntity.Phone = this.Phone;
		personEntity.Notes = this.Notes;
		personEntity.Created = this.Created;
		personEntity.Modified = this.Modified;
		await personContext.SaveChangesAsync(ct);

		// Factory event handlers share this method's DI scope, so any
		// [FactoryEventHandler<PersonCreatedEvent>] that injects IPersonContext
		// will see THIS personContext and participate in its transaction.
		// A throwing handler rolls the whole Upsert back.
		if (isInsert)
			await factoryEvents.Raise(new PersonCreatedEvent(personEntity.Id), RaiseOptions.None, ct);
		else
			await factoryEvents.Raise(new PersonUpdatedEvent(personEntity.Id), RaiseOptions.None, ct);
	}

	[Remote]
	[Delete]
	internal async Task Delete(
		[Service] IPersonContext personContext,
		[Service] IFactoryEvents factoryEvents,
		CancellationToken ct)
	{
		var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1, ct);

		if (personEntity != null)
		{
			var id = personEntity.Id;
			personContext.Persons.Remove(personEntity);
			await personContext.SaveChangesAsync(ct);
			await factoryEvents.Raise(new PersonDeletedEvent(id), RaiseOptions.None, ct);
		}
	}
}
