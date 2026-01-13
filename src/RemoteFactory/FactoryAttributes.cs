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

/// <summary>
/// Specifies the factory generation mode for an assembly.
/// </summary>
public enum FactoryMode
{
	/// <summary>
	/// Generate full factory with both local and remote execution paths.
	/// This is the default mode for server assemblies.
	/// </summary>
	Full = 0,

	/// <summary>
	/// Generate factory with remote HTTP stubs only.
	/// Use this mode for client assemblies that call server-side factories via HTTP.
	/// Local methods that call entity methods are not generated.
	/// </summary>
	RemoteOnly = 1
}

/// <summary>
/// Specifies the factory generation mode for the assembly.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="FactoryMode.Full"/> (default) for server assemblies that need
/// both local execution and remote delegate handling.
/// </para>
/// <para>
/// Use <see cref="FactoryMode.RemoteOnly"/> for client assemblies (e.g., Blazor WASM)
/// that only need to call server-side factories via HTTP. This produces smaller
/// assemblies by excluding local method implementations and their dependencies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In client assembly's AssemblyAttributes.cs:
/// [assembly: FactoryMode(FactoryMode.RemoteOnly)]
/// </code>
/// </example>
[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class FactoryModeAttribute : Attribute
{
	/// <summary>
	/// Gets the factory generation mode.
	/// </summary>
	public FactoryMode Mode { get; }

	/// <summary>
	/// Initializes a new instance of the FactoryModeAttribute.
	/// </summary>
	/// <param name="mode">The factory generation mode for this assembly.</param>
	public FactoryModeAttribute(FactoryMode mode)
	{
		Mode = mode;
	}
}
