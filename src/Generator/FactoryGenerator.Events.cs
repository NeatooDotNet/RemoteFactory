// FactoryGenerator.Events.cs
// Discovery of concrete FactoryEventBase descendants for IL-trimming preservation
// (TRIM-007). Descendants carry no attribute of their own ([FactoryEvent] lives on
// the base and Roslyn symbols do not surface inherited attributes), so discovery is
// a CreateSyntaxProvider scan over record declarations with a base list.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.RemoteFactory.Generator;

namespace Neatoo;

/// <summary>
/// Value-equatable discovery result for one concrete <c>FactoryEventBase</c>
/// descendant: the assembly name (for the per-assembly registrar's namespace and
/// hint) and the two preservation buckets produced by walking the event root and
/// its property graph with the shared bucketed walk.
/// </summary>
internal sealed record FactoryEventInfo
{
	public FactoryEventInfo(string assemblyName, EquatableArray<string> registerTypes, EquatableArray<string> preserveTypes)
	{
		this.AssemblyName = assemblyName;
		this.RegisterTypes = registerTypes;
		this.PreserveTypes = preserveTypes;
	}

	public string AssemblyName { get; }
	public EquatableArray<string> RegisterTypes { get; }
	public EquatableArray<string> PreserveTypes { get; }
}

public partial class Factory
{
	internal static FactoryEventInfo? TransformFactoryEvent(RecordDeclarationSyntax recordSyntax, SemanticModel semanticModel)
	{
		if (semanticModel.GetDeclaredSymbol(recordSyntax) is not INamedTypeSymbol symbol)
		{
			return null;
		}

		// Base-chain match by fully-qualified name — FactoryEventBase is always a
		// metadata symbol from the referenced Neatoo.RemoteFactory assembly.
		var derivesFromEventBase = false;
		for (var baseType = symbol.BaseType; baseType != null; baseType = baseType.BaseType)
		{
			if (baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Neatoo.RemoteFactory.FactoryEventBase")
			{
				derivesFromEventBase = true;
				break;
			}
		}

		if (!derivesFromEventBase)
		{
			return null;
		}

		// Accessibility gate: the generated per-assembly registrar is a separate
		// file and cannot legally reference private/protected/file-scoped event
		// records (e.g. private nested test events).
		if (!IsAccessibleWithinAssembly(symbol))
		{
			return null;
		}

		var registerTypes = new List<string>();
		var preserveTypes = new List<string>();
		DtoTypeWalker.WalkDtoGraph(symbol, registerTypes, preserveTypes, new HashSet<string>());

		return new FactoryEventInfo(
			semanticModel.Compilation.AssemblyName ?? "NeatooEvents",
			new EquatableArray<string>([.. registerTypes]),
			new EquatableArray<string>([.. preserveTypes]));
	}

	/// <summary>
	/// True when the type (and every containing type) is public or internal —
	/// i.e. referenceable from a generated file in the same assembly.
	/// </summary>
	private static bool IsAccessibleWithinAssembly(INamedTypeSymbol symbol)
	{
		for (var current = symbol; current != null; current = current.ContainingType)
		{
			if (current.IsFileLocal)
			{
				return false;
			}

			if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
			{
				return false;
			}
		}

		return true;
	}
}
