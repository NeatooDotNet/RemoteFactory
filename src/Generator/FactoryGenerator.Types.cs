// FactoryGenerator.Types.cs
// Contains all record and class type definitions used by the FactoryGenerator.
// These types model the data extracted from source code during the transform phase
// and are used during the code generation phase.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Neatoo.Factory;
using Neatoo.RemoteFactory.FactoryGenerator;
using Neatoo.RemoteFactory.Generator;

namespace Neatoo;

public partial class Factory
{
	/// <summary>
	/// FullyQualifiedFormat with nullable reference type annotations preserved.
	/// FullyQualifiedFormat alone strips inner nullable annotations (e.g., List&lt;string?&gt; becomes List&lt;string&gt;).
	/// This format adds IncludeNullableReferenceTypeModifier so inner nullable annotations are retained.
	/// Used for property type extraction where nullable generic type arguments must survive round-trip.
	/// </summary>
	private static readonly SymbolDisplayFormat FullyQualifiedFormatWithNullable =
		SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
			SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
			| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

	/// <summary>
	/// List of factory operations that are considered "save" operations (Insert, Update, Delete).
	/// Used to determine if a factory method should be treated as a write operation.
	/// </summary>
	private static List<FactoryOperation> factorySaveOperationAttributes = [.. Enum.GetValues(typeof(FactoryOperation)).Cast<FactoryOperation>().Where(v => ((int)v & (int)AuthorizeFactoryOperation.Write) != 0)];

	/// <summary>
	/// Information about a serializable property for ordinal serialization.
	/// </summary>
	internal record OrdinalPropertyInfo
	{
		public string Name { get; }
		public string Type { get; }
		public bool IsNullable { get; }
		public int InheritanceDepth { get; }

		/// <summary>
		/// True when this property is a LazyLoad&lt;T&gt;. Occupies two ordinal slots.
		/// </summary>
		public bool IsLazyLoad { get; }

		/// <summary>
		/// The fully-qualified inner type T when <see cref="IsLazyLoad"/> is true.
		/// </summary>
		public string? InnerType { get; }

		public OrdinalPropertyInfo(string name, string type, bool isNullable, int inheritanceDepth, bool isLazyLoad = false, string? innerType = null)
		{
			Name = name;
			Type = type;
			IsNullable = isNullable;
			InheritanceDepth = inheritanceDepth;
			IsLazyLoad = isLazyLoad;
			InnerType = innerType;
		}
	}

	/// <summary>
	/// Contains all information about a type that has the [Factory] attribute.
	/// This record is populated during the transform phase and consumed during generation.
	/// </summary>
	internal record TypeInfo
	{
		public TypeInfo(TypeDeclarationSyntax syntax, INamedTypeSymbol symbol, SemanticModel semanticModel)
		{
			List<string> debugMessages = [];
			List<DiagnosticInfo> diagnostics = [];

			var serviceSymbol = symbol;

			this.Name = syntax.Identifier.Text;
			this.IsPartial = syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
			this.SignatureText = syntax.ToFullString().Substring(syntax.Modifiers.FullSpan.Start - syntax.FullSpan.Start, syntax.Identifier.FullSpan.End - syntax.Modifiers.FullSpan.Start).Trim();
			this.IsInterface = syntax is InterfaceDeclarationSyntax;
			this.IsStatic = symbol.IsStatic;
			this.IsNested = symbol.ContainingType != null;

			// Detect record types and primary constructors
			this.IsRecord = syntax is RecordDeclarationSyntax;
			bool isRecordStruct = false;
			ParameterListSyntax? primaryConstructorParameters = null;

			if (syntax is RecordDeclarationSyntax recordSyntax)
			{
				// Detect record struct (record with struct keyword)
				isRecordStruct = recordSyntax.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword);
				// Check for primary constructor parameters
				primaryConstructorParameters = recordSyntax.ParameterList;
				this.HasPrimaryConstructor = primaryConstructorParameters?.Parameters.Count > 0;

				// Collect constructor parameter names for ordinal serialization
				if (this.HasPrimaryConstructor && primaryConstructorParameters != null)
				{
					var parameterNames = new List<string>();
					foreach (var param in primaryConstructorParameters.Parameters)
					{
						// Get the property name (Pascal case) that corresponds to this parameter
						// For records, the compiler auto-generates properties with the same name as parameters
						var paramName = param.Identifier.Text;
						// Record parameters automatically become properties with the same name
						// (first letter is already the correct case as declared)
						parameterNames.Add(paramName);
					}
					this.PrimaryConstructorParameterNames = new EquatableArray<string>([.. parameterNames]);
				}
			}

			// Store class identifier location for diagnostics (NF0101)
			var classLocation = syntax.Identifier.GetLocation();
			var classLineSpan = classLocation.GetLineSpan();
			this.ClassFilePath = classLineSpan.Path ?? "";
			this.ClassStartLine = classLineSpan.StartLinePosition.Line;
			this.ClassStartColumn = classLineSpan.StartLinePosition.Character;
			this.ClassEndLine = classLineSpan.EndLinePosition.Line;
			this.ClassEndColumn = classLineSpan.EndLinePosition.Character;
			this.ClassTextSpanStart = classLocation.SourceSpan.Start;
			this.ClassTextSpanLength = classLocation.SourceSpan.Length;

			// NF0206: record struct is not supported
			if (isRecordStruct)
			{
				diagnostics.Add(new DiagnosticInfo(
					"NF0206",
					this.ClassFilePath,
					this.ClassStartLine,
					this.ClassStartColumn,
					this.ClassEndLine,
					this.ClassEndColumn,
					this.ClassTextSpanStart,
					this.ClassTextSpanLength,
					this.Name));
			}

			if (!this.IsInterface)
			{
				this.ImplementationTypeName = symbol.Name;
				var interfaceSymbol = symbol.Interfaces.FirstOrDefault(i => i.Name == $"I{this.Name}");
				if (interfaceSymbol != null)
				{
					serviceSymbol = interfaceSymbol;
				}
			}
			else
			{
				this.ImplementationTypeName = symbol.Name.Substring(1); // Remove the I prefix
			}

			this.ServiceTypeName = serviceSymbol.Name;

			this.Namespace = FindNamespace(syntax) ?? "MissingNamespace";

			var usingStatements = new List<string>() { "using Neatoo.RemoteFactory;",
																 "using Neatoo.RemoteFactory.Internal;",
																 "using Microsoft.Extensions.DependencyInjection;" };

			UsingStatements(usingStatements, syntax, semanticModel, this.Namespace, debugMessages);
			this.UsingStatements = new EquatableArray<string>([.. usingStatements.Distinct()]);

			var methodSymbols = GetMethodsRecursive(symbol);

			this.AuthMethods = TypeAuthMethods(semanticModel, symbol, serviceSymbol, debugMessages, diagnostics);

			List<FactoryOperation> defaultFactoryOperations = [];

			if (this.IsInterface)
			{
				defaultFactoryOperations.Add(FactoryOperation.Execute);
			}

			// Check for [Create] attribute on the type declaration
			var typeAttributes = syntax.AttributeLists.SelectMany(a => a.Attributes).ToList();
			var createAttributeOnType = typeAttributes.FirstOrDefault(a =>
				a.Name.ToString() == "Create" || a.Name.ToString() == "CreateAttribute");

			List<TypeFactoryMethodInfo> factoryMethodsList = [];

			if (createAttributeOnType != null)
			{
				// [Create] is on the type - this is only valid for records with primary constructors
				if (this.IsRecord && this.HasPrimaryConstructor && primaryConstructorParameters != null)
				{
					// Create a TypeFactoryMethodInfo for the primary constructor
					var primaryConstructor = symbol.Constructors.FirstOrDefault(c =>
						c.Parameters.Length == primaryConstructorParameters.Parameters.Count &&
						!c.IsImplicitlyDeclared);

					if (primaryConstructor != null)
					{
						// Find the constructor's syntax - for records, we need to handle the primary constructor differently
						// The primary constructor's syntax is the record's parameter list
						var constructorSyntax = primaryConstructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

						if (constructorSyntax is RecordDeclarationSyntax recordDeclSyntax)
						{
							// Create a factory method for the primary constructor
							var factoryMethodInfo = CreatePrimaryConstructorFactoryMethod(
								primaryConstructor,
								recordDeclSyntax,
								this.AuthMethods.ToList());
							factoryMethodsList.Add(factoryMethodInfo);
						}
					}
				}
				else
				{
					// NF0205: [Create] on type requires record with primary constructor
					var createAttrLocation = createAttributeOnType.GetLocation();
					var createAttrLineSpan = createAttrLocation.GetLineSpan();
					diagnostics.Add(new DiagnosticInfo(
						"NF0205",
						createAttrLineSpan.Path ?? "",
						createAttrLineSpan.StartLinePosition.Line,
						createAttrLineSpan.StartLinePosition.Character,
						createAttrLineSpan.EndLinePosition.Line,
						createAttrLineSpan.EndLinePosition.Character,
						createAttrLocation.SourceSpan.Start,
						createAttrLocation.SourceSpan.Length,
						this.Name));
				}
			}

			// Add factory methods from explicit methods/constructors
			factoryMethodsList.AddRange(TypeFactoryMethods(serviceSymbol, methodSymbols, defaultFactoryOperations, this.AuthMethods.ToList(), debugMessages, diagnostics));

			this.FactoryMethods = new EquatableArray<TypeFactoryMethodInfo>([.. factoryMethodsList]);

			// Aggregate DTO return types from all factory methods (deduplicated)
			var allDtoTypes = new HashSet<string>();
			foreach (var method in factoryMethodsList)
			{
				foreach (var dtoType in method.DtoReturnTypes)
				{
					allDtoTypes.Add(dtoType);
				}
			}
			this.DtoReturnTypes = new EquatableArray<string>([.. allDtoTypes]);

			// Collect properties for ordinal serialization (only for non-interface, non-static types)
			if (!this.IsInterface && !this.IsStatic)
			{
				this.OrdinalProperties = new EquatableArray<OrdinalPropertyInfo>([.. CollectOrdinalProperties(symbol)]);
			}

			// Check if type requires DI to instantiate (can't use object initializer)
			// Skip this check for records with primary constructors - they use constructor syntax
			if (!this.IsInterface && !this.IsStatic && !(this.IsRecord && this.HasPrimaryConstructor))
			{
				this.RequiresServiceInstantiation = RequiresServiceInstantiationCheck(symbol);
			}

			var hintNameResult = SafeHintName(semanticModel, $"{this.Namespace}.{this.Name}");
			this.SafeHintName = hintNameResult.ResultName;

			// NF0104: Report error if hint name was truncated (collision risk)
			if (hintNameResult.WasTruncated)
			{
				diagnostics.Add(new DiagnosticInfo(
					"NF0104",
					this.ClassFilePath,
					this.ClassStartLine,
					this.ClassStartColumn,
					this.ClassEndLine,
					this.ClassEndColumn,
					this.ClassTextSpanStart,
					this.ClassTextSpanLength,
					this.Name,
					hintNameResult.MaxLength.ToString(),
					hintNameResult.OriginalName,
					hintNameResult.ResultName,
					(hintNameResult.OriginalName.Length + 10).ToString())); // Suggest slightly larger than needed
			}

			this.Diagnostics = new EquatableArray<DiagnosticInfo>([.. diagnostics]);
		}

		/// <summary>
		/// Creates a TypeFactoryMethodInfo for a record's primary constructor.
		/// </summary>
		private static TypeFactoryMethodInfo CreatePrimaryConstructorFactoryMethod(
			IMethodSymbol constructorSymbol,
			RecordDeclarationSyntax recordSyntax,
			List<TypeAuthMethodInfo> authMethods)
		{
			return new TypeFactoryMethodInfo(
				FactoryOperation.Create,
				constructorSymbol,
				recordSyntax,
				authMethods);
		}

		public string Name { get; }
		public bool IsPartial { get; }
		public string SignatureText { get; }
		public bool IsInterface { get; }
		public bool IsStatic { get; }
		public bool IsRecord { get; }
		public bool HasPrimaryConstructor { get; }
		public string ServiceTypeName { get; }
		public string ImplementationTypeName { get; }
		public string Namespace { get; }
		public EquatableArray<string> UsingStatements { get; } = [];
		public EquatableArray<TypeFactoryMethodInfo> FactoryMethods { get; set; } = [];
		public EquatableArray<TypeAuthMethodInfo> AuthMethods { get; set; } = [];

		/// <summary>
		/// Deduplicated fully-qualified names of plain DTO types discovered across all factory methods.
		/// Used by renderers to emit DtoConstructorRegistry.Register calls for IL trimming support.
		/// </summary>
		public EquatableArray<string> DtoReturnTypes { get; } = [];

		/// <summary>
		/// Indicates if this type is nested inside another type.
		/// Nested types require special handling for code generation.
		/// </summary>
		public bool IsNested { get; }

		public string SafeHintName { get; }

		/// <summary>
		/// Properties for ordinal serialization, sorted by inheritance depth then alphabetically.
		/// </summary>
		public EquatableArray<OrdinalPropertyInfo> OrdinalProperties { get; } = [];

		/// <summary>
		/// Primary constructor parameter names in declaration order.
		/// Used for records with primary constructors to generate constructor calls.
		/// </summary>
		public EquatableArray<string> PrimaryConstructorParameterNames { get; } = [];

		/// <summary>
		/// Indicates if this type requires DI to instantiate (has constructors with non-service parameters).
		/// Types that require DI cannot use object initializer syntax for ordinal deserialization.
		/// </summary>
		public bool RequiresServiceInstantiation { get; }

		/// <summary>
		/// Diagnostics collected during the transform phase.
		/// </summary>
		public EquatableArray<DiagnosticInfo> Diagnostics { get; }

		/// <summary>
		/// Collects all public properties from a type and its base types for ordinal serialization.
		/// Properties are sorted by inheritance depth (base first) then alphabetically by name.
		/// </summary>
		private static List<OrdinalPropertyInfo> CollectOrdinalProperties(INamedTypeSymbol symbol)
		{
			var properties = new List<OrdinalPropertyInfo>();
			CollectPropertiesRecursive(symbol, properties, 0);

			// Sort by inheritance depth (base classes first), then alphabetically by name
			return properties
				.OrderBy(p => p.InheritanceDepth)
				.ThenBy(p => p.Name, StringComparer.Ordinal)
				.ToList();
		}

		private static void CollectPropertiesRecursive(INamedTypeSymbol? symbol, List<OrdinalPropertyInfo> properties, int depth)
		{
			if (symbol == null || symbol.SpecialType == SpecialType.System_Object)
			{
				return;
			}

			// First collect from base type (will have higher depth value = processed first)
			if (symbol.BaseType != null && symbol.BaseType.SpecialType != SpecialType.System_Object)
			{
				CollectPropertiesRecursive(symbol.BaseType, properties, depth + 1);
			}

			// Then collect from current type
			foreach (var member in symbol.GetMembers())
			{
				if (member is IPropertySymbol propertySymbol &&
					propertySymbol.DeclaredAccessibility == Accessibility.Public &&
					!propertySymbol.IsStatic &&
					!propertySymbol.IsIndexer &&
					propertySymbol.GetMethod != null && // Must have a getter for serialization
					propertySymbol.SetMethod != null && // Must have a setter or init accessor for deserialization
					IsSetterAccessibleForObjectInitializer(propertySymbol.SetMethod)) // Setter must be accessible
				{
					// Skip if property is already collected from a base type (override)
					if (properties.Any(p => p.Name == propertySymbol.Name))
					{
						continue;
					}

					// A property is nullable if it has the NullableAnnotation.Annotated
					// For example: string? (annotated) vs string (not annotated)
					// int? (annotated) vs int (not annotated)
					var isNullable = propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;

					// Get the type string without nullable annotation.
					// For reference types: WithNullableAnnotation strips the ? annotation
					// For value types: int? is actually Nullable<int>, so ToDisplayString() still returns "int?"
					// We strip trailing whitespace and ? to get the base type for both cases.
					// The final TrimEnd() handles cases where there was whitespace before the ? (e.g., "int ?").
					// The IsNullable flag preserves the nullability information for use during rendering.
					var typeString = propertySymbol.Type
						.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
						.ToDisplayString(FullyQualifiedFormatWithNullable)
						.TrimEnd()
						.TrimEnd('?')
						.TrimEnd();

					// Detect LazyLoad<T> properties for two-slot ordinal encoding.
					// LazyLoad<T> properties occupy two consecutive array slots: Value (inner type T) and IsLoaded (bool).
					var isLazyLoad = false;
					string? innerType = null;
					if (propertySymbol.Type is INamedTypeSymbol namedType
						&& namedType.IsGenericType
						&& namedType.TypeArguments.Length == 1
						&& namedType.ConstructedFrom.ToDisplayString() == "Neatoo.RemoteFactory.LazyLoad<T>")
					{
						isLazyLoad = true;
						innerType = namedType.TypeArguments[0]
							.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
							.ToDisplayString(FullyQualifiedFormatWithNullable)
							.TrimEnd()
							.TrimEnd('?')
							.TrimEnd();
					}

					properties.Add(new OrdinalPropertyInfo(
						propertySymbol.Name,
						typeString,
						isNullable,
						depth,
						isLazyLoad,
						innerType));
				}
			}
		}

		/// <summary>
		/// Checks if a setter is accessible for object initializer syntax.
		/// Object initializers can use public setters, internal setters (within same assembly),
		/// or init-only setters.
		/// </summary>
		private static bool IsSetterAccessibleForObjectInitializer(IMethodSymbol setMethod)
		{
			// Init-only setters are always allowed in object initializers
			if (setMethod.IsInitOnly)
			{
				return true;
			}

			// Public or internal setters are accessible
			return setMethod.DeclaredAccessibility == Accessibility.Public ||
				   setMethod.DeclaredAccessibility == Accessibility.Internal ||
				   setMethod.DeclaredAccessibility == Accessibility.ProtectedOrInternal;
		}

		/// <summary>
		/// Checks if a type requires service (DI) instantiation.
		/// Returns true if the type cannot be instantiated using object initializer syntax.
		/// Object initializer syntax requires a parameterless constructor or a constructor
		/// where all parameters have default values.
		/// Note: [Service] parameters still count as required parameters because
		/// the generated deserialization code cannot provide DI-resolved values.
		/// </summary>
		private static bool RequiresServiceInstantiationCheck(INamedTypeSymbol symbol)
		{
			// Abstract types can't be instantiated directly
			if (symbol.IsAbstract)
			{
				return true;
			}

			var constructors = symbol.Constructors.Where(c => !c.IsStatic && !c.IsImplicitlyDeclared);

			// If there are no explicit constructors, the implicit parameterless constructor is used
			if (!constructors.Any())
			{
				return false;
			}

			// Check if there's at least one constructor that can be called with no arguments
			// (i.e., parameterless or all parameters have default values)
			foreach (var ctor in constructors)
			{
				// Parameterless constructor - can use object initializer
				if (ctor.Parameters.Length == 0)
				{
					return false;
				}

				// Check if all parameters have default values
				var allParamsHaveDefaults = ctor.Parameters.All(p => p.HasExplicitDefaultValue);
				if (allParamsHaveDefaults)
				{
					return false;
				}
			}

			// No constructor can be called without arguments
			return true;
		}

		// Class location info for diagnostics (NF0101)
		public string ClassFilePath { get; }
		public int ClassStartLine { get; }
		public int ClassStartColumn { get; }
		public int ClassEndLine { get; }
		public int ClassEndColumn { get; }
		public int ClassTextSpanStart { get; }
		public int ClassTextSpanLength { get; }
	}

	/// <summary>
	/// Information about a factory method (Create, Fetch, Insert, Update, Delete, Execute).
	/// </summary>
	internal record TypeFactoryMethodInfo : MethodInfo
	{
		public TypeFactoryMethodInfo(FactoryOperation factoryOperation, IMethodSymbol methodSymbol, BaseMethodDeclarationSyntax methodSyntax, IEnumerable<TypeAuthMethodInfo> authMethods) : base(methodSymbol, methodSyntax)
		{
			this.FactoryOperation = factoryOperation;
			this.Name = methodSymbol.Name;
			if (methodSyntax is ConstructorDeclarationSyntax)
			{
				this.IsConstructor = true;
				this.Name = factoryOperation.ToString();
			}
			this.IsSave = factorySaveOperationAttributes.Contains(factoryOperation);
			this.IsStaticFactory = methodSymbol.IsStatic;
			this.IsRemote = this.IsRemote || factoryOperation == FactoryOperation.Execute;

			// Store method location for diagnostics (NF0102)
			var methodLocation = methodSyntax switch
			{
				MethodDeclarationSyntax mds => mds.Identifier.GetLocation(),
				ConstructorDeclarationSyntax cds => cds.Identifier.GetLocation(),
				_ => methodSyntax.GetLocation()
			};
			var methodLineSpan = methodLocation.GetLineSpan();
			this.MethodFilePath = methodLineSpan.Path ?? "";
			this.MethodStartLine = methodLineSpan.StartLinePosition.Line;
			this.MethodStartColumn = methodLineSpan.StartLinePosition.Character;
			this.MethodEndLine = methodLineSpan.EndLinePosition.Line;
			this.MethodEndColumn = methodLineSpan.EndLinePosition.Character;
			this.MethodTextSpanStart = methodLocation.SourceSpan.Start;
			this.MethodTextSpanLength = methodLocation.SourceSpan.Length;

			// Minor point - With Save we ignore the return values
			if (this.IsSave)
			{
				this.IsNullable = false;
			}

			List<TypeAuthMethodInfo> authMethodInfos = [];

			foreach (var authMethod in authMethods)
			{
				if (((int?)authMethod.AuthorizeFactoryOperation & (int)this.FactoryOperation) != 0)
				{
					authMethodInfos.Add(authMethod);
				}
			}
			this.AuthMethodInfos = new EquatableArray<TypeAuthMethodInfo>([.. authMethodInfos]);
		}

		/// <summary>
		/// Constructor for record primary constructors where the syntax is the RecordDeclarationSyntax.
		/// Used when [Create] is placed on the record type declaration.
		/// </summary>
		public TypeFactoryMethodInfo(FactoryOperation factoryOperation, IMethodSymbol constructorSymbol, RecordDeclarationSyntax recordSyntax, IEnumerable<TypeAuthMethodInfo> authMethods) : base(constructorSymbol, recordSyntax)
		{
			this.FactoryOperation = factoryOperation;
			this.IsConstructor = true;
			this.Name = factoryOperation.ToString();
			this.IsSave = factorySaveOperationAttributes.Contains(factoryOperation);
			this.IsStaticFactory = false; // Primary constructors are not static
			this.IsRemote = this.IsRemote || factoryOperation == FactoryOperation.Execute;

			// Store location for diagnostics - use the record's identifier
			var methodLocation = recordSyntax.Identifier.GetLocation();
			var methodLineSpan = methodLocation.GetLineSpan();
			this.MethodFilePath = methodLineSpan.Path ?? "";
			this.MethodStartLine = methodLineSpan.StartLinePosition.Line;
			this.MethodStartColumn = methodLineSpan.StartLinePosition.Character;
			this.MethodEndLine = methodLineSpan.EndLinePosition.Line;
			this.MethodEndColumn = methodLineSpan.EndLinePosition.Character;
			this.MethodTextSpanStart = methodLocation.SourceSpan.Start;
			this.MethodTextSpanLength = methodLocation.SourceSpan.Length;

			List<TypeAuthMethodInfo> authMethodInfos = [];

			foreach (var authMethod in authMethods)
			{
				if (((int?)authMethod.AuthorizeFactoryOperation & (int)this.FactoryOperation) != 0)
				{
					authMethodInfos.Add(authMethod);
				}
			}
			this.AuthMethodInfos = new EquatableArray<TypeAuthMethodInfo>([.. authMethodInfos]);
		}

		public EquatableArray<TypeAuthMethodInfo> AuthMethodInfos { get; set; } = [];
		public override string NamePostfix => this.Name.Replace(this.FactoryOperation.ToString() ?? "", "");
		public bool IsConstructor { get; set; } = false;
		public FactoryOperation FactoryOperation { get; private set; }
		public bool IsSave { get; private set; }
		public bool IsStaticFactory { get; } = false;

		// Method location info for diagnostics (NF0102)
		public string MethodFilePath { get; }
		public int MethodStartLine { get; }
		public int MethodStartColumn { get; }
		public int MethodEndLine { get; }
		public int MethodEndColumn { get; }
		public int MethodTextSpanStart { get; }
		public int MethodTextSpanLength { get; }
	}

	/// <summary>
	/// Information about an authorization method defined in an AuthorizeFactory class.
	/// </summary>
	internal record TypeAuthMethodInfo : MethodInfo
	{
		public TypeAuthMethodInfo(AuthorizeFactoryOperation authorizeFactoryOperation, IMethodSymbol methodSymbol, BaseMethodDeclarationSyntax methodDeclarationSyntax, string? concreteClassName = null) : base(methodSymbol, methodDeclarationSyntax)
		{
			this.AuthorizeFactoryOperation = authorizeFactoryOperation;
			this.ConcreteClassName = concreteClassName;
		}

		public AuthorizeFactoryOperation AuthorizeFactoryOperation { get; private set; }

		/// <summary>
		/// When ClassName is an interface, this holds the concrete implementing class name.
		/// Null when ClassName is already a concrete class or no implementation was found.
		/// </summary>
		public string? ConcreteClassName { get; private set; }
	}

	/// <summary>
	/// Base record containing common method information shared by TypeFactoryMethodInfo and TypeAuthMethodInfo.
	/// </summary>
	internal record MethodInfo
	{
		protected MethodInfo(IMethodSymbol methodSymbol, BaseMethodDeclarationSyntax methodSyntax)
		{
			var otherAttributes = methodSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).Where(a => a != null).ToList();

			this.Name = methodSymbol.Name;
			this.ClassName = methodSymbol.ContainingType.Name;
			this.IsBool = methodSymbol.ReturnType.ToString().Contains("bool");
			this.IsRemote = otherAttributes.Any(a => a == "Remote");
			this.IsInternal = methodSymbol.DeclaredAccessibility != Accessibility.Public;

			this.ReturnType = methodSymbol.ReturnType.ToString();
			this.IsNullable = methodSymbol.ReturnType.NullableAnnotation == NullableAnnotation.Annotated;


			if (methodSymbol.ReturnType is INamedTypeSymbol returnTypeSymbol && returnTypeSymbol.Name == "Task")
			{
				this.IsTask = true;
				if (returnTypeSymbol.IsGenericType)
				{
					this.IsNullable = returnTypeSymbol.TypeArguments.Any(t => t.NullableAnnotation == NullableAnnotation.Annotated);
					this.ReturnType = returnTypeSymbol.TypeArguments.First().ToString();
				}
			}

			if (methodSyntax.ParameterList is ParameterListSyntax parameterListSyntax)
			{
				this.Parameters = new EquatableArray<MethodParameterInfo>([.. parameterListSyntax.Parameters.Select(p => new MethodParameterInfo(p, methodSymbol))]);
			}
			else
			{
				this.Parameters = [];
			}

			// Discover plain DTO types for constructor registration (IL trimming support)
			this.DtoReturnTypes = DiscoverDtoTypes(methodSymbol);
		}

		/// <summary>
		/// Constructor for record primary constructors where the syntax is the RecordDeclarationSyntax.
		/// </summary>
		protected MethodInfo(IMethodSymbol constructorSymbol, RecordDeclarationSyntax recordSyntax)
		{
			var otherAttributes = constructorSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).Where(a => a != null).ToList();

			this.Name = constructorSymbol.Name;
			this.ClassName = constructorSymbol.ContainingType.Name;
			this.IsBool = false; // Constructors don't return bool
			this.IsRemote = otherAttributes.Any(a => a == "Remote");
			this.IsInternal = constructorSymbol.DeclaredAccessibility != Accessibility.Public;

			this.ReturnType = constructorSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			this.IsNullable = false; // Constructor return is not nullable

			// For records, the primary constructor parameters are in the RecordDeclarationSyntax.ParameterList
			if (recordSyntax.ParameterList is ParameterListSyntax parameterListSyntax)
			{
				this.Parameters = new EquatableArray<MethodParameterInfo>([.. parameterListSyntax.Parameters.Select(p => new MethodParameterInfo(p, constructorSymbol))]);
			}
			else
			{
				this.Parameters = [];
			}

			// Record primary constructors return [Factory]-annotated types (excluded by DiscoverDtoTypes)
			this.DtoReturnTypes = DiscoverDtoTypes(constructorSymbol);
		}

		public string Name { get; set; }
		public string ClassName { get; set; }
		public virtual string NamePostfix => this.Name;
		public bool IsNullable { get; protected set; }
		public bool IsBool { get; private set; }
		public bool IsTask { get; private set; }
		public bool IsRemote { get; protected set; }
		public bool IsInternal { get; protected set; }
		public string? ReturnType { get; protected set; }
		public EquatableArray<MethodParameterInfo> Parameters { get; private set; }
		public EquatableArray<AspAuthorizeInfo> AspAuthorizeCalls { get; set; } = [];

		/// <summary>
		/// Fully-qualified names of plain DTO types discovered in the method's return type and parameters.
		/// Used to emit DtoConstructorRegistry.Register calls for IL trimming support.
		/// </summary>
		public EquatableArray<string> DtoReturnTypes { get; private set; } = [];

		/// <summary>
		/// Discovers plain DTO types in a method's return type and non-service parameters that need
		/// constructor registration for IL trimming support. Unwraps Task, nullable, and generic
		/// collections. Excludes primitives, [Factory] types, abstract/interface types, and types
		/// without parameterless ctors. Recursively walks public properties of discovered DTOs to
		/// find nested types. Delegates to DtoTypeWalker.WalkFactoryReturn — shared with the
		/// event-type preservation path.
		/// </summary>
		private static EquatableArray<string> DiscoverDtoTypes(IMethodSymbol methodSymbol)
		{
			var visited = new HashSet<string>();
			var dtoTypes = new List<string>();

			// Discover from return type
			var returnCandidates = DtoTypeWalker.UnwrapType(methodSymbol.ReturnType, unwrapTask: true);
			foreach (var candidate in returnCandidates)
			{
				DtoTypeWalker.WalkFactoryReturn(candidate, dtoTypes, visited);
			}

			// Discover from non-service, non-CancellationToken parameters
			foreach (var parameter in methodSymbol.Parameters)
			{
				var isService = parameter.GetAttributes().Any(a =>
					a.AttributeClass?.Name == "ServiceAttribute" || a.AttributeClass?.Name == "Service");
				if (isService)
					continue;

				if (parameter.Type.Name == "CancellationToken")
					continue;

				var paramCandidates = DtoTypeWalker.UnwrapType(parameter.Type, unwrapTask: false);
				foreach (var candidate in paramCandidates)
				{
					DtoTypeWalker.WalkFactoryReturn(candidate, dtoTypes, visited);
				}
			}

			return new EquatableArray<string>([.. dtoTypes]);
		}
	}

	/// <summary>
	/// Information about a method parameter.
	/// </summary>
	internal sealed record MethodParameterInfo
	{
		public MethodParameterInfo() { }

		public MethodParameterInfo(ParameterSyntax parameterSyntax, IMethodSymbol methodSymbol)
		{
			this.Name = parameterSyntax.Identifier.Text;
			// Trim whitespace trivia to ensure consistent grouping regardless of source formatting
			this.Type = parameterSyntax.Type!.ToFullString().Trim();
			this.IsService = parameterSyntax.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.ToFullString() == "Service");

			// Detect CancellationToken parameters - they should not be serialized for remote calls
			var typeText = this.Type.Trim();
			this.IsCancellationToken = typeText == "CancellationToken" ||
									   typeText == "System.Threading.CancellationToken";

			// Detect params modifier - params must be last, so CancellationToken goes before it
			this.IsParams = parameterSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword));
		}

		public string Name { get; set; } = null!;
		public string Type { get; set; } = null!;
		public bool IsService { get; set; }
		public bool IsTarget { get; set; }
		public bool IsCancellationToken { get; set; }
		public bool IsParams { get; set; }

		public bool Equals(MethodParameterInfo obj)
		{
			return obj is MethodParameterInfo info &&
						 this.Name == info.Name &&
						 this.Type == info.Type;
		}

		public override int GetHashCode()
		{
			return $"{this.Name}{this.Type}".GetHashCode();
		}

	}

	/// <summary>
	/// Information about an ASP.NET Core [Authorize] attribute applied to a factory method.
	/// </summary>
	public record AspAuthorizeInfo
	{
		EquatableArray<string> _constructorArguments = [];
		EquatableArray<string> _namedArguments = [];

		public IReadOnlyList<string> ConstructorArguments => _constructorArguments.ToList();
		public IReadOnlyList<string> NamedArguments => _namedArguments.ToList();

		public AspAuthorizeInfo(AttributeData attribute)
		{
			var attributeSyntax = attribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

			List<string> namedArguments = [];
			List<string> constructorArguments = [];

			foreach (var attributeArgument in attributeSyntax?.ArgumentList?.Arguments ?? [])
			{
				var argumentText = attributeArgument.ToString();
				if (argumentText.Contains("="))
				{
					namedArguments.Add(argumentText);
				}
				else
				{
					constructorArguments.Add(argumentText);
				}
			}

			this._namedArguments = new EquatableArray<string>([.. namedArguments]);
			this._constructorArguments = new EquatableArray<string>([.. constructorArguments]);
		}

		public string ToAspAuthorizedDataText()
		{
			var constructorArgumentsText = string.Join(", ", this._constructorArguments);
			var namedArgumentsText = string.Join(", ", this._namedArguments);
			var text = $"new AspAuthorizeData({constructorArgumentsText})";

			if (!string.IsNullOrEmpty(namedArgumentsText))
			{
				text += $"{{ {namedArgumentsText} }}";
			}
			return text;
		}
	}
}
