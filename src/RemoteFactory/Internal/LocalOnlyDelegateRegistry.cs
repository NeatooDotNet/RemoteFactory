using System;
using System.Collections.Concurrent;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Static registry of delegate types that are local-only: static-factory <c>[Execute]</c>
/// methods without <c>[Remote]</c>. The generated <c>FactoryServiceRegistrar</c> declares each
/// one at startup, mirroring <see cref="DtoConstructorRegistry"/>: process-global, populated
/// from generated code, no reflection, idempotent.
/// </summary>
/// <remarks>
/// A local-only delegate is registered in every factory mode and runs wherever it is called, so a
/// client never sends a remote request for it. The server's delegate handler consults this
/// registry so a crafted request naming one is refused rather than executed with server services.
/// Membership is a compile-time property of the delegate type, so a process-wide set is correct
/// even when several containers (client, server, logical) coexist in one process, as they do in
/// the test suites.
/// </remarks>
public static class LocalOnlyDelegateRegistry
{
	private static readonly ConcurrentDictionary<Type, byte> LocalOnly = new();

	/// <summary>
	/// Declares a delegate type as local-only. Called from generated FactoryServiceRegistrar
	/// methods during application startup. Uses TryAdd so repeated registrations are idempotent.
	/// </summary>
	public static void Register(Type delegateType)
	{
		ArgumentNullException.ThrowIfNull(delegateType, nameof(delegateType));
		LocalOnly.TryAdd(delegateType, 0);
	}

	/// <summary>
	/// True when the delegate type was declared local-only and must not be served to a remote request.
	/// </summary>
	public static bool IsLocalOnly(Type delegateType)
	{
		return delegateType != null && LocalOnly.ContainsKey(delegateType);
	}
}
