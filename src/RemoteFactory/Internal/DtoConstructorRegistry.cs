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
}
