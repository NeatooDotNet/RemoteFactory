// DtoTypeWalker.cs
// Shared walker for discovering DTO types reachable from a root symbol.
// Used by both the factory-return path (MethodInfo.DiscoverDtoTypes) and the
// [FactoryEventHandler<T>] event-type preservation path (FactoryGenerator.RelayHandler).

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Neatoo.RemoteFactory.Generator;

internal static class DtoTypeWalker
{
	/// <summary>
	/// Unwraps a type symbol by stripping Task, nullable, and collection wrappers.
	/// Returns the inner candidate type(s) for DTO eligibility checking.
	/// </summary>
	public static List<ITypeSymbol> UnwrapType(ITypeSymbol type, bool unwrapTask)
	{
		var currentType = type;

		// Unwrap Task<T>
		if (unwrapTask && currentType is INamedTypeSymbol taskType && taskType.Name == "Task" && taskType.IsGenericType)
		{
			currentType = taskType.TypeArguments[0];
		}

		// Strip nullable annotation (e.g. T? -> T)
		if (currentType is INamedTypeSymbol nullableNamed && nullableNamed.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			currentType = nullableNamed.TypeArguments[0];
		}
		else if (currentType.NullableAnnotation == NullableAnnotation.Annotated && currentType is INamedTypeSymbol annotatedType)
		{
			currentType = annotatedType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
		}

		// Check if it's a generic collection (implements IEnumerable<T>) and unwrap
		bool isCollection = false;
		var candidates = new List<ITypeSymbol>();

		if (currentType is INamedTypeSymbol collectionType && collectionType.IsGenericType)
		{
			foreach (var iface in collectionType.AllInterfaces)
			{
				if (iface.Name == "IEnumerable" && iface.IsGenericType && iface.TypeArguments.Length == 1)
				{
					candidates.Add(iface.TypeArguments[0]);
					isCollection = true;
					break;
				}
			}

			if (!isCollection && collectionType.Name == "IEnumerable" && collectionType.TypeArguments.Length == 1)
			{
				candidates.Add(collectionType.TypeArguments[0]);
				isCollection = true;
			}
		}

		if (!isCollection && currentType is IArrayTypeSymbol arrayType)
		{
			candidates.Add(arrayType.ElementType);
			isCollection = true;
		}

		if (!isCollection)
		{
			candidates.Add(currentType);
		}

		for (int i = 0; i < candidates.Count; i++)
		{
			if (candidates[i].NullableAnnotation == NullableAnnotation.Annotated && candidates[i] is INamedTypeSymbol annotatedCandidate)
			{
				candidates[i] = annotatedCandidate.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
			}
		}

		return candidates;
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

		var ns = namedType.ContainingNamespace?.ToDisplayString() ?? "";
		if (ns.StartsWith("System"))
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
	/// Factory-return walker: recursively discovers DTO types reachable from the given root.
	/// Only accepts types that pass BOTH IsDtoStructureCandidate and HasParameterlessCtor.
	/// Walks public instance properties (including inherited) to find nested DTOs.
	/// Uses visited for cycle suppression; appends FQNs to dtoTypes.
	/// </summary>
	public static void WalkFactoryReturn(ITypeSymbol typeSymbol, List<string> dtoTypes, HashSet<string> visited)
	{
		if (!(typeSymbol is INamedTypeSymbol namedType))
		{
			return;
		}

		if (!IsDtoStructureCandidate(namedType) || !HasParameterlessCtor(namedType))
		{
			return;
		}

		var fullyQualifiedName = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		if (!visited.Add(fullyQualifiedName))
		{
			return;
		}

		dtoTypes.Add(fullyQualifiedName);

		WalkProperties(namedType, WalkFactoryReturnNested);

		void WalkFactoryReturnNested(ITypeSymbol nested) => WalkFactoryReturn(nested, dtoTypes, visited);
	}

	/// <summary>
	/// Event-root walker: the root type itself is ALWAYS added to parameterizedTypes
	/// (PreserveType&lt;T&gt;() bucket), regardless of whether it has a parameterless ctor —
	/// event records deserialize through RecordBypassConverterFactory.
	/// Nested properties bucket-sort by HasParameterlessCtor:
	///   - parameterless → parameterlessCtorTypes (Register&lt;N&gt;(() => new N()))
	///   - parameterized → parameterizedTypes (PreserveType&lt;N&gt;())
	/// Both buckets share the visited set for dedupe.
	/// </summary>
	public static void WalkEventRoot(
		ITypeSymbol eventRoot,
		List<string> parameterlessCtorTypes,
		List<string> parameterizedTypes,
		HashSet<string> visited)
	{
		if (!(eventRoot is INamedTypeSymbol namedRoot))
		{
			return;
		}

		if (!IsDtoStructureCandidate(namedRoot))
		{
			return;
		}

		var rootFqn = namedRoot.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		if (!visited.Add(rootFqn))
		{
			return;
		}

		// The root event type always goes to the PreserveType bucket, regardless of ctor shape.
		parameterizedTypes.Add(rootFqn);

		WalkProperties(namedRoot, WalkNested);

		void WalkNested(ITypeSymbol nested)
		{
			if (!(nested is INamedTypeSymbol namedNested))
			{
				return;
			}

			if (!IsDtoStructureCandidate(namedNested))
			{
				return;
			}

			var fqn = namedNested.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

			if (!visited.Add(fqn))
			{
				return;
			}

			if (HasParameterlessCtor(namedNested))
			{
				parameterlessCtorTypes.Add(fqn);
			}
			else
			{
				parameterizedTypes.Add(fqn);
			}

			WalkProperties(namedNested, WalkNested);
		}
	}

	/// <summary>
	/// Walks public instance properties (including inherited) and invokes the callback
	/// for each unwrapped candidate type.
	/// </summary>
	private static void WalkProperties(INamedTypeSymbol namedType, System.Action<ITypeSymbol> onCandidate)
	{
		var current = namedType;
		while (current != null && current.SpecialType != SpecialType.System_Object)
		{
			foreach (var member in current.GetMembers())
			{
				if (member is IPropertySymbol property &&
					property.DeclaredAccessibility == Accessibility.Public &&
					!property.IsStatic &&
					!property.IsIndexer &&
					property.GetMethod != null)
				{
					var candidates = UnwrapType(property.Type, unwrapTask: false);
					foreach (var candidate in candidates)
					{
						onCandidate(candidate);
					}
				}
			}

			current = current.BaseType;
		}
	}
}
