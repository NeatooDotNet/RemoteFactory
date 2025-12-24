using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator;

namespace Neatoo.RemoteFactory.FactoryGenerator;

[Generator(LanguageNames.CSharp)]
public class FactoryGenerator : IIncrementalGenerator
{
	public static long PredicateCount { get; set; } = 0;
	public static long TransformCount { get; set; } = 0;
	public static long GenerateCount { get; set; } = 0;

	public static int? MaxHintNameLength { get; set; }

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{

		var classesToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName("Neatoo.RemoteFactory.FactoryAttribute",
			predicate: static (s, _) => {
				PredicateCount++;
				return s is ClassDeclarationSyntax classDeclarationSyntax
														 && !(classDeclarationSyntax.TypeParameterList?.Parameters.Any() ?? false || classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword))
														 && !(classDeclarationSyntax.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "SuppressFactory"));
			},
			transform: static (ctx, _) =>
			{
				TransformCount++;
				var classDeclaration = (ClassDeclarationSyntax)ctx.TargetNode;
				var semanticModel = ctx.SemanticModel;
				return TransformClassFactory(classDeclaration, semanticModel);
			});

		context.RegisterSourceOutput(classesToGenerate, static (spc, typeInfo) =>
		{
			GenerateCount++;
			if (typeInfo.IsStatic)
			{
				GenerateExecute(spc, typeInfo);
			}
			else
			{
				GenerateFactory(spc, typeInfo);
			}
		});

		var interfacesToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName("Neatoo.RemoteFactory.FactoryAttribute",
										predicate: static (s, _) => s is InterfaceDeclarationSyntax interfaceDeclarationSyntax
																			 && !(interfaceDeclarationSyntax.TypeParameterList?.Parameters.Any() ?? false),
										transform: static (ctx, _) =>
										{
											var interfaceSyntax = (InterfaceDeclarationSyntax)ctx.TargetNode;
											var semanticModel = ctx.SemanticModel;
											return TransformInterfaceFactory(interfaceSyntax, semanticModel);
										});

		context.RegisterSourceOutput(interfacesToGenerate, static (spc, typeInfo) =>
		{
			GenerateInterfaceFactory(spc, typeInfo);
		});
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


	private static List<FactoryOperation> factorySaveOperationAttributes = [.. Enum.GetValues(typeof(FactoryOperation)).Cast<FactoryOperation>().Where(v => ((int)v & (int)AuthorizeFactoryOperation.Write) != 0)];


	internal record TypeInfo
	{
		public TypeInfo(TypeDeclarationSyntax syntax, INamedTypeSymbol symbol, SemanticModel semanticModel)
		{
			List<string> debugMessages = [];

			var serviceSymbol = symbol;

			this.Name = syntax.Identifier.Text;
			this.IsPartial = syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
			this.SignatureText = syntax.ToFullString().Substring(syntax.Modifiers.FullSpan.Start - syntax.FullSpan.Start, syntax.Identifier.FullSpan.End - syntax.Modifiers.FullSpan.Start).Trim();
			this.IsInterface = syntax is InterfaceDeclarationSyntax;
			this.IsStatic = symbol.IsStatic;

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

			this.AuthMethods = TypeAuthMethods(semanticModel, symbol, debugMessages);

			List<FactoryOperation> defaultFactoryOperations = [];

			if (this.IsInterface)
			{
				defaultFactoryOperations.Add(FactoryOperation.Execute);
			}

			this.FactoryMethods = new EquatableArray<TypeFactoryMethodInfo>([.. TypeFactoryMethods(serviceSymbol, methodSymbols, defaultFactoryOperations, this.AuthMethods.ToList(), debugMessages)]);

			this.SafeHintName = SafeHintName(semanticModel, $"{this.Namespace}.{this.Name}");
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
	}




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
	}

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
	/// Insert, Update and Delete
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
		public override bool IsNullable => this.WriteFactoryMethods.Any(m => m.FactoryOperation == RemoteFactory.FactoryGenerator.FactoryOperation.Delete || m.IsNullable);

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

				if (method.FactoryOperation == RemoteFactory.FactoryGenerator.FactoryOperation.Delete)
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

                                        {DoInsertUpdateDeleteMethodCall(this.WriteFactoryMethods.Where(s => s.FactoryOperation == RemoteFactory.FactoryGenerator.FactoryOperation.Delete).FirstOrDefault())};
                                    }}
                                    else if (target.IsNew)
                                    {{
                                        {DoInsertUpdateDeleteMethodCall(this.WriteFactoryMethods.Where(s => s.FactoryOperation == RemoteFactory.FactoryGenerator.FactoryOperation.Insert).FirstOrDefault())};
                                    }}
                                    else
                                    {{
                                         {DoInsertUpdateDeleteMethodCall(this.WriteFactoryMethods.Where(s => s.FactoryOperation == RemoteFactory.FactoryGenerator.FactoryOperation.Update).FirstOrDefault())};
                                    }}
                            ");

			methodBuilder.AppendLine("}");

			return methodBuilder;
		}
	}

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

	private static TypeInfo TransformClassFactory(ClassDeclarationSyntax syntax, SemanticModel semanticModel)
	{
		var symbol = semanticModel.GetDeclaredSymbol(syntax) ?? throw new Exception($"Cannot get named symbol for {syntax}");

		return new TypeInfo(syntax, symbol, semanticModel);
	}

	private static void GenerateFactory(SourceProductionContext context, TypeInfo typeInfo)
	{
		var messages = new List<string>();
		string source;

		try
		{
			var factoryText = new FactoryText();

			try
			{

				var factoryMethods = new List<FactoryMethod>();

				foreach (var targetCallMethod in typeInfo.FactoryMethods)
				{
					if (targetCallMethod.IsSave)
					{
						factoryMethods.Add(new WriteFactoryMethod(typeInfo.ServiceTypeName, typeInfo.ImplementationTypeName, targetCallMethod));
					}
					else
					{
						factoryMethods.Add(new ReadFactoryMethod(typeInfo.ServiceTypeName, typeInfo.ImplementationTypeName, targetCallMethod));
					}
				}

				var writeMethodsGrouped = factoryMethods
									 .OfType<WriteFactoryMethod>()
									 .Where(m => m.IsSave)
									 .GroupBy(m => string.Join(",", m.Parameters.Where(m => !m.IsTarget && !m.IsService)
																	 .Select(m => m.Type.ToString())))
									 .ToList();

				string? nameOverride = null;

				if (writeMethodsGrouped.Count == 1)
				{
					nameOverride = "Save";
				}

				foreach (var writeMethodGroup in writeMethodsGrouped)
				{
					if (writeMethodGroup.Count(m => m.FactoryOperation == FactoryOperation.Insert) > 1
						 || writeMethodGroup.Count(m => m.FactoryOperation == FactoryOperation.Update) > 1
						 || writeMethodGroup.Count(m => m.FactoryOperation == FactoryOperation.Delete) > 1)
					{
						var byName = writeMethodGroup.GroupBy(m => m.NamePostfix).ToList();

						foreach (var byNameMethod in byName)
						{
							if (byNameMethod.Count(m => m.FactoryOperation == FactoryOperation.Insert) > 1
									  || byNameMethod.Count(m => m.FactoryOperation == FactoryOperation.Update) > 1
									  || byNameMethod.Count(m => m.FactoryOperation == FactoryOperation.Delete) > 1)
							{
								messages.Add($"Multiple Insert/Update/Delete methods with the same name: {writeMethodGroup.First().Name}");
								break;
							}

							factoryMethods.Add(new SaveFactoryMethod(nameOverride, typeInfo.ServiceTypeName, typeInfo.ImplementationTypeName, [.. byNameMethod]));
						}
					}
					else
					{
						factoryMethods.Add(new SaveFactoryMethod(nameOverride, typeInfo.ServiceTypeName, typeInfo.ImplementationTypeName, [.. writeMethodGroup]));
					}
				}

				foreach (var factoryMethod in factoryMethods.ToList())
				{
					if (factoryMethod.HasAuth && !factoryMethod.AuthMethodInfos.Any(m => m.Parameters.Any(p => p.IsTarget)))
					{
						var canMethod = new CanFactoryMethod(typeInfo.ServiceTypeName, typeInfo.ImplementationTypeName, factoryMethod.Name, factoryMethod.AuthMethodInfos, factoryMethod.AspAuthorizeInfo);

						// For if there are two FactoryOperations on a single method
						if (!factoryMethods.Any(factoryMethods => factoryMethods.Name == canMethod.Name))
						{
							factoryMethods.Add(canMethod);
						}
					}
				}

				var hasDefaultSave = false;
				var defaultSaveMethod = factoryMethods.OfType<SaveFactoryMethod>()
									 .Where(s => s.Parameters.Where(p => !p.IsTarget && !p.IsService).Count() == 0 && s.Parameters.First().IsTarget)
									 .FirstOrDefault();
				if (defaultSaveMethod != null)
				{
					defaultSaveMethod.IsDefault = true;
					hasDefaultSave = true;
				}

				var methodNames = new List<string>();

				foreach (var method in factoryMethods.OrderBy(m => m.Parameters.Count).ToList())
				{
					if (methodNames.Contains(method.Name))
					{
						var count = 1;
						while (methodNames.Contains($"{method.UniqueName}{count}"))
						{
							count += 1;
						}
						method.UniqueName = $"{method.UniqueName}{count}";
					}
					methodNames.Add(method.UniqueName);
				}

				foreach (var factoryMethod in factoryMethods)
				{
					factoryMethod.AddFactoryText(factoryText);
				}

				// We only need the target registered if we do a fetch or create that is not the constructor
				if (factoryMethods.OfType<ReadFactoryMethod>().Any(f => !(f.CallMethod.IsConstructor || f.CallMethod.IsStaticFactory)))
				{
					factoryText.ServiceRegistrations.AppendLine($@"services.AddTransient<{typeInfo.ImplementationTypeName}>();");
					if (typeInfo.ServiceTypeName != typeInfo.ImplementationTypeName)
					{
						factoryText.ServiceRegistrations.AppendLine($@"services.AddTransient<{typeInfo.ServiceTypeName}, {typeInfo.ImplementationTypeName}>();");
					}
				}

				var editText = "";
				if (hasDefaultSave)
				{
					editText = "Save";
					factoryText.ServiceRegistrations.AppendLine($@"services.AddScoped<IFactorySave<{typeInfo.ImplementationTypeName}>, {typeInfo.ImplementationTypeName}Factory>();");
				}

				source = $@"
						  #nullable enable

                    {WithStringBuilder(typeInfo.UsingStatements)}

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
							Predicate Count: {PredicateCount}
							Transform Count: {TransformCount}
							Generate Count: {GenerateCount}
*/
                    namespace {typeInfo.Namespace}
                    {{

                        public interface I{typeInfo.ImplementationTypeName}Factory
                        {{
                    {factoryText.InterfaceMethods}
                        }}

                        internal class {typeInfo.ImplementationTypeName}Factory : Factory{editText}Base<{typeInfo.ServiceTypeName}>{(hasDefaultSave ? $", IFactorySave<{typeInfo.ImplementationTypeName}>" : "")}, I{typeInfo.ImplementationTypeName}Factory
                        {{

                            private readonly IServiceProvider ServiceProvider;  
                            private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

                    // Delegates
                    {factoryText.Delegates}
                    // Delegate Properties to provide Local or Remote fork in execution
                    {factoryText.PropertyDeclarations}

                            public {typeInfo.ImplementationTypeName}Factory(IServiceProvider serviceProvider, IFactoryCore<{typeInfo.ServiceTypeName}> factoryCore) : base(factoryCore)
                            {{
                                    this.ServiceProvider = serviceProvider;
                                    {factoryText.ConstructorPropertyAssignmentsLocal}
                            }}

                            public {typeInfo.ImplementationTypeName}Factory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<{typeInfo.ServiceTypeName}> factoryCore) : base(factoryCore)
                            {{
                                    this.ServiceProvider = serviceProvider;
                                    this.MakeRemoteDelegateRequest = remoteMethodDelegate;
                                    {factoryText.ConstructorPropertyAssignmentsRemote}
                            }}

                    {factoryText.MethodsBuilder}
                    {factoryText.SaveMethods}

                            public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
                            {{
                                services.AddScoped<{typeInfo.ImplementationTypeName}Factory>();
                                services.AddScoped<I{typeInfo.ImplementationTypeName}Factory, {typeInfo.ImplementationTypeName}Factory>();
                    {factoryText.ServiceRegistrations}
                            }}

                        }}
                    }}";
				source = source.Replace("[, ", "[");
				source = source.Replace("(, ", "(");
				source = source.Replace(", )", ")");
				source = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
			}
			catch (Exception ex)
			{
				source = @$"/* Error: {ex.GetType().FullName} {ex.Message} 

	{WithStringBuilder(messages)}
*/";

			}

			context.AddSource($"{typeInfo.SafeHintName}Factory.g.cs", source);
		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NT0004", "Error", ex.Message, "FactoryGenerator.GenerateFactory", DiagnosticSeverity.Error, true), Location.None));
		}

	}

	private static void GenerateExecute(SourceProductionContext context, TypeInfo typeInfo)
	{
		string source;
		List<string> messages = [];

		try
		{
			var delegates = new StringBuilder();
			var remoteMethods = new StringBuilder();
			var localMethods = new StringBuilder();

			try
			{

				if (!typeInfo.IsPartial)
				{
					messages.Add($"Class {typeInfo.Name} is not partial. Cannot generate factory.");
					return;
				}


				foreach (var method in typeInfo.FactoryMethods)
				{

					if (method.FactoryOperation == FactoryOperation.Execute)
					{

						if (!method.IsTask)
						{
							messages.Add($"{method.Name} skipped. Delegates must return Task not {method.ReturnType}");
							continue;
						}

						var delegateName = method.Name;

						if (delegateName.StartsWith("Execute"))
						{
							delegateName = delegateName.Substring("Execute".Length);
						}
						if (delegateName.StartsWith("_"))
						{
							delegateName = delegateName.Substring(1);
						}

						var nullableText = method.IsNullable ? "Nullable" : "";

						var parameters = method.Parameters;

						var parameterDeclarations = string.Join(", ", parameters.Where(p => !p.IsService)
																		.Select(p => $"{p.Type} {p.Name}"));
						var parameterIdentifiers = string.Join(", ", parameters.Where(p => !p.IsService).Select(p => p.Name));
						var allParameterIdentifiers = string.Join(", ", parameters.Select(p => p.Name));
						var serviceAssignmentsText = WithStringBuilder(parameters.Where(p => p.IsService).Select(p => $"var {p.Name} = cc.GetRequiredService<{p.Type}>();"));

						delegates.AppendLine($"public delegate Task<{method.ReturnType}> {delegateName}({parameterDeclarations});");

						remoteMethods.AppendLine(@$"
						  services.AddTransient<{typeInfo.Name}.{delegateName}>(cc =>
						  {{
								return ({parameterIdentifiers}) => cc.GetRequiredService<IMakeRemoteDelegateRequest>().ForDelegate{nullableText}<{method.ReturnType}>(typeof({typeInfo.Name}.{delegateName}), [{parameterIdentifiers}]);
						  }});");

						localMethods.AppendLine(@$"
						  services.AddTransient<{typeInfo.Name}.{delegateName}>(cc =>
						  {{
								return ({parameterDeclarations}) => {{
								{serviceAssignmentsText}
								return {typeInfo.Name}.{method.Name}({allParameterIdentifiers});
							}};
						  }});");

					}
				}

				var partialClassSignature =

				source = $@"
						  #nullable enable

                    {WithStringBuilder(typeInfo.UsingStatements)}

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
*/
                    namespace {typeInfo.Namespace}
                    {{

								 {typeInfo.SignatureText} {{

{delegates}

                            internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
                            {{
						if(remoteLocal == NeatooFactory.Remote)
						  {{
{remoteMethods}
}}

						if(remoteLocal == NeatooFactory.Logical || remoteLocal == NeatooFactory.Server)
						  {{
{localMethods}
                            }}

	                        }}
							  }}
						}}";


				source = source.Replace("[, ", "[");
				source = source.Replace("(, ", "(");
				source = source.Replace(", )", ")");
				source = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
			}
			catch (Exception ex)
			{
				source = @$"/* Error: {ex.GetType().FullName} {ex.Message} 

	{WithStringBuilder(messages)}
*/";

			}

			context.AddSource($"{typeInfo.SafeHintName}Factory.g.cs", source);
		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NT0002", "Error", ex.Message, "FactoryGenerator.GenerateExecute", DiagnosticSeverity.Error, true), Location.None));
		}

	}

	private static TypeInfo TransformInterfaceFactory(InterfaceDeclarationSyntax interfaceSyntax, SemanticModel semanticModel)
	{
		return new TypeInfo(interfaceSyntax, semanticModel.GetDeclaredSymbol(interfaceSyntax) ?? throw new Exception($"Cannot get named symbol for {interfaceSyntax}"), semanticModel);
	}

	private static void GenerateInterfaceFactory(SourceProductionContext context, TypeInfo typeInfo)
	{
		var messages = new EquatableArray<string>();
		var source = string.Empty;

		try
		{
			var factoryText = new FactoryText();

			try
			{

				var factoryMethods = new List<FactoryMethod>();

				foreach (var typeFactoryMethod in typeInfo.FactoryMethods)
				{
					factoryMethods.Add(new InterfaceFactoryMethod(typeFactoryMethod.ReturnType!, typeInfo.ServiceTypeName, typeFactoryMethod));
				}

				foreach (var factoryMethod in factoryMethods.ToList())
				{
					if (factoryMethod.HasAuth)
					{
						var canMethod = new CanFactoryMethod(typeInfo.ServiceTypeName, typeInfo.ImplementationTypeName, factoryMethod.Name, factoryMethod.AuthMethodInfos, factoryMethod.AspAuthorizeInfo);
						factoryMethods.Add(canMethod);
					}
				}

				var methodNames = new List<string>();

				foreach (var method in factoryMethods.OrderBy(m => m.Parameters.Count).ToList())
				{
					if (methodNames.Contains(method.Name))
					{
						var count = 1;
						while (methodNames.Contains($"{method.UniqueName}{count}"))
						{
							count += 1;
						}
						method.UniqueName = $"{method.UniqueName}{count}";
					}
					methodNames.Add(method.UniqueName);
				}

				foreach (var factoryMethod in factoryMethods)
				{
					factoryMethod.AddFactoryText(factoryText);
				}

				source = $@"
											  #nullable enable

					                    {WithStringBuilder(typeInfo.UsingStatements)}

					/*
												READONLY - DO NOT EDIT!!!!
												Generated by Neatoo.RemoteFactory
					*/
					                    namespace {typeInfo.Namespace}
					                    {{
													public interface {typeInfo.ServiceTypeName}Factory : {typeInfo.ServiceTypeName}
													{{
														 {factoryText.InterfaceMethods}
													}}

					                        internal class {typeInfo.ImplementationTypeName}Factory : {typeInfo.ServiceTypeName}Factory
					                        {{

					                            private readonly IServiceProvider ServiceProvider;  
					                            private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

														  // Delegates
														  {factoryText.Delegates}
														  // Delegate Properties to provide Local or Remote fork in execution
														  {factoryText.PropertyDeclarations}

														 public {typeInfo.ImplementationTypeName}Factory(IServiceProvider serviceProvider)
														 {{
																	this.ServiceProvider = serviceProvider;
																	{factoryText.ConstructorPropertyAssignmentsLocal}
														 }}

														 public {typeInfo.ImplementationTypeName}Factory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate)
														 {{
																	this.ServiceProvider = serviceProvider;
																	this.MakeRemoteDelegateRequest = remoteMethodDelegate;
																	{factoryText.ConstructorPropertyAssignmentsRemote}
														 }}

														{factoryText.MethodsBuilder}

					                            public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
					                            {{
																// On the client the Factory is registered
																// All method calls lead to a remote call
																if(remoteLocal == NeatooFactory.Remote)
																{{
																		services.AddScoped<{typeInfo.ServiceTypeName}, {typeInfo.ImplementationTypeName}Factory>();
																		services.AddScoped<{typeInfo.ServiceTypeName}Factory, {typeInfo.ImplementationTypeName}Factory>();
																}}

																// On the server the Delegates are registered
																// {typeInfo.ImplementationTypeName}Factory is not used
																// {typeInfo.ServiceTypeName} must be registered to actual implementation
																if(remoteLocal == NeatooFactory.Server)
																{{
																		services.AddScoped<{typeInfo.ServiceTypeName}Factory, {typeInfo.ImplementationTypeName}Factory>();
																		services.AddScoped<{typeInfo.ImplementationTypeName}Factory>();
																		{factoryText.ServiceRegistrations}
																}}
					                            }}
					                        }}
					                    }}";

				source = source.Replace("[, ", "[");
				source = source.Replace("(, ", "(");
				source = source.Replace(", )", ")");
				source = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
			}
			catch (Exception ex)
			{
				source = @$"/* Error: {ex.GetType().FullName} {ex.Message} 

					{WithStringBuilder(messages)}
	*/";

			}

			context.AddSource($"{typeInfo.SafeHintName}Factory.g.cs", source);
		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NT0004", "Error", ex.Message, "FactoryGenerator.GenerateInterfaceFactory", DiagnosticSeverity.Error, true), Location.None));
		}
	}
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

	private static List<TypeFactoryMethodInfo> TypeFactoryMethods(INamedTypeSymbol serviceSymbol, List<IMethodSymbol> methods, List<FactoryOperation> defaultFactoryOperations, List<TypeAuthMethodInfo> authMethods, List<string> messages)
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

			foreach (var attributeName in attributeNames.Where(a => a != null))
			{
				if (Enum.TryParse<FactoryOperation>(attributeName, out var factoryOperation))
				{
					if (methodSymbol.ReturnType.ToDisplayString().Contains(serviceSymbol.Name))
					{
						if (methodType == serviceSymbol.ToDisplayString())
						{
							if (((int?)factoryOperation & (int)RemoteFactory.FactoryGenerator.AuthorizeFactoryOperation.Read) == 0)
							{
								messages.Add($"Ignoring {methodSymbol.Name}, Only Fetch and Create methods can return the target type");
								continue;
							}

							if (!methodSymbol.IsStatic)
							{
								messages.Add($"Ignoring {methodSymbol.Name}; it must be static. Only static factories are allowed.");
								continue;
							}
						}
					}
					else if (factoryOperation == RemoteFactory.FactoryGenerator.FactoryOperation.Execute
								&& serviceSymbol.TypeKind != TypeKind.Interface)
					{
						if (!methodSymbol.IsStatic || !serviceSymbol.IsStatic)
						{
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
		}
		return callFactoryMethods;
	}

	private static EquatableArray<TypeAuthMethodInfo> TypeAuthMethods(SemanticModel semanticModel, INamedTypeSymbol typeSymbol, List<string> messages)
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

	public static string? FindNamespace(SyntaxNode syntaxNode)
	{
		if (syntaxNode.Parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
		{
			return namespaceDeclarationSyntax.Name.ToString();
		}
		else if (syntaxNode.Parent is FileScopedNamespaceDeclarationSyntax parentClassDeclarationSyntax)
		{
			return parentClassDeclarationSyntax.Name.ToString();
		}
		else if (syntaxNode.Parent != null)
		{
			return FindNamespace(syntaxNode.Parent);
		}
		else
		{
			return null;
		}
	}

	public static string WithStringBuilder(IEnumerable<string> strings)
	{
		var sb = new StringBuilder();
		foreach (var s in strings)
		{
			sb.AppendLine(s);
		}
		return sb.ToString();
	}

	public static string SafeHintName(SemanticModel semanticModel, string hintName, int? maxLength = null)
	{
		if (maxLength == null)
		{
			var hintNameLengthAttribute = semanticModel.Compilation.Assembly.GetAttributes()
				.Where(a => a.AttributeClass?.Name == "FactoryHintNameLengthAttribute")
				.FirstOrDefault();

			maxLength = hintNameLengthAttribute?.ConstructorArguments.FirstOrDefault().Value is int length ? length : 50;

		}

		if (hintName.Length > maxLength)
		{
			if (hintName.Contains('.'))
			{
				return SafeHintName(semanticModel, hintName.Substring(hintName.IndexOf('.') + 1, hintName.Length - hintName.IndexOf('.') - 1), maxLength);
			}
			else
			{
				return hintName.Substring(hintName.Length - maxLength.Value, maxLength.Value);
			}
		}
		return hintName;
	}
}