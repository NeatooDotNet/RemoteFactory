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
/// Marks a static method as a mediator-style event handler discoverable by <see cref="IFactoryEvents"/>.
/// The first non-[Service] parameter must be a type inheriting from <see cref="FactoryEventBase"/>.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="EventAttribute"/> (which generates per-handler delegates), this attribute
/// participates in the mediator pattern: multiple handlers for the same event type are discovered
/// at compile time and dispatched via <see cref="IFactoryEvents.Raise{T}"/>.
/// </para>
/// <para>
/// Must be placed on a <c>static</c> method in a <c>[Factory]</c> class (static or non-static).
/// The method must have <see cref="CancellationToken"/> as its final parameter.
/// </para>
/// </remarks>
public sealed class FactoryEventHandlerAttribute : FactoryOperationAttribute
{
	public FactoryEventHandlerAttribute() : base(FactoryOperation.FactoryEventHandler) { }
}

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

