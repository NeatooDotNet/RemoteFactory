using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests;


public interface IAuthorizeTeaser
{
	[Authorize(AuthorizeOperation.Read | AuthorizeOperation.Write)]
	bool CanCreate();

	[Authorize(AuthorizeOperation.Insert)]
	bool CanInsert();
}

public interface ITeaser : IFactorySaveMeta { }

[Factory]
[Authorize<IAuthorizeTeaser>()]
internal class Teaser : ITeaser 
 {
   public bool IsDeleted => throw new NotImplementedException();

   public bool IsNew => throw new NotImplementedException();

   [Create]
	public void Create() { }

	[Fetch]
	public bool Fetch() { return false; }

	[Create]
	public Task CreateTask() { return Task.CompletedTask; }

	[Remote]
	[Create]
	public void CreateRemote() { }

	[Create]
	public Task CreateDependency([Service] IService service) { return Task.CompletedTask; }

	[Insert]
	public void Insert([Service] IService service) { }

	[Update]
	public void Update([Service] IService service) { }

	[Delete]
	public void Delete([Service] IService service) { }

	[Insert]
	public void Insert(int p, [Service] IService service) { }

	[Update]
	public Task Update(int p, [Service] IService service) { return Task.CompletedTask; }

	[Delete]
	public Task Delete(int p, [Service] IService service) { return Task.CompletedTask; }

	[Remote]
	[Insert]
	public void InsertRemote(int? p, [Service] IService service) { }

	[Remote]
	[Update]
	public void UpdateRemote(int? p, [Service] IService service) { }

	[Remote]
	[Delete]
	public void DeleteRemote(int? p, [Service] IService service) { }

}
