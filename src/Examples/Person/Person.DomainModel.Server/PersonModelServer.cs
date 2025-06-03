using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using Person.Ef;

namespace Person.DomainModel.Server;

[Factory<IPersonModel>]
[AuthorizeFactory<IPersonModelAuth>]
internal sealed partial class PersonModelServer : PersonModel
{

	public partial void MapFrom(PersonEntity personEntity);
	public partial void MapTo(PersonEntity personEntity);

	[Create]
	[Remote]
	public void Create()
	{
		this.Created = DateTime.Now;
		this.Modified = DateTime.Now;
		this.IsNew = true;
	}

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
		var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1);
		if (personEntity == null)
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
		var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1);

		if (personEntity != null)
		{
			personContext.Persons.Remove(personEntity);
			await personContext.SaveChangesAsync();
		}
	}
}
