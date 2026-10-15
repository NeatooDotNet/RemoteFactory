using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Static registry for DTO constructor lambdas emitted by the source generator.
/// Replaces Activator.CreateInstance for IL trimming support: the generator emits
/// <c>DtoConstructorRegistry.Register&lt;Dto&gt;(() => new Dto())</c> calls that
/// create static references the trimmer preserves.
/// </summary>
public static class DtoConstructorRegistry
{
	private static readonly ConcurrentDictionary<Type, Func<object>> Constructors = new();

	/// <summary>
	/// Registers a constructor lambda for a DTO type. Called from generated
	/// FactoryServiceRegistrar methods during application startup.
	/// Uses TryAdd so duplicate registrations from multiple factories are idempotent.
	/// </summary>
	public static void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Func<object> factory)
	{
		Constructors.TryAdd(typeof(T), factory);
	}

	/// <summary>
	/// Attempts to retrieve a registered constructor lambda for the given type.
	/// Used by NeatooJsonTypeInfoResolver to set CreateObject without reflection.
	/// </summary>
	public static bool TryCreate(Type type, out Func<object>? factory)
	{
		return Constructors.TryGetValue(type, out factory);
	}

	/// <summary>
	/// Declares a type as preserved-from-trimming without registering a constructor factory.
	/// Use for record-shaped DTOs without a public parameterless constructor — deserialization
	/// flows through RecordBypassConverterFactory rather than DefaultJsonTypeInfoResolver.CreateObject.
	/// The [DynamicallyAccessedMembers(All)] attribute on T instructs the trimmer to preserve
	/// every constructor, property, and field on T.
	/// </summary>
	public static void PreserveType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
	{
		_ = typeof(T);
	}

	/// <summary>
	/// Preserves the public properties and fields of a [Factory] class that has no generated
	/// ordinal converter, so their accessors survive IL trimming and the class still
	/// serializes. Called from generated FactoryServiceRegistrar methods.
	/// </summary>
	/// <remarks>
	/// The ordinal converter is what references a factory class's accessors on a trimmed
	/// client; a class without one (a constructor that takes services, not partial, or nested)
	/// otherwise loses every accessor nothing on the client calls, IsNew and IsDeleted included.
	/// Only public properties and fields are kept: All would also keep every method body,
	/// [Remote] bodies included, which the IsServerRuntime guards exist to remove.
	/// </remarks>
	public static void PreserveProperties<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>()
	{
		_ = typeof(T);
	}
}
