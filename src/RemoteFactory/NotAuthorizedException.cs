using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory;

public class NotAuthorizedException : Exception
{
	public NotAuthorizedException(Authorized authorized) : base(authorized?.Message ?? "")
	{

	}
	public NotAuthorizedException(string message) : base(message)
	{
	}

	public NotAuthorizedException()
	{
	}

	public NotAuthorizedException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
