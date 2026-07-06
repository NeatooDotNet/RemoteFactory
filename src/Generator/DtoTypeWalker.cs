// DtoTypeWalker.cs
// Shared walker for discovering DTO types reachable from a root symbol.
// Used by the factory-signature path (MethodInfo.DiscoverDtoTypes) for both
// return types and non-service parameters. Discovered types bucket-sort by
// constructor shape: parameterless -> DtoConstructorRegistry.Register<T>(),
// parameterized-only -> DtoConstructorRegistry.PreserveType<T>().

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
	/// Walks public instance properties (including inherited) of both buckets' types
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

		WalkProperties(namedType, WalkNested);

		void WalkNested(ITypeSymbol nested) => WalkDtoGraph(nested, registerTypes, preserveTypes, visited);
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
