using Neatoo.RemoteFactory;

namespace Person.DomainModel;

public interface IPersonModelAuth
{
	[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
	public bool CanAccess();
	[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
	public bool CanCreate();
	[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
	public bool CanFetch();
	[AuthorizeFactory(AuthorizeFactoryOperation.Update)]
	public bool CanUpdate();
	[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
	public bool CanDelete();
}

internal class PersonModelAuth : IPersonModelAuth
{
	public PersonModelAuth(IUser user)
	{
		this.User = user;
	}

	public IUser User { get; }

	public bool CanAccess()
	{
		if (this.User.Role > Role.None)
		{
			return true;
		}
		return false;
	}

	public bool CanCreate()
	{
		if (this.User.Role >= Role.Create)
		{
			return true;
		}
		return false;
	}

	public bool CanFetch()
	{
		if (this.User.Role >= Role.Fetch)
		{
			return true;
		}
		return false;
	}

	public bool CanUpdate()
	{
		if (this.User.Role >= Role.Update)
		{
			return true;
		}
		return false;
	}

	public bool CanDelete()
	{
		if (this.User.Role >= Role.Delete)
		{
			return true;
		}
		return false;
	}
}
