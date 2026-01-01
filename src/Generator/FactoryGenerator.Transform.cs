// FactoryGenerator.Transform.cs
// Contains methods that transform Roslyn syntax trees and semantic models into
// the data model types (TypeInfo, TypeFactoryMethodInfo, etc.).
// These methods are called during the IncrementalGenerator's transform phase.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo;

public partial class Factory
{
	/// <summary>
	/// Checks if a class or any of its base classes has the specified attribute.
	/// </summary>
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

	/// <summary>
	/// Transforms a class or record declaration with [Factory] attribute into a TypeInfo model.
	/// Handles both ClassDeclarationSyntax and RecordDeclarationSyntax.
	/// </summary>
	private static TypeInfo TransformTypeFactory(TypeDeclarationSyntax syntax, SemanticModel semanticModel)
	{
		var symbol = semanticModel.GetDeclaredSymbol(syntax) ?? throw new Exception($"Cannot get named symbol for {syntax}");

		return new TypeInfo(syntax, symbol, semanticModel);
	}

	/// <summary>
	/// Transforms an interface declaration with [Factory] attribute into a TypeInfo model.
	/// </summary>
	private static TypeInfo TransformInterfaceFactory(InterfaceDeclarationSyntax interfaceSyntax, SemanticModel semanticModel)
	{
		return new TypeInfo(interfaceSyntax, semanticModel.GetDeclaredSymbol(interfaceSyntax) ?? throw new Exception($"Cannot get named symbol for {interfaceSyntax}"), semanticModel);
	}

	/// <summary>
	/// Gets all methods from a type symbol and its base types recursively.
	/// </summary>
	/// <param name="symbol">The type symbol to get methods from.</param>
	/// <param name="includeConst">Whether to include constructors (only included for the top-level type).</param>
	private static List<IMethodSymbol> GetMethodsRecursive(INamedTypeSymbol? symbol, bool includeConst = true)
	{
		var methods = symbol?.GetMembers().OfType<IMethodSymbol>()
						.Where(m => includeConst || m.MethodKind != MethodKind.Constructor) // Only include top-level constructors
						.ToList() ?? [];
		if (symbol?.BaseType != null)
		{
			methods.AddRange(GetMethodsRecursive(symbol.BaseType, false));
		}
		return methods;
	}

	/// <summary>
	/// Extracts factory method information from a type's methods.
	/// Identifies methods with factory operation attributes (Create, Fetch, Insert, Update, Delete, Execute)
	/// and validates their signatures.
	/// </summary>
	private static List<TypeFactoryMethodInfo> TypeFactoryMethods(INamedTypeSymbol serviceSymbol, List<IMethodSymbol> methods, List<FactoryOperation> defaultFactoryOperations, List<TypeAuthMethodInfo> authMethods, List<string> messages, List<DiagnosticInfo> diagnostics)
	{
		var callFactoryMethods = new List<TypeFactoryMethodInfo>();

		foreach (var methodSymbol in methods)
		{
			var methodType = methodSymbol.ReturnType.ToDisplayString();

			if (methodType.Contains(@"Task<"))
			{
				methodType = Regex.Match(methodType, @"Task<(.*?)>").Groups[1].Value;
			}

			if (methodType.EndsWith("?"))
			{
				methodType = methodType.Substring(0, methodType.Length - 1);
			}

			if (methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not BaseMethodDeclarationSyntax methodSyntax)
			{
				messages.Add($"No BaseMethodDeclarationSyntax for {methodSymbol.Name}");
				continue;
			}

			TypeFactoryMethodInfo factoryMethod;

			var attributes = methodSymbol.GetAttributes().ToList();
			var attributeNames = methodSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).ToList();

			attributeNames.AddRange(defaultFactoryOperations.Select(o => o.ToString()));

			var aspAuthorizeAttributes = attributes.Where(a => a.AttributeClass?.Name == "AspAuthorizeAttribute").ToList();
			List<AspAuthorizeInfo> aspAuthorizeCalls = [];

			foreach (var aspAuthorizeAttribute in aspAuthorizeAttributes)
			{
				aspAuthorizeCalls.Add(new AspAuthorizeInfo(aspAuthorizeAttribute));
				attributes.Remove(aspAuthorizeAttribute);
			}

			// Track if this method has any factory operation attribute for NF0301
			bool hasFactoryOperationAttribute = false;

			foreach (var attributeName in attributeNames.Where(a => a != null))
			{
				if (Enum.TryParse<FactoryOperation>(attributeName, out var factoryOperation))
				{
					hasFactoryOperationAttribute = true;
					if (methodSymbol.ReturnType.ToDisplayString().Contains(serviceSymbol.Name))
					{
						if (methodType == serviceSymbol.ToDisplayString())
						{
							if (((int?)factoryOperation & (int)AuthorizeFactoryOperation.Read) == 0)
							{
								// NF0204: Write operation should not return target type
								var methodLocation = methodSyntax switch
								{
									MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
									ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
									_ => methodSyntax.GetLocation()
								};
								var lineSpan = methodLocation.GetLineSpan();
								diagnostics.Add(new DiagnosticInfo(
									"NF0204",
									lineSpan.Path ?? "",
									lineSpan.StartLinePosition.Line,
									lineSpan.StartLinePosition.Character,
									lineSpan.EndLinePosition.Line,
									lineSpan.EndLinePosition.Character,
									methodLocation.SourceSpan.Start,
									methodLocation.SourceSpan.Length,
									methodSymbol.Name,
									factoryOperation.ToString(),
									serviceSymbol.Name));
								messages.Add($"Ignoring {methodSymbol.Name}, Only Fetch and Create methods can return the target type");
								continue;
							}

							if (!methodSymbol.IsStatic)
							{
								// NF0201: Factory method returning target type must be static
								var methodLocation = methodSyntax switch
								{
									MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
									ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
									_ => methodSyntax.GetLocation()
								};
								var lineSpan = methodLocation.GetLineSpan();
								diagnostics.Add(new DiagnosticInfo(
									"NF0201",
									lineSpan.Path ?? "",
									lineSpan.StartLinePosition.Line,
									lineSpan.StartLinePosition.Character,
									lineSpan.EndLinePosition.Line,
									lineSpan.EndLinePosition.Character,
									methodLocation.SourceSpan.Start,
									methodLocation.SourceSpan.Length,
									methodSymbol.Name,
									serviceSymbol.Name));
								messages.Add($"Ignoring {methodSymbol.Name}; it must be static. Only static factories are allowed.");
								continue;
							}
						}
					}
					else if (factoryOperation == FactoryOperation.Execute
								&& serviceSymbol.TypeKind != TypeKind.Interface)
					{
						if (!methodSymbol.IsStatic || !serviceSymbol.IsStatic)
						{
							// NF0103: Execute method must be in a static class
							var methodLocation = methodSyntax switch
							{
								MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
								ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
								_ => methodSyntax.GetLocation()
							};
							var lineSpan = methodLocation.GetLineSpan();
							diagnostics.Add(new DiagnosticInfo(
								"NF0103",
								lineSpan.Path ?? "",
								lineSpan.StartLinePosition.Line,
								lineSpan.StartLinePosition.Character,
								lineSpan.EndLinePosition.Line,
								lineSpan.EndLinePosition.Character,
								methodLocation.SourceSpan.Start,
								methodLocation.SourceSpan.Length,
								methodSymbol.Name));
							messages.Add($"Ignoring {methodSymbol.Name}. Execute Operations must be a static method in a static class");
							continue;
						}
					}

					factoryMethod = new TypeFactoryMethodInfo(factoryOperation, methodSymbol, methodSyntax, authMethods);
				}
				else
				{
					messages.Add($"Ignoring [{methodSymbol.Name}] method with attribute [{attributeName}]. Not a FactoryOperation attribute.");
					continue;
				}

				foreach (var targetParam in methodSymbol.Parameters.Where(p => p.Type == serviceSymbol))
				{
					factoryMethod.Parameters.Where(p => p.Name == targetParam.Name).ToList().ForEach(p => p.IsTarget = true);
				}

				factoryMethod.AspAuthorizeCalls = new EquatableArray<AspAuthorizeInfo>([.. aspAuthorizeCalls]);

				callFactoryMethods.Add(factoryMethod);
			}

			// NF0301: Report for public methods without factory operation attributes (opt-in)
			// Only report for:
			// - Public methods (skip private/internal helpers)
			// - Instance methods or static methods that look like they could be factory methods
			// - Not constructors (they require explicit [Create] attribute)
			// - Not compiler-generated (like property getters/setters)
			if (!hasFactoryOperationAttribute
				&& methodSymbol.DeclaredAccessibility == Accessibility.Public
				&& methodSymbol.MethodKind == MethodKind.Ordinary
				&& !methodSymbol.IsImplicitlyDeclared)
			{
				var methodLocation = methodSyntax switch
				{
					MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
					ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
					_ => methodSyntax.GetLocation()
				};
				var lineSpan = methodLocation.GetLineSpan();
				diagnostics.Add(new DiagnosticInfo(
					"NF0301",
					lineSpan.Path ?? "",
					lineSpan.StartLinePosition.Line,
					lineSpan.StartLinePosition.Character,
					lineSpan.EndLinePosition.Line,
					lineSpan.EndLinePosition.Character,
					methodLocation.SourceSpan.Start,
					methodLocation.SourceSpan.Length,
					methodSymbol.Name,
					serviceSymbol.Name));
			}
		}
		return callFactoryMethods;
	}

	/// <summary>
	/// Extracts authorization method information from a type that has [AuthorizeFactory] attribute.
	/// </summary>
	private static EquatableArray<TypeAuthMethodInfo> TypeAuthMethods(SemanticModel semanticModel, INamedTypeSymbol typeSymbol, List<string> messages, List<DiagnosticInfo> diagnostics)
	{

		var authorizeAttribute = ClassOrBaseClassHasAttribute(typeSymbol, "AuthorizeFactoryAttribute");
		var callAuthMethods = new List<TypeAuthMethodInfo>();

		if (authorizeAttribute != null)
		{
			var authorizationRuleType = authorizeAttribute.AttributeClass?.TypeArguments[0];

			if (authorizationRuleType?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is TypeDeclarationSyntax syntax)
			{
				var authSemanticModel = semanticModel.Compilation.GetSemanticModel(syntax.SyntaxTree);
				var authSymbol = authSemanticModel.GetDeclaredSymbol(syntax);

				var methodSymbols = GetMethodsRecursive(authSymbol);

				foreach (var methodSymbol in methodSymbols)
				{
					var methodType = methodSymbol.ReturnType.ToDisplayString();

					if (methodType.Contains(@"Task<"))
					{
						methodType = Regex.Match(methodType, @"Task<(.*?)>").Groups[1].Value;
					}

					if (methodType.EndsWith("?"))
					{
						methodType = methodType.Substring(0, methodType.Length - 1);
					}

					var attributes = methodSymbol.GetAttributes().ToList();

					if (methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax methodSyntax)
					{
						messages.Add($"No MethodDeclarationSyntax for {methodSymbol.Name}");
						continue;
					}

					foreach (var attribute in attributes)
					{
						var attributeName = attribute.AttributeClass?.Name.Replace("Attribute", "");

						if (attributeName == "AuthorizeFactory")
						{
							if (attribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax)
							{
								messages.Add($"No AttributeSyntax for {methodSymbol.Name} {attribute.ToString()}");
								continue;
							}

							var attr = attributeSyntax.ArgumentList?.Arguments.ToFullString();

							var pattern = @"AuthorizeFactoryOperation\.(\w+)";

							// Use Regex.Matches to find all matches in the attr string
							var matches = Regex.Matches(attr, pattern);
							var authorizeOperationList = new List<AuthorizeFactoryOperation>();

							foreach (Match match in matches)
							{
								// Extract the matched value (e.g., "Read", "Write")
								var value = match.Groups[1].Value;

								// Try to parse the value into the AuthorizeFactoryOperation enum
								if (Enum.TryParse<AuthorizeFactoryOperation>(value, out var dmType))
								{
									// Successfully parsed the value into the AuthorizeFactoryOperation enum
									authorizeOperationList.Add(dmType);
								}
							}

							var authorizeFactoryOperation = authorizeOperationList.Aggregate((a, b) => a | b);

							if (!(methodType == "bool" || methodType == "string" || methodType == "string?"))
							{
								// NF0202: Authorization method has invalid return type
								var methodLocation = methodSyntax.Identifier.GetLocation();
								var lineSpan = methodLocation.GetLineSpan();
								diagnostics.Add(new DiagnosticInfo(
									"NF0202",
									lineSpan.Path ?? "",
									lineSpan.StartLinePosition.Line,
									lineSpan.StartLinePosition.Character,
									lineSpan.EndLinePosition.Line,
									lineSpan.EndLinePosition.Character,
									methodLocation.SourceSpan.Start,
									methodLocation.SourceSpan.Length,
									methodSymbol.Name,
									methodSymbol.ReturnType.ToDisplayString()));
								messages.Add($"Ignoring {methodSymbol.Name}; wrong return type of {methodType} for an AuthorizeFactory method");
								continue;
							}

							callAuthMethods.Add(new TypeAuthMethodInfo(authorizeFactoryOperation, methodSymbol, methodSyntax));
						}
						else
						{
							messages.Add($"Ignoring [{methodSymbol.Name}] method with attribute [{attributeName}]. Not a AuthorizeFactoryOperation attribute.");
							continue;
						}
					}
				}
			}
			else
			{
				messages.Add($"No TypeDeclarationSyntax for {authorizeAttribute}");
			}
		}
		else
		{
			messages.Add("No AuthorizeFactoryAttribute");
		}

		return new EquatableArray<TypeAuthMethodInfo>([.. callAuthMethods]);
	}

	/// <summary>
	/// Collects using statements from a type declaration and its base types.
	/// </summary>
	public static void UsingStatements(List<string> usingDirectives, TypeDeclarationSyntax syntax, SemanticModel semanticModel, string namespaceName, List<string> messages)
	{
		var parentSyntax = syntax.Parent as TypeDeclarationSyntax;
		var parentClassUsingText = "";

		while (parentSyntax != null)
		{
			messages.Add("Parent class: " + parentSyntax.Identifier.Text);
			parentClassUsingText = $"{parentSyntax.Identifier.Text}.{parentClassUsingText}";
			parentSyntax = parentSyntax.Parent as TypeDeclarationSyntax;
		}

		if (!string.IsNullOrEmpty(parentClassUsingText))
		{
			usingDirectives.Add($"using static {namespaceName}.{parentClassUsingText.TrimEnd('.')};");
		}

		var recurseClassDeclaration = syntax;

		while (recurseClassDeclaration != null)
		{
			var compilationUnitSyntax = recurseClassDeclaration.SyntaxTree.GetCompilationUnitRoot();
			foreach (var using_ in compilationUnitSyntax.Usings)
			{
				if (!usingDirectives.Contains(using_.ToString()))
				{
					usingDirectives.Add(using_.ToString());
				}
			}
			recurseClassDeclaration = GetBaseTypeDeclarationSyntax(semanticModel, recurseClassDeclaration, messages);
		}
	}

	/// <summary>
	/// Gets the base type's declaration syntax for a given type declaration.
	/// Used to traverse the inheritance hierarchy when collecting using statements.
	/// </summary>
	private static TypeDeclarationSyntax? GetBaseTypeDeclarationSyntax(SemanticModel semanticModel, TypeDeclarationSyntax syntax, List<string> messages)
	{
		try
		{
			var correctSemanticModel = semanticModel.Compilation.GetSemanticModel(syntax.SyntaxTree);

			var classSymbol = correctSemanticModel.GetDeclaredSymbol(syntax) as INamedTypeSymbol;

			if (classSymbol?.BaseType == null)
			{
				return null;
			}

			var baseTypeSymbol = classSymbol.BaseType;
			var baseTypeSyntaxReference = baseTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault();

			if (baseTypeSyntaxReference == null)
			{
				return null;
			}

			var baseTypeSyntaxNode = baseTypeSyntaxReference.GetSyntax() as TypeDeclarationSyntax;

			return baseTypeSyntaxNode;
		}
		catch (Exception ex)
		{
			messages.Add(ex.Message);
			return null;
		}
	}
}
