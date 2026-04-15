#if NETSTANDARD
namespace Neatoo.RemoteFactory.FactoryGenerator;

#else
namespace Neatoo.RemoteFactory;
#endif

#pragma warning disable CA1813

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
public sealed class FactoryAttribute : Attribute
{
	public FactoryAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true, AllowMultiple = false)]
public sealed class SuppressFactoryAttribute : Attribute
{
	public SuppressFactoryAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class RemoteAttribute : Attribute
{
	public RemoteAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class FactoryOperationAttribute : Attribute
{
	public FactoryOperation Operation { get; }

	public FactoryOperationAttribute(FactoryOperation operation)
	{
		this.Operation = operation;
	}
}

[System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class ServiceAttribute : Attribute
{
	public ServiceAttribute()
	{
	}
}

[System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CreateAttribute : Attribute
{
	public FactoryOperation Operation { get; }

	public CreateAttribute()
	{
		this.Operation = FactoryOperation.Create;
	}
}

[System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
public sealed class FetchAttribute : Attribute
{
	public FactoryOperation Operation { get; }
	public FetchAttribute()
	{
		this.Operation = FactoryOperation.Fetch;
	}
}

public sealed class InsertAttribute : FactoryOperationAttribute
{
	public InsertAttribute() : base(FactoryOperation.Insert) { }
}

public sealed class UpdateAttribute : FactoryOperationAttribute
{
	public UpdateAttribute() : base(FactoryOperation.Update) { }
}

public sealed class DeleteAttribute : FactoryOperationAttribute
{
	public DeleteAttribute() : base(FactoryOperation.Delete) { }
}

public sealed class ExecuteAttribute : FactoryOperationAttribute
{
	public ExecuteAttribute() : base(FactoryOperation.Execute) { }
}

/// <summary>
/// Marks a method as an event handler that runs in an isolated scope.
/// Event handlers use fire-and-forget semantics with scope isolation for transactionally-independent operations.
/// </summary>
/// <remarks>
/// <para>
/// The method must have <see cref="CancellationToken"/> as its final parameter.
/// The generated delegate will always return <see cref="Task"/>, even for void methods.
/// </para>
/// <para>
/// Service parameters marked with [Service] are resolved from the new scope.
/// The CancellationToken receives ApplicationStopping for graceful shutdown support.
/// </para>
/// </remarks>
public sealed class EventAttribute : FactoryOperationAttribute
{
	public EventAttribute() : base(FactoryOperation.Event) { }
}

/// <summary>
/// Marks a class as a handler for factory events of type <typeparamref name="T"/>.
/// The handler method must be <c>static</c>, return <see cref="Task"/>, and have a first
/// non-[Service]/non-CancellationToken parameter of type <typeparamref name="T"/>.
/// <para>
/// Handlers are registered in <see cref="FactoryEventHandlerRegistry"/> and dispatched
/// via <see cref="IFactoryEvents.Raise{T}"/> on the server, in the caller's DI scope,
/// sequentially, awaited.
/// </para>
/// <para>
/// Instance-method handlers are silently ignored by the generator — they were the
/// former client-side relay pattern, now replaced by <see cref="IFactoryEventRelay"/>.
/// Client-side event consumers implement <see cref="IFactoryEventRelay"/> to bridge
/// relayed events to their own event aggregator.
/// </para>
/// </summary>
/// <typeparam name="T">The event type (must inherit from <see cref="FactoryEventBase"/>).</typeparam>
[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
#pragma warning disable CA1813 // Sealed generic attribute must be unsealed for generator discovery
public sealed class FactoryEventHandlerAttribute<T> : Attribute { }
#pragma warning restore CA1813

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class AuthorizeFactoryAttribute<T> : Attribute
{
	public AuthorizeFactoryAttribute() { }
}

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class AuthorizeFactoryAttribute : Attribute
{
	public AuthorizeFactoryOperation Operation { get; }
	public AuthorizeFactoryAttribute(AuthorizeFactoryOperation operation)
	{
		this.Operation = operation;
	}
}

[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class FactoryHintNameLengthAttribute : Attribute
{
	// See the attribute guidelines at 
	//  http://go.microsoft.com/fwlink/?LinkId=85236
	readonly int maxHintNameLength;

	// This is a positional argument
	public FactoryHintNameLengthAttribute(int maxHintNameLength)
	{
		this.maxHintNameLength = maxHintNameLength;
	}

	public int MaxHintNameLength => this.maxHintNameLength;
}

#if !NETSTANDARD
/// <summary>
/// Assembly-level attribute emitted by the source generator for each factory type.
/// <see cref="RegisterFactories"/> enumerates these to discover FactoryServiceRegistrar methods
/// in a trimming-safe way (replaces the trim-unsafe assembly.GetTypes() scan).
/// </summary>
[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class NeatooFactoryRegistrarAttribute : Attribute
{
	public NeatooFactoryRegistrarAttribute(
		[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
			System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods |
			System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
	{
		Type = type;
	}

	[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
		System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods |
		System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public Type Type { get; }
}
#endif

