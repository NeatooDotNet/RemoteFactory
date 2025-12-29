// FactoryGenerator.Types.cs
// Contains all record and class type definitions used by the FactoryGenerator.
// These types model the data extracted from source code during the transform phase
// and are used during the code generation phase.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using static Neatoo.Factory;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo;

public partial class Factory
{
	/// <summary>
	/// List of factory operations that are considered "save" operations (Insert, Update, Delete).
	/// Used to determine if a factory method should be treated as a write operation.
	/// </summary>
	private static List<FactoryOperation> factorySaveOperationAttributes = [.. Enum.GetValues(typeof(FactoryOperation)).Cast<FactoryOperation>().Where(v => ((int)v & (int)AuthorizeFactoryOperation.Write) != 0)];

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

			this.AuthMethods = TypeAuthMethods(semanticModel, symbol, debugMessages, diagnostics);

			List<FactoryOperation> defaultFactoryOperations = [];

			if (this.IsInterface)
			{
				defaultFactoryOperations.Add(FactoryOperation.Execute);
			}

			this.FactoryMethods = new EquatableArray<TypeFactoryMethodInfo>([.. TypeFactoryMethods(serviceSymbol, methodSymbols, defaultFactoryOperations, this.AuthMethods.ToList(), debugMessages, diagnostics)]);

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

		public string Name { get; }
		public bool IsPartial { get; }
		public string SignatureText { get; }
		public bool IsInterface { get; }
		public bool IsStatic { get; }
		public string ServiceTypeName { get; }
		public string ImplementationTypeName { get; }
		public string Namespace { get; }
		public EquatableArray<string> UsingStatements { get; } = [];
		public EquatableArray<TypeFactoryMethodInfo> FactoryMethods { get; set; } = [];
		public EquatableArray<TypeAuthMethodInfo> AuthMethods { get; set; } = [];

		public string SafeHintName { get; }

		/// <summary>
		/// Diagnostics collected during the transform phase.
		/// </summary>
		public EquatableArray<DiagnosticInfo> Diagnostics { get; }

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
		public TypeAuthMethodInfo(AuthorizeFactoryOperation authorizeFactoryOperation, IMethodSymbol methodSymbol, BaseMethodDeclarationSyntax methodDeclarationSyntax) : base(methodSymbol, methodDeclarationSyntax)
		{
			this.AuthorizeFactoryOperation = authorizeFactoryOperation;
		}

		public AuthorizeFactoryOperation AuthorizeFactoryOperation { get; private set; }

		public void MakeAuthCall(FactoryMethod inMethod, StringBuilder methodBuilder)
		{
			var parameterText = "";

			if (this.Parameters.Count > 0)
			{
				var declaredParameters = inMethod.Parameters.ToList();

				declaredParameters.RemoveAll(p => p.IsTarget);

				var callParameter = this.Parameters.ToList().GetEnumerator();
				var declaredParameter = declaredParameters.GetEnumerator();
				var parameters = new List<string>();

				declaredParameter.MoveNext();

				while (callParameter.MoveNext() && declaredParameter.Current != null)
				{
					while (declaredParameter.Current.Type != callParameter.Current.Type)
					{
						if (!declaredParameter.MoveNext())
						{
							break;
						}
					}

					if (declaredParameter.Current == null)
					{
						break;
					}

					parameters.Add(declaredParameter.Current.Name);

					declaredParameter.MoveNext();
				}

				while (callParameter.Current != null)
				{
					parameters.Add($"/* Missing {callParameter.Current.Type} {callParameter.Current.Name} */");
					callParameter.MoveNext();
				}

				parameterText = string.Join(", ", parameters);
			}

			var callText = $"{this.ClassName.ToLower()}.{this.Name}({parameterText})";

			if (this.IsTask)
			{
				callText = $"await {callText}";
			}

			methodBuilder.AppendLine($"authorized = {callText};");
			methodBuilder.AppendLine($"if (!authorized.HasAccess)");
			methodBuilder.AppendLine("{");

			if (!inMethod.AspForbid)
			{
				var returnText = $"authorized";
				if (inMethod is not CanFactoryMethod)
				{
					returnText = $"new {inMethod.ReturnType(includeTask: false)}(authorized)";
				}

				if (!this.IsTask && inMethod.IsTask && !inMethod.IsAsync)
				{
					returnText = $"Task.FromResult({returnText})";
				}

				methodBuilder.AppendLine($"return {returnText};");
			}
			else
			{
				methodBuilder.AppendLine($"throw new NotAuthorizedException(authorized);");
			}

			methodBuilder.AppendLine("}");
		}
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
		}
		public string Name { get; set; }
		public string ClassName { get; set; }
		public virtual string NamePostfix => this.Name;
		public bool IsNullable { get; protected set; }
		public bool IsBool { get; private set; }
		public bool IsTask { get; private set; }
		public bool IsRemote { get; protected set; }
		public string? ReturnType { get; protected set; }
		public EquatableArray<MethodParameterInfo> Parameters { get; private set; }
		public EquatableArray<AspAuthorizeInfo> AspAuthorizeCalls { get; set; } = [];
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
			this.Type = parameterSyntax.Type!.ToFullString();
			this.IsService = parameterSyntax.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.ToFullString() == "Service");
		}

		public string Name { get; set; } = null!;
		public string Type { get; set; } = null!;
		public bool IsService { get; set; }
		public bool IsTarget { get; set; }

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
		EquatableArray<string> ConstructorArguments = [];
		EquatableArray<string> NamedArguments = [];

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

			this.NamedArguments = new EquatableArray<string>([.. namedArguments]);
			this.ConstructorArguments = new EquatableArray<string>([.. constructorArguments]);
		}

		public string ToAspAuthorizedDataText()
		{
			var constructorArgumentsText = string.Join(", ", this.ConstructorArguments);
			var namedArgumentsText = string.Join(", ", this.NamedArguments);
			var text = $"new AspAuthorizeData({constructorArgumentsText})";

			if (!string.IsNullOrEmpty(namedArgumentsText))
			{
				text += $"{{ {namedArgumentsText} }}";
			}
			return text;
		}
	}

	/// <summary>
	/// Abstract base class for all factory method code generators.
	/// Provides common functionality for generating method signatures, delegates, and service registrations.
	/// </summary>
	internal abstract class FactoryMethod(string serviceType, string implementationType)
	{
		public string ServiceType { get; protected set; } = serviceType;
		public string ImplementationType { get; set; } = implementationType;
		public string Name { get; protected set; } = null!;
		public string UniqueName { get; set; } = null!;
		public string? NamePostfix { get; protected set; }
		public string DelegateName => $"{this.UniqueName}Delegate";

		public List<TypeAuthMethodInfo> AuthMethodInfos { get; set; } = [];
		public List<AspAuthorizeInfo> AspAuthorizeInfo { get; set; } = [];
		public virtual bool HasAuth => this.AuthMethodInfos.Count > 0 || this.AspAuthorizeInfo.Count > 0;
		public bool AspForbid { get; set; } = false; // If true, the method will return a 403 Forbidden response if authorization fails
		public FactoryOperation FactoryOperation { get; set; }
		public List<MethodParameterInfo> Parameters { get; set; } = null!;

		public virtual bool IsSave => false;
		public virtual bool IsBool => false;
		public virtual bool IsNullable => false;// (this.IsBool || this.IsSave) && (!this.IsSave || !this.HasAuth);
		public virtual bool IsTask => this.IsRemote || this.AuthMethodInfos.Any(m => m.IsTask) || this.AspAuthorizeInfo.Count > 0;
		public virtual bool IsRemote => this.AuthMethodInfos.Any(m => m.IsRemote) || this.AspAuthorizeInfo.Count > 0;
		public virtual bool IsAsync => this.AuthMethodInfos.Any(m => m.IsTask) || this.AspAuthorizeInfo.Count > 0;

		public virtual string AsyncKeyword => this.IsAsync ? "async" : "";
		public virtual string AwaitKeyword => this.IsAsync ? "await" : "";
		public string ServiceAssignmentsText => WithStringBuilder(this.Parameters.Where(p => p.IsService).Select(p => $"var {p.Name} = ServiceProvider.GetRequiredService<{p.Type}>();"));
		public virtual string ReturnType(bool includeTask = true, bool includeAuth = true, bool includeBool = true)
		{
			var returnType = this.ServiceType;

			if (this.HasAuth && includeAuth)
			{
				returnType = $"Authorized<{returnType}>";
			}
			else if (this.IsNullable || (this.IsBool && includeBool))
			{
				returnType = $"{returnType}?";
			}

			if (includeTask && this.IsTask)
			{
				returnType = $"Task<{returnType}>";
			}

			return returnType;
		}
		public string ParameterDeclarationsText(bool includeServices = false, bool includeTarget = true)
		{
			return string.Join(", ", this.Parameters.Where(p => (includeServices || !p.IsService) && (includeTarget || !p.IsTarget)).Select(p => $"{p.Type} {p.Name}"));
		}

		public string ParameterIdentifiersText(bool includeServices = false, bool includeTarget = true)
		{
			var result = string.Join(", ", this.Parameters.Where(p => (includeServices || !p.IsService) && (includeTarget || !p.IsTarget)).Select(p => p.Name));

			return result.TrimStart(',').TrimEnd(',');
		}

		public virtual void AddFactoryText(FactoryText classText)
		{
			classText.InterfaceMethods.Append(this.InterfaceMethods());

			if (this.IsRemote)
			{
				classText.Delegates.Append(this.Delegates());
				classText.PropertyDeclarations.Append(this.PropertyDeclarations());
				classText.ConstructorPropertyAssignmentsLocal.Append(this.ConstructorPropertyAssignmentsLocal());
				classText.ConstructorPropertyAssignmentsRemote.Append(this.ConstructorPropertyAssignmentsRemote());
				classText.ServiceRegistrations.Append(this.ServiceRegistrations());
			}

			var methodBuilder = new StringBuilder();
			methodBuilder.Append(this.PublicMethod());
			methodBuilder.Append(this.RemoteMethod());
			methodBuilder.Append(this.LocalMethod());

			classText.MethodsBuilder.Append(methodBuilder);
		}

		public virtual StringBuilder InterfaceMethods()
		{
			return new StringBuilder().AppendLine($"{this.ReturnType(includeAuth: false)} {this.Name}({this.ParameterDeclarationsText(includeServices: false)});");
		}

		public virtual StringBuilder Delegates()
		{
			return new StringBuilder().AppendLine($"public delegate {this.ReturnType()} {this.DelegateName}({this.ParameterDeclarationsText()});");
		}

		public virtual StringBuilder PropertyDeclarations()
		{
			var propertyBuilder = new StringBuilder();
			propertyBuilder.AppendLine($"public {this.DelegateName} {this.UniqueName}Property {{ get; }}");
			return propertyBuilder;
		}

		public virtual StringBuilder ConstructorPropertyAssignmentsLocal()
		{
			var methodBuilder = new StringBuilder();
			methodBuilder.AppendLine($"{this.UniqueName}Property = Local{this.UniqueName};");
			return methodBuilder;
		}

		public virtual StringBuilder ConstructorPropertyAssignmentsRemote()
		{
			var methodBuilder = new StringBuilder();
			if (this.IsRemote)
			{
				methodBuilder.AppendLine($"{this.UniqueName}Property = Remote{this.UniqueName};");
			}
			return methodBuilder;
		}

		public virtual StringBuilder ServiceRegistrations()
		{
			return new StringBuilder().AppendLine($@"services.AddScoped<{this.DelegateName}>(cc => {{
                                                    var factory = cc.GetRequiredService<{this.ImplementationType}Factory>();
                                                    return ({this.ParameterDeclarationsText()}) => factory.Local{this.UniqueName}({this.ParameterIdentifiersText()});
                                                }});");
		}

		public virtual StringBuilder PublicMethod(bool? overrideHasAuth = null)
		{
			var hasAuth = overrideHasAuth ?? this.HasAuth;

			var asyncKeyword = this.IsTask && hasAuth ? "async" : "";
			var awaitKeyword = this.IsTask && hasAuth ? "await" : "";

			var methodBuilder = new StringBuilder();

			methodBuilder.AppendLine($"public virtual {asyncKeyword} {this.ReturnType(includeAuth: false)} {this.Name}({this.ParameterDeclarationsText(includeServices: false)})");
			methodBuilder.AppendLine("{");

			if (!hasAuth)
			{
				methodBuilder.AppendLine($"return Local{this.UniqueName}({this.ParameterIdentifiersText()});");
			}
			else
			{
				methodBuilder.AppendLine($"return ({awaitKeyword} Local{this.UniqueName}({this.ParameterIdentifiersText(includeServices: false)})).Result;");
			}

			methodBuilder.AppendLine("}");

			if (this.IsRemote)
			{
				methodBuilder.Replace($"Local{this.UniqueName}", $"{this.UniqueName}Property");
			}

			return methodBuilder;
		}

		public virtual StringBuilder RemoteMethod()
		{
			var methodBuilder = new StringBuilder();
			if (this.IsRemote)
			{
				var nullableText = this.ReturnType(includeTask: false).EndsWith("?") ? "Nullable" : "";

				methodBuilder.AppendLine($"public virtual async {this.ReturnType()} Remote{this.UniqueName}({this.ParameterDeclarationsText()})");
				methodBuilder.AppendLine("{");
				methodBuilder.AppendLine($" return (await MakeRemoteDelegateRequest!.ForDelegate{nullableText}<{this.ReturnType(includeTask: false)}>(typeof({this.DelegateName}), [{this.ParameterIdentifiersText()}]))!;");
				methodBuilder.AppendLine("}");
				methodBuilder.AppendLine("");

			}
			return methodBuilder;
		}

		protected virtual StringBuilder LocalMethodStart()
		{
			var methodBuilder = new StringBuilder();

			methodBuilder.AppendLine($"public {this.AsyncKeyword} {this.ReturnType()} Local{this.UniqueName}({this.ParameterDeclarationsText()})");
			methodBuilder.AppendLine("{");

			if (this.AuthMethodInfos.Count > 0 || this.AspAuthorizeInfo.Count > 0)
			{
				methodBuilder.AppendLine("Authorized authorized;");
				foreach (var authClass in this.AuthMethodInfos.GroupBy(m => m.ClassName))
				{
					methodBuilder.AppendLine($"{authClass.Key} {authClass.Key.ToLower()} = ServiceProvider.GetRequiredService<{authClass.Key}>();");
					foreach (var authMethod in authClass)
					{
						authMethod.MakeAuthCall(this, methodBuilder);
					}
				}

				if (this.AspAuthorizeInfo.Count > 0)
				{
					methodBuilder.AppendLine($"var aspAuthorized = ServiceProvider.GetRequiredService<IAspAuthorize>();");

					var aspAuthorizeDataText = string.Join(", ", this.AspAuthorizeInfo.Select(a => a.ToAspAuthorizedDataText()));
					methodBuilder.AppendLine($"authorized = await aspAuthorized.Authorize([ {aspAuthorizeDataText} ], {this.AspForbid.ToString().ToLower()});");

					if (!this.AspForbid)
					{
						methodBuilder.AppendLine($"if (!authorized.HasAccess)");
						methodBuilder.AppendLine("{");
						methodBuilder.AppendLine($"return new {this.ReturnType(includeTask: false)}(authorized);");
						methodBuilder.AppendLine("}");
					}
				}
			}

			return methodBuilder;
		}

		public abstract StringBuilder LocalMethod();
	}

	/// <summary>
	/// Factory method for read operations (Create, Fetch).
	/// Creates new instances or retrieves existing ones.
	/// </summary>
	internal class ReadFactoryMethod : FactoryMethod
	{
		public ReadFactoryMethod(string serviceType, string implementationType, TypeFactoryMethodInfo callMethod) : base(serviceType, implementationType)
		{
			this.ImplementationType = implementationType;
			this.CallMethod = callMethod;
			this.Name = callMethod.Name;
			this.UniqueName = callMethod.Name;
			this.NamePostfix = callMethod.NamePostfix;
			this.FactoryOperation = callMethod.FactoryOperation;
			this.Parameters = callMethod.Parameters.ToList();
			this.AuthMethodInfos.AddRange(callMethod.AuthMethodInfos);
			this.AspAuthorizeInfo = callMethod.AspAuthorizeCalls.ToList();
		}
		public override bool IsSave => this.CallMethod.IsSave;
		public override bool IsBool => this.CallMethod.IsBool;
		public override bool IsTask => this.IsRemote || this.CallMethod.IsTask || this.AuthMethodInfos.Any(m => m.IsTask) || this.AspAuthorizeInfo.Count > 0;
		public override bool IsRemote => this.CallMethod.IsRemote || this.AuthMethodInfos.Any(m => m.IsRemote) || this.AspAuthorizeInfo.Count > 0;
		public override bool IsAsync => (this.HasAuth && this.CallMethod.IsTask) || this.AuthMethodInfos.Any(m => m.IsTask) || this.AspAuthorizeInfo.Count > 0;
		public override bool IsNullable => this.CallMethod.IsNullable || this.HasAuth || this.IsBool;

		public TypeFactoryMethodInfo CallMethod { get; set; }

		public virtual string DoFactoryMethodCall()
		{
			var methodCall = "DoFactoryMethodCall";

			if (this.CallMethod.IsBool)
			{
				methodCall += "Bool";
			}

			if (this.CallMethod.IsTask)
			{
				methodCall += "Async";
				if (this.CallMethod.IsNullable)
				{
					methodCall += "Nullable";
				}
			}

			if (this.CallMethod.IsConstructor)
			{
				methodCall = $"{methodCall}(FactoryOperation.{this.FactoryOperation}, () => new {this.ImplementationType}({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";
			}
			else if (this.CallMethod.IsStaticFactory)
			{
				methodCall = $"{methodCall}(FactoryOperation.{this.FactoryOperation}, () => {this.ImplementationType}.{this.Name}({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";
			}
			else
			{
				methodCall = $"{methodCall}(target, FactoryOperation.{this.FactoryOperation}, () => target.{this.Name} ({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";
			}

			if (this.IsAsync && this.CallMethod.IsTask)
			{
				methodCall = $"await {methodCall}";
			}

			if (this.HasAuth)
			{
				methodCall = $"new Authorized<{this.ServiceType}>({methodCall})";
			}

			if (!this.CallMethod.IsTask && this.IsTask && !this.IsAsync)
			{
				methodCall = $"Task.FromResult({methodCall})";
			}

			return methodCall;
		}

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = base.LocalMethodStart();

			if (!this.CallMethod.IsConstructor && !this.CallMethod.IsStaticFactory)
			{
				methodBuilder.AppendLine($"var target = ServiceProvider.GetRequiredService<{this.ImplementationType}>();");
			}

			methodBuilder.AppendLine($"{this.ServiceAssignmentsText}");
			methodBuilder.AppendLine($"return {this.DoFactoryMethodCall()};");
			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("");

			return methodBuilder;
		}
	}

	/// <summary>
	/// Factory method for write operations (Insert, Update, Delete).
	/// Operates on an existing target instance.
	/// </summary>
	internal class WriteFactoryMethod : ReadFactoryMethod
	{
		public WriteFactoryMethod(string targetType, string concreteType, TypeFactoryMethodInfo callFactoryMethod) : base(targetType, concreteType, callFactoryMethod)
		{
			this.Parameters.Insert(0, new MethodParameterInfo() { Name = "target", Type = $"{targetType}", IsService = false, IsTarget = true });
		}

		public override void AddFactoryText(FactoryText classText)
		{
			classText.MethodsBuilder.Append(this.LocalMethod());
		}

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = base.LocalMethodStart();

			methodBuilder.AppendLine($"var cTarget = ({this.ImplementationType}) target ?? throw new Exception(\"{this.ServiceType} must implement {this.ImplementationType}\");");
			methodBuilder.AppendLine($"{this.ServiceAssignmentsText}");
			methodBuilder.AppendLine($"return {this.DoFactoryMethodCall().Replace("target", "cTarget")};");
			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("");

			return methodBuilder;
		}
	}

	/// <summary>
	/// Factory method for combined Save operations (Insert + Update + Delete).
	/// Determines which operation to call based on the target's state (IsNew, IsDeleted).
	/// </summary>
	internal class SaveFactoryMethod : FactoryMethod
	{
		public SaveFactoryMethod(string? nameOverride, string serviceType, string implementationType, List<WriteFactoryMethod> writeFactoryMethods) : base(serviceType, implementationType)
		{
			var writeFactoryMethod = writeFactoryMethods.OrderByDescending(w => w.FactoryOperation!).First();
			this.Name = $"Save{writeFactoryMethod.NamePostfix}";
			this.UniqueName = this.Name;
			this.WriteFactoryMethods = writeFactoryMethods;
			this.Parameters = writeFactoryMethods.First().Parameters;
			this.AuthMethodInfos.AddRange(writeFactoryMethods.SelectMany(m => m.AuthMethodInfos).Distinct());
			this.AspAuthorizeInfo.AddRange(writeFactoryMethods.SelectMany(m => m.AspAuthorizeInfo).Distinct());
		}

		public bool IsDefault { get; set; } = false;
		public override bool IsSave => true;
		public override bool IsRemote => this.WriteFactoryMethods.Any(m => m.IsRemote);
		public override bool IsTask => this.IsRemote || this.WriteFactoryMethods.Any(m => m.IsTask);
		public override bool IsAsync => this.WriteFactoryMethods.Any(m => m.IsTask);
		public override bool HasAuth => this.WriteFactoryMethods.Any(m => m.HasAuth);
		public override bool IsNullable => this.WriteFactoryMethods.Any(m => m.FactoryOperation == FactoryOperation.Delete || m.IsNullable);

		public List<WriteFactoryMethod> WriteFactoryMethods { get; }

		public override StringBuilder PublicMethod(bool? overrideHasAuth = null)
		{
			if (!(overrideHasAuth ?? this.HasAuth))
			{
				return base.PublicMethod(overrideHasAuth);
			}

			var methodBuilder = new StringBuilder();

			var asyncKeyword = this.IsTask && this.HasAuth ? "async" : "";
			var awaitKeyword = this.IsTask && this.HasAuth ? "await" : "";

			methodBuilder.AppendLine($"public virtual {asyncKeyword} {this.ReturnType(includeAuth: false)} {this.Name}({this.ParameterDeclarationsText()})");
			methodBuilder.AppendLine("{");

			methodBuilder.AppendLine($"var authorized = ({awaitKeyword} Local{this.UniqueName}({this.ParameterIdentifiersText()}));");

			methodBuilder.AppendLine("if (!authorized.HasAccess)");
			methodBuilder.AppendLine("{");
			methodBuilder.AppendLine("throw new NotAuthorizedException(authorized);");
			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("return authorized.Result;");
			methodBuilder.AppendLine("}");

			methodBuilder.AppendLine($"public virtual {this.AsyncKeyword} {this.ReturnType()} Try{this.Name}({this.ParameterDeclarationsText()})");
			methodBuilder.AppendLine("{");
			methodBuilder.AppendLine($"return {this.AwaitKeyword} Local{this.UniqueName}({this.ParameterIdentifiersText()});");
			methodBuilder.AppendLine("}");

			if (this.IsRemote)
			{
				methodBuilder.Replace($"Local{this.UniqueName}", $"{this.UniqueName}Property");
			}

			return methodBuilder;
		}

		public override StringBuilder InterfaceMethods()
		{
			var stringBuilder = base.InterfaceMethods();

			if (this.HasAuth)
			{
				stringBuilder.AppendLine($"{this.ReturnType()} Try{this.Name}({this.ParameterDeclarationsText()});");
			}
			return stringBuilder;
		}

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = new StringBuilder();

			if (this.IsDefault)
			{
				methodBuilder.AppendLine($"async Task<IFactorySaveMeta?> IFactorySave<{this.ImplementationType}>.Save({this.ImplementationType} target)");
				methodBuilder.AppendLine("{");

				if (this.IsTask)
				{
					methodBuilder.AppendLine($"return (IFactorySaveMeta?) await {this.Name}(target);");
				}
				else
				{
					methodBuilder.AppendLine($"return await Task.FromResult((IFactorySaveMeta?) {this.Name}(target));");
				}
				methodBuilder.AppendLine("}");
			}

			methodBuilder.AppendLine($@"public virtual {this.AsyncKeyword} {this.ReturnType()} Local{this.UniqueName}({this.ParameterDeclarationsText()})
                                            {{");

			var defaultReturn = $"default({this.ServiceType})";
			if (this.HasAuth)
			{
				defaultReturn = $"new Authorized<{this.ServiceType}>()";
			}

			if (this.IsTask && !this.IsAsync)
			{
				defaultReturn = $"Task.FromResult({defaultReturn})";
			}

			string DoInsertUpdateDeleteMethodCall(FactoryMethod? method)
			{

				if (method == null)
				{
					return $"throw new NotImplementedException()";
				}

				var methodCall = $"Local{method.UniqueName}({this.ParameterIdentifiersText()})";

				if (method.IsTask && this.IsAsync)
				{
					methodCall = $"await {methodCall}";
				}

				if (this.HasAuth && !method.HasAuth)
				{
					methodCall = $"new Authorized<{this.ServiceType}>({methodCall})";
				}

				if (!method.IsTask && this.IsTask && !this.IsAsync)
				{
					methodCall = $"Task.FromResult({methodCall})";
				}

				if (method.FactoryOperation == FactoryOperation.Delete)
				{
					methodCall = $@"if (target.IsNew) {{ return {defaultReturn}; }}
										return {methodCall}";
					return methodCall;
				}
				else
				{
					return $"return {methodCall}";
				}
			}



			methodBuilder.AppendLine($@"
                                            if (target.IsDeleted)
                                    {{

                                        {DoInsertUpdateDeleteMethodCall(this.WriteFactoryMethods.Where(s => s.FactoryOperation == FactoryOperation.Delete).FirstOrDefault())};
                                    }}
                                    else if (target.IsNew)
                                    {{
                                        {DoInsertUpdateDeleteMethodCall(this.WriteFactoryMethods.Where(s => s.FactoryOperation == FactoryOperation.Insert).FirstOrDefault())};
                                    }}
                                    else
                                    {{
                                         {DoInsertUpdateDeleteMethodCall(this.WriteFactoryMethods.Where(s => s.FactoryOperation == FactoryOperation.Update).FirstOrDefault())};
                                    }}
                            ");

			methodBuilder.AppendLine("}");

			return methodBuilder;
		}
	}

	/// <summary>
	/// Factory method for interface-based remote operations.
	/// Used when [Factory] is applied to an interface.
	/// </summary>
	internal class InterfaceFactoryMethod : ReadFactoryMethod
	{
		public InterfaceFactoryMethod(string serviceType, string implementationType, TypeFactoryMethodInfo callMethod) : base(serviceType, implementationType, callMethod)
		{
			this.AspForbid = true;
		}

		public override StringBuilder ServiceRegistrations()
		{
			return new StringBuilder().AppendLine($@"services.AddScoped<{this.DelegateName}>(cc => {{
                                                    var factory = cc.GetRequiredService<{this.ImplementationType}Factory>();
                                                    return ({this.ParameterDeclarationsText()}) => factory.{this.Name}({this.ParameterIdentifiersText()});
                                                }});");
		}

		public override StringBuilder PublicMethod(bool? overrideHasAuth = null) => base.PublicMethod(false);

		public override StringBuilder InterfaceMethods() => new StringBuilder();

		public override bool IsBool => false;
		public override bool IsNullable => this.CallMethod.IsNullable;

		public override string ReturnType(bool includeTask = true, bool includeAuth = true, bool includeBool = true) => base.ReturnType(includeTask, false, includeBool);

		public override string DoFactoryMethodCall()
		{
			var methodCall = $"target.{this.Name} ({this.ParameterIdentifiersText(includeServices: false, includeTarget: false)})";

			if (this.IsAsync && this.CallMethod.IsTask)
			{
				methodCall = $"await {methodCall}";
			}

			if (!this.CallMethod.IsTask && this.IsTask && !this.IsAsync)
			{
				methodCall = $"Task.FromResult({methodCall})";
			}

			return methodCall;
		}
	}

	/// <summary>
	/// Factory method for authorization checks.
	/// Generates CanXxx methods that check authorization without performing the operation.
	/// </summary>
	internal class CanFactoryMethod : FactoryMethod
	{
		public CanFactoryMethod(string targetType, string concreteType, string methodName) : base(targetType, concreteType)
		{
			this.Name = $"Can{methodName}";
			this.UniqueName = $"Can{methodName}";
		}
		public CanFactoryMethod(string targetType, string concreteType, string methodName, ICollection<TypeAuthMethodInfo> authMethods, ICollection<AspAuthorizeInfo> aspAuthorizeCalls) : this(targetType, concreteType, methodName)
		{
			this.Parameters = authMethods.SelectMany(m => m.Parameters).Distinct().ToList();
			this.AuthMethodInfos.AddRange(authMethods);
			this.AspAuthorizeInfo.AddRange(aspAuthorizeCalls);
		}

		public override bool IsBool => true;

		public override string ReturnType(bool includeTask = true, bool includeAuth = true, bool includeBool = true)
		{
			var returnType = "Authorized";

			if (includeTask && this.IsTask)
			{
				returnType = $"Task<{returnType}>";
			}

			return returnType;
		}

		public override StringBuilder PublicMethod(bool? overrideHasAuth = null)
		{
			var methodBuilder = new StringBuilder();

			methodBuilder.AppendLine($"public virtual {this.ReturnType()} {this.UniqueName}({this.ParameterDeclarationsText(includeServices: false)})");
			methodBuilder.AppendLine("{");

			methodBuilder.AppendLine($"return Local{this.UniqueName}({this.ParameterIdentifiersText(includeServices: false)});");

			methodBuilder.AppendLine("}");

			if (this.IsRemote)
			{
				methodBuilder.Replace($"Local{this.UniqueName}", $"{this.UniqueName}Property");
			}

			return methodBuilder;
		}

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = base.LocalMethodStart();

			var returnText = $"new Authorized(true)";

			if (this.IsTask && !this.IsAsync)
			{
				returnText = $"Task.FromResult({returnText})";
			}

			methodBuilder.AppendLine($"return {returnText};");

			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("");
			return methodBuilder;
		}
	}

	/// <summary>
	/// Container for collecting generated source code fragments.
	/// Used during the generation phase to accumulate various parts of the factory class.
	/// </summary>
	internal class FactoryText
	{
		public StringBuilder Delegates { get; set; } = new();
		public StringBuilder ConstructorPropertyAssignmentsLocal { get; set; } = new();
		public StringBuilder ConstructorPropertyAssignmentsRemote { get; set; } = new();
		public StringBuilder PropertyDeclarations { get; set; } = new();
		public StringBuilder MethodsBuilder { get; set; } = new();
		public StringBuilder SaveMethods { get; set; } = new();
		public StringBuilder InterfaceMethods { get; set; } = new();
		public StringBuilder ServiceRegistrations { get; set; } = new();
	}
}
