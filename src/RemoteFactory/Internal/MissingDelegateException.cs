namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Exception thrown when a delegate type cannot be found in the registered assemblies.
/// This typically indicates a mismatch between client and server configurations.
/// </summary>
public class MissingDelegateException : Exception
{
	public MissingDelegateException() { }
	public MissingDelegateException(string message) : base(message) { }
	public MissingDelegateException(string message, Exception inner) : base(message, inner) { }
}
