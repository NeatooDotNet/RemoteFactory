using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Thrown when an authorization method refuses a factory operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>The message is never empty.</b> An authorization method may return <c>bool</c>,
/// <c>Task&lt;bool&gt;</c>, <c>string?</c> or <c>Task&lt;string?&gt;</c>. Only the string forms
/// carry a reason, so a method returning a bare <c>false</c> produces an
/// <see cref="Authorized"/> whose <see cref="Authorized.Message"/> is <c>null</c>. When that
/// happens this exception supplies its own message naming what was refused, because an
/// exception with an empty message is dropped outright by some telemetry pipelines — the
/// failure then leaves no record at all.
/// </para>
/// <para>
/// A message the authorization method did supply is used <b>verbatim</b> and is never
/// prefixed or decorated. Use <see cref="Context"/> to read the operation and type
/// independently of the message text.
/// </para>
/// </remarks>
public class NotAuthorizedException : Exception
{
	/// <summary>
	/// Creates an exception for a refused operation, with no information about which
	/// operation it was. Generated code uses the overload that supplies context.
	/// </summary>
	public NotAuthorizedException(Authorized authorized) : this(authorized, null)
	{

	}

	/// <summary>
	/// Creates an exception for a refused operation.
	/// </summary>
	/// <param name="authorized">The authorization result, as the authorization method returned it.</param>
	/// <param name="context">
	/// What was refused — operation and type, and the authorization method where a single one
	/// is responsible. Used only when <paramref name="authorized"/> carries no message of its own.
	/// </param>
	public NotAuthorizedException(Authorized authorized, string? context) : base(BuildMessage(authorized?.Message, context))
	{
		this.Authorized = authorized;
		this.Context = context;
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

	/// <summary>
	/// The authorization result that was refused, as the authorization method returned it.
	/// Null only when an overload that takes no <see cref="RemoteFactory.Authorized"/> was used.
	/// </summary>
	public Authorized? Authorized { get; }

	/// <summary>
	/// What was refused — operation and type, and the authorization method where known.
	/// Populated by generated code; null when constructed without context. Available whether
	/// or not the authorization method supplied its own message, so structured logging can
	/// capture the operation without parsing <see cref="Exception.Message"/>.
	/// </summary>
	public string? Context { get; }

	/// <summary>
	/// A supplied message wins and is used verbatim. Otherwise the message names what was
	/// refused, and says that no reason was given — so a reader can tell "the authorization
	/// method returned bare false" from "the reason went missing in transit".
	/// </summary>
	/// <remarks>
	/// Whitespace counts as absent: <c>new Authorized("")</c> and the string conversion's
	/// <c>IsNullOrEmpty</c> check both admit a denial whose message says nothing.
	/// </remarks>
	private static string BuildMessage(string? message, string? context)
	{
		if (!string.IsNullOrWhiteSpace(message))
		{
			return message!;
		}

		return context is null
			? "Authorization denied; no reason was supplied."
			: $"Authorization denied for {context}; no reason was supplied.";
	}
}
