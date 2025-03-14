using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Neatoo.RemoteFactory.FactoryGenerator;


[Generator(LanguageNames.CSharp)]
public class MapperGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context) =>
			// Register the source output
			context.RegisterSourceOutput(context.SyntaxProvider.CreateSyntaxProvider(
				predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
				transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
				.Where(static m => m is not null),
				static (ctx, source) => Execute(ctx, source!.Value.classDeclaration, source.Value.semanticModel));

	public static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax classDeclarationSyntax
				&& !(classDeclarationSyntax.TypeParameterList?.Parameters.Any() ?? false || classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword))
				&& !(classDeclarationSyntax.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "SuppressFactory"));

	public static (ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
	{
		try
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;

			var classNamedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

			if (classNamedTypeSymbol == null)
			{
				return null;
			}

			if (ClassOrBaseClassHasAttribute(classNamedTypeSymbol, "SuppressFactory") != null)
			{
				return null;
			}

			if (ClassOrBaseClassHasAttribute(classNamedTypeSymbol, "FactoryAttribute") != null)
			{
				if (classDeclaration != null && classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
				{
					return (classDeclaration, context.SemanticModel);
				}
			}
		}
		catch (Exception)
		{

		}

		return null;
	}

	private static AttributeData? ClassOrBaseClassHasAttribute(INamedTypeSymbol namedTypeSymbol, string attributeName)
	{
		var attribute = namedTypeSymbol.GetAttributes().FirstOrDefault(a => (a.AttributeClass?.Name ?? "") == attributeName);

		if (attribute != null)
		{
			return attribute;
		}
		if (namedTypeSymbol.BaseType != null)
		{
			return ClassOrBaseClassHasAttribute(namedTypeSymbol.BaseType, attributeName);
		}
		return null;
	}

	private static void Execute(SourceProductionContext context, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
	{
		List<string> messages = [];
		List<string> usingDirectives = [];

		var mapperMethods = new StringBuilder();
		try
		{
			var classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
			var className = classDeclarationSyntax.Identifier.Text;
			if (classSymbol == null)
			{
				messages.Add("Class symbol is null");
			}

			var namespaceName = FactoryGenerator.FindNamespace(classDeclarationSyntax) ?? "MissingNamespace";

			FactoryGenerator.UsingStatements(usingDirectives, classDeclarationSyntax, semanticModel, namespaceName, messages);


			var classProperties = GetPropertiesRecursive(classSymbol);

			classProperties.ForEach(p =>
			{
				messages.Add($"Class Property {p.Name} {p.Type} found");
			});

			var classMethods = classSymbol?.GetMembers().OfType<IMethodSymbol>().ToList() ?? [];

			foreach (var classMethod in classMethods)
			{
				var methodBuilder = new StringBuilder();

				if (classMethod.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is MethodDeclarationSyntax classSyntax)
				{
					var mapTo = classSyntax.Identifier.Text == "MapTo";
					var mapFrom = classSyntax.Identifier.Text == "MapFrom";
					if ((mapTo || mapFrom) && classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
					{
						messages.Add($"Method {classMethod.Name} is a Match");

						methodBuilder.AppendLine($"{classSyntax.ToFullString().Trim().TrimEnd(';')}");
						methodBuilder.AppendLine("{");

						var parameterSymbol = classMethod.Parameters.SingleOrDefault();
						if (parameterSymbol == null)
						{
							messages.Add($"Single parameter not found for {classMethod.Name}");
							break;
						}
						else
						{
							messages.Add($"Parameter {parameterSymbol.Name} {parameterSymbol.Type} found for {classMethod.Name}");
						}

						var parameterSyntax = classSyntax.ParameterList.Parameters.First();
						var parameterIdentifier = parameterSyntax.Identifier.Text;

						var parameterProperties = GetPropertiesRecursive(parameterSymbol?.Type as INamedTypeSymbol);
						parameterProperties.ForEach(p =>
						{
							messages.Add($"Parameter Property {p.Name} {p.Type} found");
						});

						var propertiesMatched = false;

						foreach (var parameterProperty in parameterProperties)
						{
							var classProperty = classProperties.FirstOrDefault(p => p.Name == parameterProperty.Name);
							if (classProperty != null)
							{
								propertiesMatched = true;
								if (mapTo)
								{
									methodBuilder.AppendLine($"{parameterIdentifier}.{parameterProperty.Name} = this.{classProperty.Name};");
								}
								else if (mapFrom)
								{
									methodBuilder.Append($"this.{classProperty.Name} = {parameterIdentifier}.{parameterProperty.Name};");
								}
							}
						}

						methodBuilder.AppendLine("}");

						if (propertiesMatched)
						{
							mapperMethods.Append(methodBuilder);
						}
					}
				}
			}

			var classDeclaration = classDeclarationSyntax.GetText().GetSubText(new Microsoft.CodeAnalysis.Text.TextSpan(classDeclarationSyntax.Modifiers.Span.Start - classDeclarationSyntax.SpanStart, classDeclarationSyntax.Identifier.Span.End - classDeclarationSyntax.Modifiers.Span.Start + 2));

			var source = $@"
						  #nullable enable

                    using Neatoo.RemoteFactory.Internal;
{FactoryGenerator.WithStringBuilder(usingDirectives)}
namespace {namespaceName};

/*
{FactoryGenerator.WithStringBuilder(messages)}

*/

{classDeclaration}
{{
{mapperMethods}
}}


";
			source = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();

			context.AddSource($"{namespaceName}.{className}Partial.g.cs", source);

		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NT0001", "Error", ex.Message, "MapperGenerator", DiagnosticSeverity.Error, true), Location.None));
		}
	}

	public static List<IPropertySymbol> GetPropertiesRecursive(INamedTypeSymbol? classNamedSymbol)
	{
		var properties = classNamedSymbol?.GetMembers().OfType<IPropertySymbol>().ToList() ?? [];
		if (classNamedSymbol?.BaseType != null)
		{
			properties.AddRange(GetPropertiesRecursive(classNamedSymbol.BaseType));
		}
		return properties;
	}
}
