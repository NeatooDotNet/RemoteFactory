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

/// <summary>
/// Marks a factory method as a client-to-server entry point. <c>[Remote]</c> decides where
/// an operation runs, on every operation: with it, the client routes the call to the server
/// and the server-side path is guarded by <c>NeatooRuntime.IsServerRuntime</c>; without it,
/// the method runs on whichever tier resolves the factory.
/// </summary>
/// <remarks>
/// <para>
/// On class factories <c>[Remote]</c> requires <c>internal</c> (NF0105): the generator
/// promotes the member to <c>public</c> on the factory interface, and the guarded body is
/// what a trimmed client drops. Static methods are exempt from NF0105 — a static-factory
/// <c>[Execute]</c> is <c>private static</c> behind a generated public wrapper, and a
/// class-level <c>[Execute]</c> is <c>public static</c> by documented convention — because on
/// those shapes <c>[Remote]</c> alone drives the guard and the body is measured trimmable
/// regardless of visibility.
/// </para>
/// <para>
/// <c>[Execute]</c> obeys <c>[Remote]</c> like every other operation; see
/// <see cref="ExecuteAttribute"/>.
/// </para>
/// </remarks>
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
/// Marks a static method as a request-response operation. On a <c>[Factory]</c> static class
/// the generator emits a delegate type and its DI registration; on a class factory it emits
/// a factory-interface method returning the containing type.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>[Execute]</c> obeys <c>[Remote]</c>, like every other operation.</b> Without
/// <c>[Remote]</c> there is no remote delegate and no endpoint: the local path is unguarded,
/// the method runs on whichever tier resolves it, its <c>[Service]</c> parameters come from
/// that tier's container, and on a trimmed client the body ships and runs there. With
/// <c>[Remote]</c> the client routes to the server and the local path is guarded by
/// <c>NeatooRuntime.IsServerRuntime</c>, so a client published with the feature switch set
/// to <c>false</c> drops the method body, its <c>[Service]</c> dependencies, and their
/// transitive references.
/// </para>
/// <para>
/// <b>Shapes.</b> Static factory: <c>private static</c> with an underscore prefix (the
/// convention used throughout the Design projects) behind a generated public wrapper. Class
/// factory: <c>public static</c> to run where the factory resolves; <c>internal static</c>
/// without <c>[Remote]</c> is server-only like every other internal operation — guarded,
/// trimmable, and carried on the factory interface with the <c>internal</c> modifier rather
/// than promoted. Static methods are exempt from NF0105 (see <see cref="RemoteAttribute"/>).
/// </para>
/// <para>
/// Both halves are measured rather than inferred: <c>RemoteFactory.TrimmingTests</c> asserts
/// a <c>[Remote, Execute]</c> body absent from a publish-trimmed client and a bare
/// <c>[Execute]</c> body present and running there, on both shapes.
/// </para>
/// </remarks>
public sealed class ExecuteAttribute : FactoryOperationAttribute
{
	public ExecuteAttribute() : base(FactoryOperation.Execute) { }
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

