using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;

namespace Neatoo.RemoteFactory.FactoryGenerator;


[Generator(LanguageNames.CSharp)]
public class MapperGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{

		var classesToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName("Neatoo.RemoteFactory.FactoryAttribute",
			predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
			transform: static (ctx, _) =>
			{
				var classDeclaration = (ClassDeclarationSyntax)ctx.TargetNode;
				var semanticModel = ctx.SemanticModel;
				return Transform(classDeclaration, semanticModel);
			});

		context.RegisterSourceOutput(classesToGenerate, static (spc, typeInfo) =>
		{
			if (typeInfo == null)
			{
				return;
			}
			Execute(spc, typeInfo);
		});
	}

	public static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax classDeclarationSyntax
				&& !(classDeclarationSyntax.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "SuppressFactory"))
				&& classDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
				&& classDeclarationSyntax.Members.Any(m => m is MethodDeclarationSyntax methodDeclarationSyntax
					&& (methodDeclarationSyntax.Identifier.Text == "MapTo" || methodDeclarationSyntax.Identifier.Text == "MapFrom")
					&& methodDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword));

	private static MapperInfo? Transform(ClassDeclarationSyntax syntax, SemanticModel semanticModel)
	{
		if (semanticModel.GetDeclaredSymbol(syntax) is INamedTypeSymbol symbol)
		{
			return new MapperInfo(syntax, symbol, semanticModel);
		}

		return null;
	}

	internal record MapperInfo
	{
		public MapperInfo(TypeDeclarationSyntax syntax, INamedTypeSymbol symbol, SemanticModel semanticModel)
		{

			this.Type = new(symbol);
			this.Signature = syntax.ToFullString().Substring(syntax.Modifiers.FullSpan.Start - syntax.FullSpan.Start, syntax.Identifier.FullSpan.End - syntax.Modifiers.FullSpan.Start);
			this.Namespace = FactoryGenerator.FindNamespace(syntax) ?? "MissingNamespace";
			this.SafeHintName = FactoryGenerator.SafeHintName(semanticModel, $"{this.Namespace}.{this.Type.Name}");

			List<string> usingStatements = new();
			FactoryGenerator.UsingStatements(usingStatements, syntax, semanticModel, this.Namespace, []);
			this.UsingStatements = new EquatableArray<string>([.. usingStatements]);

			if (syntax.TypeParameterList != null)
			{
				this.Signature = syntax.ToFullString().Substring(syntax.Modifiers.FullSpan.Start - syntax.FullSpan.Start, syntax.TypeParameterList.FullSpan.End - syntax.Modifiers.FullSpan.Start);
			}

			var methods = syntax.Members.OfType<MethodDeclarationSyntax>()
				.Where(m => m.Modifiers.Any(SyntaxKind.PartialKeyword) && (m.Identifier.Text == "MapTo" || m.Identifier.Text == "MapFrom"))
				.Select(m => new { Syntax = m, Symbol = semanticModel.GetDeclaredSymbol(m) as IMethodSymbol })
				.Where(m => m.Symbol != null)
				.ToList();

			List<MapperMethodInfo> methodInfos = new();

			foreach (var method in methods)
			{
				var parameterSymbol = method.Symbol!.Parameters.SingleOrDefault();

				if (parameterSymbol == null)
				{
					continue;
				}

				methodInfos.Add(new MapperMethodInfo(method.Syntax, parameterSymbol.Type));
			}

			this.MethodInfos = new EquatableArray<MapperMethodInfo>([.. methodInfos]);
		}

		public MapperTypeInfo Type { get; }
		public string Signature { get; }
		public string Namespace { get; }
		public string SafeHintName { get; }
		public EquatableArray<string> UsingStatements { get; } = [];
		public EquatableArray<MapperMethodInfo> MethodInfos { get; } = [];
	}


	internal record MapperTypeInfo
	{
		public MapperTypeInfo(ITypeSymbol symbol)
		{
			this.Properties = new EquatableArray<PropertyInfo>([.. GetPropertiesRecursive(symbol).Select(p => new PropertyInfo(p))]);
			this.Name = symbol.Name;
		}
		public string Name { get; }
		public EquatableArray<PropertyInfo> Properties { get; set; } = [];

		private static List<IPropertySymbol> GetPropertiesRecursive(ITypeSymbol? typeSymbol)
		{
			var properties = typeSymbol?.GetMembers().OfType<IPropertySymbol>().ToList() ?? [];
			if (typeSymbol?.BaseType != null)
			{
				properties.AddRange(GetPropertiesRecursive(typeSymbol.BaseType));
			}
			return properties;
		}
	}

	internal sealed record MapperMethodInfo
	{
		public MapperMethodInfo(MethodDeclarationSyntax syntax, ITypeSymbol toFromType)
		{
			this.ToFromType = new MapperTypeInfo(toFromType);
			this.Signature = syntax.ToString().TrimEnd(';');
			this.MapTo = syntax.Identifier.Text == "MapTo";
			this.MapFrom = syntax.Identifier.Text == "MapFrom";
			this.ParameterIdentifier = syntax.ParameterList.Parameters.Single().Identifier.Text;
		}

		public MapperTypeInfo ToFromType { get; }
		public string ParameterIdentifier { get; }
		public string Signature { get; }
		public bool MapTo { get; }
		public bool MapFrom { get; }
	}


	internal sealed record PropertyInfo
	{
		public PropertyInfo(IPropertySymbol propertySymbol)
		{
			this.Name = propertySymbol.Name;
			this.Type = propertySymbol.Type.ToDisplayString().TrimEnd('?');
			this.IsNullable = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;
			this.IsReadOnly = propertySymbol.IsReadOnly;
			this.HasMapperIgnore = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "MapperIgnoreAttribute");
		}

		public string Name { get; }
		public string Type { get; }
		public bool IsNullable { get; }
		public bool IsReadOnly { get; }
		public bool HasMapperIgnore { get; }
	}


	private static void Execute(SourceProductionContext context, MapperInfo inType)
	{
		List<string> messages = [];

		var mapperMethods = new StringBuilder();
		try
		{
			foreach (var method in inType.MethodInfos)
			{
				var methodBuilder = new StringBuilder();

				methodBuilder.AppendLine(method.Signature);
				methodBuilder.AppendLine("{");

				var propertiesMatched = false;

				foreach (var toFromProperty in method.ToFromType.Properties)
				{
					var inTypeProperty = inType.Type.Properties.FirstOrDefault(p => p.Name == toFromProperty.Name);

					if (inTypeProperty != null)
					{
						if (inTypeProperty.HasMapperIgnore)
						{
							messages.Add($"Property {inTypeProperty.Name} ignored has MapperIgnore attribute");
							continue;
						}

						propertiesMatched = true;

						var typesMatch = toFromProperty.Type == inTypeProperty.Type;

						if (!typesMatch)
						{
							messages.Add($"Warning: Property {toFromProperty.Name}'s type of {toFromProperty.Type} does not match {inTypeProperty.Type}");
						}

						var nullException = string.Empty;
						var typeCast = string.Empty;

						if (method.MapTo)
						{
							if (inTypeProperty.IsNullable && !toFromProperty.IsNullable)
							{
								nullException = $"?? throw new NullReferenceException(\"{inType.Type.Name}.{inTypeProperty.Name}\")";
							}

							if (!typesMatch)
							{
								typeCast = $"({toFromProperty.Type}{(nullException.Length > 0 ? "?" : "")}) ";
							}

							methodBuilder.AppendLine($"{method.ParameterIdentifier}.{toFromProperty.Name} = {typeCast} this.{inTypeProperty.Name}{nullException};");
						}
						else if (method.MapFrom)
						{
							if (!inTypeProperty.IsNullable && toFromProperty.IsNullable)
							{
								nullException = $"?? throw new NullReferenceException(\"{method.ToFromType.Name}.{toFromProperty.Name}\")";
							}

							if (!typesMatch)
							{
								typeCast = $"({inTypeProperty.Type}{(inTypeProperty.IsNullable ? "?" : "")}) ";
							}

							methodBuilder.Append($"this.{inTypeProperty.Name} = {typeCast} {method.ParameterIdentifier}.{toFromProperty.Name}{nullException};");
						}
					}
				}

				methodBuilder.AppendLine("}");

				if (propertiesMatched)
				{
					mapperMethods.Append(methodBuilder);
				}

			}


			if (mapperMethods.Length > 0)
			{
				var source = $@"
									  #nullable enable

			                    using Neatoo.RemoteFactory.Internal;
			{FactoryGenerator.WithStringBuilder(inType.UsingStatements)}
			namespace {inType.Namespace};

			/*
			READONLY CODE DO NOT MODIFY!!
			This code is generated by the Neatoo.RemoteFactory.MapperGenerator.
			*/

			{inType.Signature}
			{{
			{mapperMethods}
			}}


			";
				source = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();

				context.AddSource($"{inType.SafeHintName}Mapper.g.cs", source);
			}
		}

		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NT0001", "Error", ex.Message, "MapperGenerator", DiagnosticSeverity.Error, true), Location.None));
		}
	}


}
