using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory;

public class Authorized
{
	[JsonInclude]
	public bool HasAccess { get; init; }

	[JsonInclude]
	public string? Message { get; init; }

	public Authorized()
	{
		this.HasAccess = false;
	}

	public Authorized(bool hasAccess)
	{
		this.HasAccess = hasAccess;
	}

	public Authorized(string? message)
	{
		this.HasAccess = false;
		this.Message = message;
	}

	[JsonConstructor]
	public Authorized(bool hasAccess, string? message)
	{
		this.HasAccess = hasAccess;
		this.Message = message;
	}

	public static implicit operator Authorized(string? message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return new Authorized(true);
		}

		return new Authorized(message);
	}

	public static implicit operator bool(Authorized result)
	{
		return result?.HasAccess ?? false;
	}

	public static implicit operator Authorized(bool result)
	{
		return new Authorized(result);
	}

   public Authorized ToAuthorized()
   {
	  throw new NotImplementedException();
   }

   public bool ToBoolean()
   {
	  throw new NotImplementedException();
   }
}

public class Authorized<T> : Authorized
{
	[JsonInclude]
	public T? Result { get; init; }

	public Authorized()
	{
	}

	public Authorized(Authorized result)
	{
		ArgumentNullException.ThrowIfNull(result, nameof(result));
		this.HasAccess = result.HasAccess;
		this.Message = result.Message;
	}

	public Authorized(T? result)
	{
		this.Result = result;
		if(result == null)
		{
			this.HasAccess = false;
		}
		else
		{
			this.HasAccess = true;
		}
	}

   public T? ToT()
   {
		return this.Result;
   }
}
