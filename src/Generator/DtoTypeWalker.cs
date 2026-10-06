// DtoTypeWalker.cs
// Shared walker for discovering DTO types reachable from a root symbol.
// Three callers, all bucketing into the same two registries:
//   - factory signatures (MethodInfo.DiscoverDtoTypes) — return types and
//     non-service parameters
//   - [Factory] entity property graphs (FactoryGenerator.Types.cs, TRIM-002)
//   - FactoryEventBase descendant graphs (FactoryGenerator.Events.cs, TRIM-007)
// Discovered types bucket-sort by
// constructor shape: parameterless -> DtoConstructorRegistry.Register<T>(),
// parameterized-only -> DtoConstructorRegistry.PreserveType<T>().
//
// What the walk reaches mirrors what the reflection serializer constructs on a
// trimmed client (NeatooJsonSerializer, IncludeFields = true):
//   - every public instance member the serializer writes — properties with a
//     getter AND fields (DICT-002; fields were missed before)
//   - through every generic collection wrapper, recursively, including the
//     KeyValuePair<K,V> element of every dictionary, whose K and V are both on the
//     wire (DICT-002; a dictionary's entry types were missed before, because the
//     pair is a System type and the walk rejects System types)
// A type the walk cannot reach gets no preservation and is trimmed, which surfaces
// on the client as a no-constructor deserialization failure.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Neatoo.RemoteFactory.Generator;

internal static class DtoTypeWalker
{
	/// <summary>
	/// Deepest wrapper nesting <see cref="UnwrapType"/> follows. Real wire shapes stay
	/// shallow — <c>Task&lt;Dictionary&lt;string, List&lt;Dto&gt;&gt;&gt;</c> is four levels — so
	/// this never truncates one. It exists for a type whose enumeration expands without
	/// repeating, such as <c>Weird&lt;T&gt; : IEnumerable&lt;Weird&lt;Weird&lt;T&gt;&gt;&gt;</c>,
	/// which the path check alone cannot stop. A stack overflow here would fail the
	/// whole generator and remove every factory from the consumer's build.
	/// </summary>
	private const int MaxUnwrapDepth = 8;

	/// <summary>
	/// Unwraps a type to the candidate type(s) the serializer constructs for it, for DTO
	/// eligibility checking. Strips <c>Task&lt;T&gt;</c> (at the top only, when
	/// <paramref name="unwrapTask"/> is set), nullable, and generic collection wrappers —
	/// recursively, so <c>List&lt;List&lt;T&gt;&gt;</c> and
	/// <c>Dictionary&lt;K, List&lt;V&gt;&gt;</c> reach <c>T</c>, <c>K</c>, and <c>V</c>. A
	/// <c>KeyValuePair&lt;K,V&gt;</c> yields both <c>K</c> and <c>V</c>. Candidates are
	/// returned without nullable annotations and without duplicates.
	/// </summary>
	/// <remarks>
	/// The collection check is gated on <see cref="INamedTypeSymbol.IsGenericType"/>, as it
	/// always has been. A non-generic subclass such as <c>class Dtos : List&lt;Dto&gt;</c> is
	/// returned as a candidate itself, so its own constructor is registered; its element
	/// type is not reached.
	/// </remarks>
	public static List<ITypeSymbol> UnwrapType(ITypeSymbol type, bool unwrapTask)
	{
		var candidates = new List<ITypeSymbol>();
		var onPath = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
		var expanded = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
		ExpandCandidates(type, unwrapTask, depth: 0, candidates, onPath, expanded);
		return candidates;
	}

	/// <summary>
	/// One step of <see cref="UnwrapType"/>. <paramref name="onPath"/> holds the types being
	/// expanded above this call, so a self-referential enumerable is caught;
	/// <paramref name="expanded"/> holds every type already expanded on this walk, so a type
	/// met twice contributes its candidates once.
	/// </summary>
	private static void ExpandCandidates(
		ITypeSymbol type,
		bool unwrapTask,
		int depth,
		List<ITypeSymbol> candidates,
		HashSet<ITypeSymbol> onPath,
		HashSet<ITypeSymbol> expanded)
	{
		// Task<T> wraps a method's return once; it is never nested on the wire.
		if (unwrapTask && type is INamedTypeSymbol taskType && taskType.Name == "Task" && taskType.IsGenericType)
		{
			ExpandCandidates(taskType.TypeArguments[0], unwrapTask: false, depth + 1, candidates, onPath, expanded);
			return;
		}

		var current = StripNullable(type);

		// A self-referential enumerable — Node<T> : IEnumerable<Node<T>> — or a nesting past
		// the cap. Keep the type itself as the candidate: exactly where single-level
		// unwrapping stopped before DICT-002, so registration for these shapes is unchanged.
		if (onPath.Contains(current) || depth > MaxUnwrapDepth)
		{
			AddCandidate(candidates, current);
			return;
		}

		if (!expanded.Add(current))
		{
			return;
		}

		onPath.Add(current);

		var pair = AsKeyValuePair(current);
		if (pair != null)
		{
			ExpandCandidates(pair.TypeArguments[0], unwrapTask: false, depth + 1, candidates, onPath, expanded);
			ExpandCandidates(pair.TypeArguments[1], unwrapTask: false, depth + 1, candidates, onPath, expanded);
		}
		else
		{
			var element = CollectionElementType(current);
			if (element != null)
			{
				ExpandCandidates(element, unwrapTask: false, depth + 1, candidates, onPath, expanded);
			}
			else
			{
				AddCandidate(candidates, current);
			}
		}

		onPath.Remove(current);
	}

	/// <summary>
	/// Strips <c>Nullable&lt;T&gt;</c> to <c>T</c>, and a nullable reference annotation to the
	/// unannotated type.
	/// </summary>
	private static ITypeSymbol StripNullable(ITypeSymbol type)
	{
		if (type is INamedTypeSymbol nullableNamed && nullableNamed.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			return nullableNamed.TypeArguments[0];
		}

		if (type.NullableAnnotation == NullableAnnotation.Annotated && type is INamedTypeSymbol annotated)
		{
			return annotated.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
		}

		return type;
	}

	/// <summary>
	/// The type as a <c>System.Collections.Generic.KeyValuePair&lt;K,V&gt;</c> — the element
	/// type of every dictionary — or null.
	/// </summary>
	private static INamedTypeSymbol? AsKeyValuePair(ITypeSymbol type)
	{
		return type is INamedTypeSymbol named
			&& named.IsGenericType
			&& named.TypeArguments.Length == 2
			&& named.Name == "KeyValuePair"
			&& named.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic"
			? named
			: null;
	}

	/// <summary>
	/// The element type of a generic collection — the first generic <c>IEnumerable&lt;T&gt;</c>
	/// it implements, or <c>IEnumerable&lt;T&gt;</c> itself — or of an array; otherwise null.
	/// </summary>
	private static ITypeSymbol? CollectionElementType(ITypeSymbol type)
	{
		if (type is INamedTypeSymbol named && named.IsGenericType)
		{
			foreach (var iface in named.AllInterfaces)
			{
				if (iface.Name == "IEnumerable" && iface.IsGenericType && iface.TypeArguments.Length == 1)
				{
					return iface.TypeArguments[0];
				}
			}

			if (named.Name == "IEnumerable" && named.TypeArguments.Length == 1)
			{
				return named.TypeArguments[0];
			}
		}

		if (type is IArrayTypeSymbol arrayType)
		{
			return arrayType.ElementType;
		}

		return null;
	}

	private static void AddCandidate(List<ITypeSymbol> candidates, ITypeSymbol type)
	{
		foreach (var existing in candidates)
		{
			if (SymbolEqualityComparer.Default.Equals(existing, type))
			{
				return;
			}
		}

		candidates.Add(type);
	}

	/// <summary>
	/// Structural DTO candidacy checks: not primitive, not System.*, not abstract/interface,
	/// not [Factory]-annotated (directly or via interface). Does NOT require a parameterless ctor.
	/// </summary>
	public static bool IsDtoStructureCandidate(INamedTypeSymbol namedType)
	{
		if (namedType.SpecialType != SpecialType.None)
		{
			return false;
		}

		// Segment match, not prefix match — consumer namespaces like "Systems.Domain"
		// must not be excluded (TRIM-001 code-review callout).
		var ns = namedType.ContainingNamespace?.ToDisplayString() ?? "";
		if (ns == "System" || ns.StartsWith("System."))
		{
			return false;
		}

		if (namedType.IsAbstract || namedType.TypeKind == TypeKind.Interface)
		{
			return false;
		}

		var hasFactoryAttribute = namedType.GetAttributes().Any(a =>
			a.AttributeClass?.Name == "FactoryAttribute" || a.AttributeClass?.Name == "Factory");
		if (hasFactoryAttribute)
		{
			return false;
		}

		var implementsFactoryInterface = namedType.AllInterfaces.Any(i =>
			i.GetAttributes().Any(a =>
				a.AttributeClass?.Name == "FactoryAttribute" || a.AttributeClass?.Name == "Factory"));
		if (implementsFactoryInterface)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Whether the named type has a public parameterless constructor (implicit or explicit).
	/// </summary>
	public static bool HasParameterlessCtor(INamedTypeSymbol namedType)
	{
		return namedType.Constructors.Any(c =>
			c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0);
	}

	/// <summary>
	/// Whether the named type has at least one public constructor with parameters.
	/// </summary>
	public static bool HasParameterizedPublicCtor(INamedTypeSymbol namedType)
	{
		return namedType.Constructors.Any(c =>
			c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length > 0);
	}

	/// <summary>
	/// Factory-signature walker: recursively discovers DTO types reachable from the
	/// given root and bucket-sorts every discovered type (roots and nested alike) by
	/// constructor shape:
	///   - public parameterless ctor → registerTypes (Register&lt;T&gt;(() => new T()))
	///   - only parameterized public ctors (positional records) → preserveTypes
	///     (PreserveType&lt;T&gt;(); deserialization flows through RecordBypassConverterFactory)
	///   - no public ctor at all → skipped (not deserializable)
	/// Walks the serialized members (see <see cref="WalkMembers"/>) of both buckets' types
	/// to find nested DTOs. Both buckets share the visited set for cycle suppression.
	/// </summary>
	public static void WalkDtoGraph(
		ITypeSymbol typeSymbol,
		List<string> registerTypes,
		List<string> preserveTypes,
		HashSet<string> visited)
	{
		if (!(typeSymbol is INamedTypeSymbol namedType))
		{
			return;
		}

		if (!IsDtoStructureCandidate(namedType))
		{
			return;
		}

		var hasParameterless = HasParameterlessCtor(namedType);
		if (!hasParameterless && !HasParameterizedPublicCtor(namedType))
		{
			return;
		}

		var fullyQualifiedName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		if (!visited.Add(fullyQualifiedName))
		{
			return;
		}

		if (hasParameterless)
		{
			registerTypes.Add(fullyQualifiedName);
		}
		else
		{
			preserveTypes.Add(fullyQualifiedName);
		}

		WalkMembers(namedType, WalkNested);

		void WalkNested(ITypeSymbol nested) => WalkDtoGraph(nested, registerTypes, preserveTypes, visited);
	}

	/// <summary>
	/// Entity-rooted walker: walks a [Factory] class type's serialized members (see
	/// <see cref="WalkMembers"/>) and bucket-sorts reachable DTO types via
	/// WalkDtoGraph. The entity root itself is never bucketed — entities are
	/// preserved via DI registration. Factory-typed members are skipped entirely
	/// (no bucket, no descent): each class type carrying [Factory] directly owns its
	/// own graph through its own registrar.
	/// </summary>
	/// <remarks>
	/// An entity's public fields are walked as well as its properties. In the default
	/// <c>Ordinal</c> format an entity serializes through generated code that carries
	/// properties only, but in <c>Named</c> format it goes through the reflection
	/// serializer with <c>IncludeFields</c> on, and the generator cannot see which format
	/// the consumer configures.
	/// </remarks>
	public static void WalkEntityProperties(
		INamedTypeSymbol entityType,
		List<string> registerTypes,
		List<string> preserveTypes,
		HashSet<string> visited)
	{
		WalkMembers(entityType, nested => WalkDtoGraph(nested, registerTypes, preserveTypes, visited));
	}

	/// <summary>
	/// Walks the public instance members the serializer writes, across the base chain, and
	/// invokes the callback for each unwrapped candidate type: properties with a getter,
	/// and fields, because <c>NeatooJsonSerializer</c> runs with <c>IncludeFields = true</c>.
	/// </summary>
	private static void WalkMembers(INamedTypeSymbol namedType, System.Action<ITypeSymbol> onCandidate)
	{
		var current = namedType;
		while (current != null && current.SpecialType != SpecialType.System_Object)
		{
			foreach (var member in current.GetMembers())
			{
				var memberType = SerializedMemberType(member);
				if (memberType == null)
				{
					continue;
				}

				foreach (var candidate in UnwrapType(memberType, unwrapTask: false))
				{
					onCandidate(candidate);
				}
			}

			current = current.BaseType;
		}
	}

	/// <summary>
	/// The type of a member the serializer writes, or null. A public instance property
	/// with a getter, or a public instance field. Indexers, const fields, static members,
	/// non-public members, and compiler-declared fields are never on the wire.
	/// </summary>
	private static ITypeSymbol? SerializedMemberType(ISymbol member)
	{
		if (member.DeclaredAccessibility != Accessibility.Public || member.IsStatic)
		{
			return null;
		}

		switch (member)
		{
			case IPropertySymbol property when !property.IsIndexer && property.GetMethod != null:
				return property.Type;
			case IFieldSymbol field when !field.IsConst && !field.IsImplicitlyDeclared:
				return field.Type;
			default:
				return null;
		}
	}
}
