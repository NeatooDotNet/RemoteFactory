namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// The body of a 403 that carries a <see cref="NotAuthorizedException"/> from the server to
/// the client (#103). The <see cref="HeaderName"/> header marks it, so a 403 from anything
/// else, such as an authentication scheme's forbid, is not read as one.
/// </summary>
internal sealed class NotAuthorizedResponseDto
{
	public const string HeaderName = "X-Neatoo-Not-Authorized";

	public string Message { get; set; } = string.Empty;

	public string? Context { get; set; }

	public Authorized? Authorized { get; set; }

	public static NotAuthorizedResponseDto From(NotAuthorizedException exception) => new()
	{
		Message = exception.Message,
		Context = exception.Context,
		Authorized = exception.Authorized,
	};

	public NotAuthorizedException ToException() => new(Message, Authorized, Context);
}
