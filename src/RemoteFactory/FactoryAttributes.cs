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

/// <summary>
/// Marks a static method on a <c>[Factory]</c> static class as a request-response command.
/// The generator emits a delegate type and its DI registration.
/// </summary>
/// <remarks>
/// <para>
/// <b>Trimming:</b> mark the method <c>private static</c> (the convention used throughout
/// the Design projects) and the generated local registration is guarded by
/// <c>NeatooRuntime.IsServerRuntime</c>, so on a client published with the feature switch
/// set to <c>false</c> the method body, its <c>[Service]</c> dependencies, and their
/// transitive references are removed from the output.
/// </para>
/// <para>
/// <b><c>[Remote]</c> is decorative on <c>[Execute]</c> methods.</b> Static factories are
/// exempt from the NF0105 <c>[Remote] public</c> check, and the renderer emits both a remote
/// and a local registration for every delegate regardless of whether <c>[Remote]</c> is
/// present — only the local one is feature-switch guarded. Trimming of the body therefore
/// depends on the guard, not on <c>[Remote]</c>. This differs from class factories, where
/// <c>[Remote] internal</c> is what drives the guard, and several documentation pages
/// present <c>[Remote]</c> as the trimming-enabling marker generally.
/// </para>
/// </remarks>
public sealed class ExecuteAttribute : FactoryOperationAttribute
{
	public ExecuteAttribute() : base(FactoryOperation.Execute) { }
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
/// <para>
/// <b>Trimming:</b> every generated handler registration is wrapped in
/// <c>NeatooRuntime.IsServerRuntime</c>, so on a client published with the feature switch
/// set to <c>false</c> the handler bodies and their <c>[Service]</c> dependencies are
/// removed from the output. This works because the generator points the assembly's
/// <see cref="NeatooFactoryRegistrarAttribute"/> at a generated forwarding holder rather
/// than at the handler class itself — the attribute preserves every method on whatever it
/// names, bodies included, so naming the handler class would ship the handler bodies to the
/// browser. Fixed in v1.7.0; before that, it did.
/// </para>
/// <para>
/// One consequence for consumers: because the registrations are server-guarded, there is
/// nothing on a trimmed client to resolve, and handler registration cannot be verified from
/// a client-side test. Coverage for it lives in server-side/untrimmed tests.
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
/// <remarks>
/// <para>
/// <b>CONTRACT: the <see cref="Type"/> must be a GENERATED single-method registrar holder.
/// Never a consumer's own class — and "generated" alone is not enough.</b>
/// (One leg does not yet satisfy this: see the interface-factory exception below.)
/// </para>
/// <para>
/// The <c>[DynamicallyAccessedMembers]</c> annotation below preserves <b>every method on the
/// named type, method bodies included</b>. Naming a consumer's class therefore ships that
/// class's <c>[Remote]</c> server-only method bodies — SQL, business rules, credentials,
/// whatever they contain — to a trimmed Blazor WebAssembly client, where they are
/// decompilable. That is the exact opposite of what RemoteFactory promises, and it is
/// invisible: everything compiles, every test passes, and the only symptom is code sitting
/// in a published <c>.wasm</c> that should never have left the server.
/// </para>
/// <para>
/// This is not hypothetical, and it happened twice for two different reasons. Static
/// <c>[Factory]</c> classes and <c>[FactoryEventHandler&lt;T&gt;]</c> classes have no separate
/// generated type to host <c>FactoryServiceRegistrar</c> — the generator re-opens the user's
/// own partial class — so from v0.21.2 until v1.7.0, both pointed here at the consumer's class
/// and leaked their bodies.
/// </para>
/// <para>
/// <b>"Generated, not consumer" is necessary but NOT sufficient, and assuming otherwise cost a
/// second defect.</b> Class factories always pointed at the <i>generated</i>
/// <c>{X}Factory</c> — and still leaked, because that type hosts every <c>Local*</c> method and
/// the annotation preserves all of them. What bounds the damage is not that the named type is
/// generated but that it has exactly <b>one</b> method. All three legs now emit a forwarding
/// holder (<c>NeatooFactoryRegistrar_{TypeName}</c>,
/// <c>NeatooEventHandlerRegistrar_{TypeName}</c>, <c>NeatooClassFactoryRegistrar_{TypeName}</c>),
/// with distinct prefixes so a class carrying several factory attributes does not collide.
/// </para>
/// <para>
/// The interface-factory leg still names <c>{ImplName}Factory</c>. No leak has been observed
/// there, but the leg reaches its implementation through interfaces, so a client-side trimmed
/// test cannot report on body elimination either way. Treat it as unverified, not proven.
/// </para>
/// <para>
/// A holder is also not sufficient on its own for a class factory. The
/// <c>IsServerRuntime</c> guard inside each <c>Local*</c> method does the other half, and for
/// <c>async</c> operations that guard must be emitted in a <b>non-async wrapper</b> forwarding
/// to a private core — inside an <c>async</c> method the guard is lowered into <c>MoveNext</c>
/// within the builder's protected region, where the trimmer folds the switch but leaves the
/// unreachable remainder in place. Removing either half reopens the leak; that was measured,
/// not reasoned.
/// </para>
/// <para>
/// Consequences for anyone editing the generator: point this attribute at a type that exists
/// only to forward, keep its surface to the one <c>FactoryServiceRegistrar</c> method, and do
/// not "simplify" it away by naming the class that already has the method. Pointing it
/// somewhere convenient is what caused the defect.
/// </para>
/// <para>
/// The annotation is deliberately <b>not</b> narrowed to <c>PublicMethods</c>.
/// <c>DynamicallyAccessedMemberTypes</c> has no sub-method granularity, so no narrowing can
/// keep <c>FactoryServiceRegistrar</c> rooted while dropping siblings — the holder indirection
/// is the only mechanism that shrinks the blast radius. Narrowing would also silently unroot
/// the registrars of any prebuilt library compiled by an older generator, whose registrar is
/// <c>internal static</c>: its factories would stop registering on a trimmed client with no
/// diagnostic and no exception.
/// </para>
/// <para>
/// The method this attribute exists to reach is looked up by the literal name
/// <c>"FactoryServiceRegistrar"</c> and invoked with a null-conditional call, so a holder whose
/// method is renamed or missing produces <b>no diagnostic and no exception</b> — registration
/// simply stops for that type and surfaces later as an unrelated DI resolution failure.
/// </para>
/// </remarks>
[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class NeatooFactoryRegistrarAttribute : Attribute
{
	/// <param name="type">
	/// The generated registrar holder type. Must never be a consumer-authored class — see the
	/// contract on the type-level documentation for why.
	/// </param>
	public NeatooFactoryRegistrarAttribute(
		[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
			System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods |
			System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
	{
		Type = type;
	}

	/// <summary>
	/// The generated registrar holder whose <c>FactoryServiceRegistrar</c> method is invoked
	/// during registration. Every method on this type is preserved under trimming, bodies
	/// included, so it must be a generated forwarding type and never a consumer's class.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
		System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods |
		System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)]
	public Type Type { get; }
}
#endif

