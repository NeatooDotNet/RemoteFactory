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
	public static int? MaxHintNameLength { get; set; }
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{

		// Register the source output
		context.RegisterSourceOutput(context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (s, _) => IsClassSyntaxTargetForGeneration(s),
			transform: static (ctx, _) => GetClassSemanticTargetForGeneration(ctx))
			.Where(static m => m is not null),
			static (ctx, source) =>
			{
				if (source!.Value.classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
				{
					GenerateExecute(ctx, source!.Value.classDeclaration, source.Value.semanticModel);
				}
				else
				{
					GenerateFactory(ctx, source!.Value.classDeclaration, source.Value.semanticModel);
				}
			});

		context.RegisterSourceOutput(context.SyntaxProvider.CreateSyntaxProvider(
				predicate: static (s, _) => IsInterfaceSyntaxTargetForGeneration(s),
				transform: static (ctx, _) => GetInterfaceSemanticTargetForGeneration(ctx))
				.Where(static m => m is not null),
				static (ctx, source) =>
				{
					GenerateInterfaceFactory(ctx, source!.Value.interfaceSyntax, source.Value.semanticModel);
				});
	}

	public static bool IsClassSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax classDeclarationSyntax
				 && !(classDeclarationSyntax.TypeParameterList?.Parameters.Any() ?? false || classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword))
				 && !(classDeclarationSyntax.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "SuppressFactory"));

	public static (ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)? GetClassSemanticTargetForGeneration(GeneratorSyntaxContext context)
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
			return (classDeclaration, context.SemanticModel);
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

	public static bool IsInterfaceSyntaxTargetForGeneration(SyntaxNode node) => node is InterfaceDeclarationSyntax interfaceDeclarationSyntax
			 && !(interfaceDeclarationSyntax.TypeParameterList?.Parameters.Any() ?? false);
	public static (InterfaceDeclarationSyntax interfaceSyntax, SemanticModel semanticModel)? GetInterfaceSemanticTargetForGeneration(GeneratorSyntaxContext context)
	{
		var interfaceDeclaration = (InterfaceDeclarationSyntax)context.Node;

		var interfaceSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclaration);

		if (interfaceSymbol == null)
		{
			return null;
		}

		if (ClassOrBaseClassHasAttribute(interfaceSymbol, "SuppressFactory") != null)
		{
			return null;
		}

		if (ClassOrBaseClassHasAttribute(interfaceSymbol, "FactoryAttribute") != null)
		{
			return (interfaceDeclaration, context.SemanticModel);
		}

		return null;
	}

	private static List<FactoryOperation> factorySaveOperationAttributes = [.. Enum.GetValues(typeof(FactoryOperation)).Cast<FactoryOperation>().Where(v => ((int)v & (int)AuthorizeFactoryOperation.Write) != 0)];

	internal class CallFactoryMethod : CallMethod
	{
		public CallFactoryMethod(FactoryOperation factoryOperation, IMethodSymbol methodSymbol, BaseMethodDeclarationSyntax methodSyntax) : base(methodSymbol, methodSyntax)
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
		}

		public List<CallAuthorizeFactoryMethod> AuthCallMethods { get; set; } = [];
		public override string NamePostfix => this.Name.Replace(this.FactoryOperation.ToString() ?? "", "");
		public bool IsConstructor { get; set; } = false;
		public FactoryOperation FactoryOperation { get; private set; }
		public bool IsSave { get; private set; }
		public bool IsStaticFactory { get; } = false;
	}

	internal class CallAuthorizeFactoryMethod : CallMethod
	{
		public CallAuthorizeFactoryMethod(AuthorizeFactoryOperation authorizeFactoryOperation, IMethodSymbol methodSymbol, BaseMethodDeclarationSyntax methodDeclarationSyntax) : base(methodSymbol, methodDeclarationSyntax)
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

				var callParameter = this.Parameters.GetEnumerator();
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

	internal abstract class CallMethod
	{
		protected CallMethod(IMethodSymbol methodSymbol, BaseMethodDeclarationSyntax methodSyntax)
		{
			this.MethodSymbol = methodSymbol;
			var otherAttributes = methodSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).Where(a => a != null).ToList();

			this.Name = methodSymbol.Name;
			this.ClassName = methodSymbol.ContainingType.Name;
			this.IsBool = methodSymbol.ReturnType.ToString().Contains("bool");
			this.IsRemote = otherAttributes.Any(a => a == "Remote");

			this.ReturnTypeName = methodSymbol.ReturnType.ToString();
			this.IsNullable = methodSymbol.ReturnType.NullableAnnotation == NullableAnnotation.Annotated;

			if (methodSymbol.ReturnType is INamedTypeSymbol returnTypeSymbol && returnTypeSymbol.Name == "Task")
			{
				this.IsTask = true;
				if (returnTypeSymbol.IsGenericType)
				{
					this.IsNullable = returnTypeSymbol.TypeArguments.Any(t => t.NullableAnnotation == NullableAnnotation.Annotated);
					this.ReturnTypeName = returnTypeSymbol.TypeArguments.FirstOrDefault()?.ToString() ?? "void";
				}
			}

			this.ReturnTypeName = this.ReturnTypeName.TrimEnd('?');

			if (methodSyntax.ParameterList is ParameterListSyntax parameterListSyntax)
			{
				this.Parameters = [.. parameterListSyntax.Parameters.Select(p => new ParameterInfo(p, methodSymbol))];
			}
			else
			{
				this.Parameters = [];
			}
		}

		public string Name { get; set; }
		public string ClassName { get; set; }
		public virtual string NamePostfix => this.Name;
		public bool IsNullable { get; protected set; } = false;
		public bool IsBool { get; private set; }
		public bool IsTask { get; private set; }
		public bool IsRemote { get; protected set; }
		public IMethodSymbol MethodSymbol { get; protected set; }
		public string? ReturnTypeName { get; protected set; }
		public List<ParameterInfo> Parameters { get; private set; }
		public List<AspAuthorizeCall> AspAuthorizeCalls { get; set; } = [];
	}

	/// <summary>
	/// Insert, Update and Delete
	/// </summary>
	internal class WriteFactoryMethod : ReadFactoryMethod
	{
		public WriteFactoryMethod(string targetType, string concreteType, CallFactoryMethod callFactoryMethod) : base(targetType, concreteType, callFactoryMethod)
		{
			this.Parameters.Insert(0, new ParameterInfo() { Name = "target", Type = $"{targetType}", IsService = false, IsTarget = true });
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
			this.AuthCallMethods.AddRange(writeFactoryMethods.SelectMany(m => m.AuthCallMethods).Distinct());
			this.AspAuthorizeCalls.AddRange(writeFactoryMethods.SelectMany(m => m.AspAuthorizeCalls).Distinct());
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
		public ReadFactoryMethod(string serviceType, string implementationType, CallFactoryMethod callMethod) : base(serviceType, implementationType)
		{
			this.ImplementationType = implementationType;
			this.CallFactoryMethod = callMethod;
			this.Name = callMethod.Name;
			this.UniqueName = callMethod.Name;
			this.NamePostfix = callMethod.NamePostfix;
			this.FactoryOperation = callMethod.FactoryOperation;
			this.Parameters = callMethod.Parameters;
			this.AuthCallMethods.AddRange(callMethod.AuthCallMethods);
			this.AspAuthorizeCalls = callMethod.AspAuthorizeCalls;
		}
		public override bool IsSave => this.CallFactoryMethod.IsSave;
		public override bool IsBool => this.CallFactoryMethod.IsBool;
		public override bool IsTask => this.IsRemote || this.CallFactoryMethod.IsTask || this.AuthCallMethods.Any(m => m.IsTask) || this.AspAuthorizeCalls.Count > 0;
		public override bool IsRemote => this.CallFactoryMethod.IsRemote || this.AuthCallMethods.Any(m => m.IsRemote) || this.AspAuthorizeCalls.Count > 0;
		public override bool IsAsync => (this.HasAuth && this.CallFactoryMethod.IsTask) || this.AuthCallMethods.Any(m => m.IsTask) || this.AspAuthorizeCalls.Count > 0;
		public override bool IsNullable => this.CallFactoryMethod.IsNullable || this.HasAuth || this.IsBool;

		public CallFactoryMethod CallFactoryMethod { get; set; }

		public virtual string DoFactoryMethodCall()
		{
			var methodCall = "DoFactoryMethodCall";

			if (this.CallFactoryMethod.IsBool)
			{
				methodCall += "Bool";
			}

			if (this.CallFactoryMethod.IsTask)
			{
				methodCall += "Async";
				if (this.CallFactoryMethod.IsNullable)
				{
					methodCall += "Nullable";
				}
			}

			if (this.CallFactoryMethod.IsConstructor)
			{
				methodCall = $"{methodCall}(FactoryOperation.{this.FactoryOperation}, () => new {this.ImplementationType}({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";
			}
			else if (this.CallFactoryMethod.IsStaticFactory)
			{
				methodCall = $"{methodCall}(FactoryOperation.{this.FactoryOperation}, () => {this.ImplementationType}.{this.Name}({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";
			}
			else
			{
				methodCall = $"{methodCall}(target, FactoryOperation.{this.FactoryOperation}, () => target.{this.Name} ({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";
			}

			if (this.IsAsync && this.CallFactoryMethod.IsTask)
			{
				methodCall = $"await {methodCall}";
			}

			if (this.HasAuth)
			{
				methodCall = $"new Authorized<{this.ServiceType}>({methodCall})";
			}

			if (!this.CallFactoryMethod.IsTask && this.IsTask && !this.IsAsync)
			{
				methodCall = $"Task.FromResult({methodCall})";
			}

			return methodCall;
		}

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = base.LocalMethodStart();

			if (!this.CallFactoryMethod.IsConstructor && !this.CallFactoryMethod.IsStaticFactory)
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
		public InterfaceFactoryMethod(string serviceType, string implementationType, CallFactoryMethod callMethod) : base(serviceType, implementationType, callMethod)
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
		public override bool IsNullable => this.CallFactoryMethod.IsNullable;

		public override string ReturnType(bool includeTask = true, bool includeAuth = true, bool includeBool = true) => base.ReturnType(includeTask, false, includeBool);

		public override string DoFactoryMethodCall()
		{
			var methodCall = $"target.{this.Name} ({this.ParameterIdentifiersText(includeServices: false, includeTarget: false)})";

			if (this.IsAsync && this.CallFactoryMethod.IsTask)
			{
				methodCall = $"await {methodCall}";
			}

			if (!this.CallFactoryMethod.IsTask && this.IsTask && !this.IsAsync)
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
		public CanFactoryMethod(string targetType, string concreteType, string methodName, ICollection<CallAuthorizeFactoryMethod> authMethods, ICollection<AspAuthorizeCall> aspAuthorizeCalls) : this(targetType, concreteType, methodName)
		{
			this.Parameters = authMethods.SelectMany(m => m.Parameters).Distinct().ToList();
			this.AuthCallMethods.AddRange(authMethods);
			this.AspAuthorizeCalls.AddRange(aspAuthorizeCalls);
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

		public List<CallAuthorizeFactoryMethod> AuthCallMethods { get; set; } = [];
		public List<AspAuthorizeCall> AspAuthorizeCalls { get; set; } = [];
		public virtual bool HasAuth => this.AuthCallMethods.Count > 0 || this.AspAuthorizeCalls.Count > 0;
		public bool AspForbid { get; set; } = false; // If true, the method will return a 403 Forbidden response if authorization fails
		public FactoryOperation FactoryOperation { get; set; }
		public List<ParameterInfo> Parameters { get; set; } = null!;

		public virtual bool IsSave => false;
		public virtual bool IsBool => false;
		public virtual bool IsNullable => false;// (this.IsBool || this.IsSave) && (!this.IsSave || !this.HasAuth);
		public virtual bool IsTask => this.IsRemote || this.AuthCallMethods.Any(m => m.IsTask) || this.AspAuthorizeCalls.Count > 0;
		public virtual bool IsRemote => this.AuthCallMethods.Any(m => m.IsRemote) || this.AspAuthorizeCalls.Count > 0;
		public virtual bool IsAsync => this.AuthCallMethods.Any(m => m.IsTask) || this.AspAuthorizeCalls.Count > 0;

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

			if (this.AuthCallMethods.Count > 0 || this.AspAuthorizeCalls.Count > 0)
			{
				methodBuilder.AppendLine("Authorized authorized;");
				foreach (var authClass in this.AuthCallMethods.GroupBy(m => m.ClassName))
				{
					methodBuilder.AppendLine($"{authClass.Key} {authClass.Key.ToLower()} = ServiceProvider.GetRequiredService<{authClass.Key}>();");
					foreach (var authMethod in authClass)
					{
						authMethod.MakeAuthCall(this, methodBuilder);
					}
				}

				if (this.AspAuthorizeCalls.Count > 0)
				{
					methodBuilder.AppendLine($"var aspAuthorized = ServiceProvider.GetRequiredService<IAspAuthorize>();");

					var aspAuthorizeDataText = string.Join(", ", this.AspAuthorizeCalls.Select(a => a.ToAspAuthorizedDataText()));
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

	public class ParameterInfo
	{
		public ParameterInfo() { }

		public ParameterInfo(ParameterSyntax parameterSyntax, IMethodSymbol methodSymbol)
		{
			this.Name = parameterSyntax.Identifier.Text;
			this.Type = parameterSyntax.Type!.ToFullString();
			this.ParameterSymbol = methodSymbol.Parameters.Where(mp => mp.Name == parameterSyntax.Identifier.Text).FirstOrDefault();
			this.IsService = parameterSyntax.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.ToFullString() == "Service");
		}

		public string Name { get; set; } = null!;
		public string Type { get; set; } = null!;
		public IParameterSymbol? ParameterSymbol { get; set; } = null!;
		public bool IsService { get; set; }
		public bool IsTarget { get; set; }

		public override bool Equals(object obj)
		{
			return obj is ParameterInfo info &&
						 this.Name == info.Name &&
						 this.Type == info.Type;
		}

		override public int GetHashCode() => (this.Name, this.Type).GetHashCode();
	}

	public class AspAuthorizeCall
	{
		List<string> ConstructorArguments = [];
		List<string> NamedArguments = [];

		public AspAuthorizeCall(AttributeData attribute)
		{
			var attributeSyntax = attribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

			foreach (var attributeArgument in attributeSyntax?.ArgumentList?.Arguments ?? [])
			{
				var argumentText = attributeArgument.ToString();
				if (argumentText.Contains("="))
				{
					this.NamedArguments.Add(argumentText);
				}
				else
				{
					this.ConstructorArguments.Add(argumentText);
				}
			}
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

	private static void GenerateFactory(SourceProductionContext context, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
	{
		var messages = new List<string>();
		string source;
		var usingDirectives = new List<string>() { "using Neatoo.RemoteFactory;",
																 "using Neatoo.RemoteFactory.Internal;",
																 "using Microsoft.Extensions.DependencyInjection;" };

		try
		{

			var concreteSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax) ?? throw new Exception($"Cannot get named symbol for {classDeclarationSyntax}");
			var methodNames = new List<string>();
			var targetClassName = classDeclarationSyntax.Identifier.Text;
			var targetType = $"{targetClassName}";
			var targetConcreteType = $"{targetClassName}";
			var factoryText = new FactoryText();
			var typeSymbol = concreteSymbol;

			var interfaceSymbol = concreteSymbol.Interfaces.FirstOrDefault(i => i.Name == $"I{concreteSymbol.Name}");
			if (interfaceSymbol != null)
			{
				targetType = $"I{concreteSymbol.Name}";
				typeSymbol = interfaceSymbol;
				messages.Add($"Interface Found. TargetType: {targetType} ConcreteType: {concreteSymbol.Name}");
			}
			else
			{
				messages.Add($"No Interface Found. TargetType: {targetType}");
			}

			// Generate the source code for the found method
			var namespaceName = FindNamespace(classDeclarationSyntax) ?? "MissingNamespace";

			try
			{
				UsingStatements(usingDirectives, classDeclarationSyntax, semanticModel, namespaceName, messages);

				messages.Add($"Class: {concreteSymbol.ToDisplayString()} Name: {concreteSymbol.Name}");
				var typeMethods = GetMethodsRecursive(concreteSymbol);
				var typeFactoryMethods = TypeFactoryMethods(typeSymbol, typeMethods, [], messages);
				var typeAuthMethods = TypeAuthMethods(semanticModel, concreteSymbol, messages);

				var factoryMethods = new List<FactoryMethod>();

				MatchAuthMethods(typeFactoryMethods, typeAuthMethods, messages);

				foreach (var targetCallMethod in typeFactoryMethods)
				{
					if (targetCallMethod.IsSave)
					{
						factoryMethods.Add(new WriteFactoryMethod(targetType, targetConcreteType, targetCallMethod));
					}
					else
					{
						factoryMethods.Add(new ReadFactoryMethod(targetType, targetConcreteType, targetCallMethod));
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

							factoryMethods.Add(new SaveFactoryMethod(nameOverride, targetType, targetConcreteType, [.. byNameMethod]));
						}
					}
					else
					{
						factoryMethods.Add(new SaveFactoryMethod(nameOverride, targetType, targetConcreteType, [.. writeMethodGroup]));
					}
				}

				foreach (var factoryMethod in factoryMethods.ToList())
				{
					if (factoryMethod.HasAuth && !factoryMethod.AuthCallMethods.Any(m => m.Parameters.Any(p => p.IsTarget)))
					{
						var canMethod = new CanFactoryMethod(targetType, targetConcreteType, factoryMethod.Name, factoryMethod.AuthCallMethods, factoryMethod.AspAuthorizeCalls);

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
				if (factoryMethods.OfType<ReadFactoryMethod>().Any(f => !(f.CallFactoryMethod.IsConstructor || f.CallFactoryMethod.IsStaticFactory)))
				{
					factoryText.ServiceRegistrations.AppendLine($@"services.AddTransient<{targetConcreteType}>();");
					if (targetType != targetConcreteType)
					{
						factoryText.ServiceRegistrations.AppendLine($@"services.AddTransient<{targetType}, {targetConcreteType}>();");
					}
				}

				var editText = "";
				if (hasDefaultSave)
				{
					editText = "Save";
					factoryText.ServiceRegistrations.AppendLine($@"services.AddScoped<IFactorySave<{targetClassName}>, {targetClassName}Factory>();");
				}

				source = $@"
						  #nullable enable

                    {WithStringBuilder(usingDirectives)}

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
*/
                    namespace {namespaceName}
                    {{

                        public interface I{targetClassName}Factory
                        {{
                    {factoryText.InterfaceMethods}
                        }}

                        internal class {targetClassName}Factory : Factory{editText}Base<{targetType}>{(hasDefaultSave ? $", IFactorySave<{targetClassName}>" : "")}, I{targetClassName}Factory
                        {{

                            private readonly IServiceProvider ServiceProvider;  
                            private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

                    // Delegates
                    {factoryText.Delegates}
                    // Delegate Properties to provide Local or Remote fork in execution
                    {factoryText.PropertyDeclarations}

                            public {targetClassName}Factory(IServiceProvider serviceProvider, IFactoryCore<{targetType}> factoryCore) : base(factoryCore)
                            {{
                                    this.ServiceProvider = serviceProvider;
                                    {factoryText.ConstructorPropertyAssignmentsLocal}
                            }}

                            public {targetClassName}Factory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<{targetType}> factoryCore) : base(factoryCore)
                            {{
                                    this.ServiceProvider = serviceProvider;
                                    this.MakeRemoteDelegateRequest = remoteMethodDelegate;
                                    {factoryText.ConstructorPropertyAssignmentsRemote}
                            }}

                    {factoryText.MethodsBuilder}
                    {factoryText.SaveMethods}

                            public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
                            {{
                                services.AddScoped<{targetClassName}Factory>();
                                services.AddScoped<I{targetClassName}Factory, {targetClassName}Factory>();
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

			context.AddSource($"{SafeHintName(semanticModel, $"{namespaceName}.{targetClassName}")}Factory.g.cs", source);
		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NT0004", "Error", ex.Message, "FactoryGenerator.GenerateFactory", DiagnosticSeverity.Error, true), Location.None));
		}

	}

	private static void GenerateExecute(SourceProductionContext context, ClassDeclarationSyntax typeSyntax, SemanticModel semanticModel)
	{
		var messages = new List<string>();
		string source;
		var usingDirectives = new List<string>() { "using Neatoo.RemoteFactory;",
																 "using Neatoo.RemoteFactory.Internal;",
																 "using Microsoft.Extensions.DependencyInjection;" };

		try
		{
			var typeSymbol = semanticModel.GetDeclaredSymbol(typeSyntax) ?? throw new Exception($"Cannot get named symbol for {typeSyntax}");
			var methodNames = new List<string>();
			var delegates = new StringBuilder();
			var remoteMethods = new StringBuilder();
			var localMethods = new StringBuilder();
			var typeName = typeSyntax.Identifier.Text;

			// Generate the source code for the found method
			var namespaceName = FindNamespace(typeSyntax) ?? "MissingNamespace";

			try
			{
				UsingStatements(usingDirectives, typeSyntax, semanticModel, namespaceName, messages);

				messages.Add($"Static Class: {typeSymbol.ToDisplayString()} Name: {typeSymbol.Name}");

				if (!typeSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
				{
					messages.Add($"Class {typeName} is not partial. Cannot generate factory.");
					return;
				}

				var typeMethods = GetMethodsRecursive(typeSymbol);

				foreach (var method in typeMethods)
				{
					if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax methodSyntax)
					{
						messages.Add($"No MethodDeclarationSyntax for {method.Name}");
						continue;
					}

					if (!methodSyntax.ReturnType.ToString().StartsWith("Task"))
					{
						messages.Add($"{method.Name} skipped. Method must return Task");
						continue;
					}

					var executeAttribute = method.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ExecuteAttribute");

					if (executeAttribute != null)
					{
						//INamedTypeSymbol? delegateSymbol = method;

						//if (executeAttribute.AttributeClass?.TypeArguments.Length == 1)
						//{
						//	delegateSymbol = executeAttribute.AttributeClass.TypeArguments[0];
						//}


						//if (delegateSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not DelegateDeclarationSyntax delegateSyntax)
						//{
						//	messages.Add($"No DelegateDeclarationSyntax for {delegateSymbol.Name}");
						//	continue;
						//}

						if (semanticModel.GetTypeInfo(methodSyntax.ReturnType).Type is not INamedTypeSymbol returnTypeSymbol)
						{
							messages.Add($"No INamedTypeSymbol for {methodSyntax.Identifier}");
							continue;
						}

						if (!returnTypeSymbol.OriginalDefinition.ToDisplayString().EndsWith("Task<TResult>"))
						{
							messages.Add($"{method.Name} skipped. Delegates must return Task not {returnTypeSymbol.OriginalDefinition.ToDisplayString()}");
							continue;
						}

						var returnType = returnTypeSymbol.TypeArguments[0].ToString();
						var isNullable = returnTypeSymbol.TypeArguments[0].NullableAnnotation == NullableAnnotation.Annotated;
						var nullableText = isNullable ? "Nullable" : "";

						var delegateName = methodSyntax.Identifier.Text;

						if (delegateName.StartsWith("Execute"))
						{
							delegateName = delegateName.Substring("Execute".Length);
						}
						if (delegateName.StartsWith("_"))
						{
							delegateName = delegateName.Substring(1);
						}

						var parameters = methodSyntax.ParameterList.Parameters.Select(p => new ParameterInfo(p, method)).ToList();

						var parameterDeclarations = string.Join(", ", parameters.Where(p => !p.IsService)
																		.Select(p => $"{p.Type} {p.Name}"));
						var parameterIdentifiers = string.Join(", ", parameters.Where(p => !p.IsService).Select(p => p.Name));
						var allParameterIdentifiers = string.Join(", ", parameters.Select(p => p.Name));
						var serviceAssignmentsText = WithStringBuilder(parameters.Where(p => p.IsService).Select(p => $"var {p.Name} = cc.GetRequiredService<{p.Type}>();"));

						delegates.AppendLine($"public delegate {methodSyntax.ReturnType} {delegateName}({parameterDeclarations});");

						remoteMethods.AppendLine(@$"
						  services.AddTransient<{typeName}.{delegateName}>(cc =>
						  {{
								return ({parameterIdentifiers}) => cc.GetRequiredService<IMakeRemoteDelegateRequest>().ForDelegate{nullableText}<{returnType}>(typeof({typeName}.{delegateName}), [{parameterIdentifiers}]);
						  }});");

						localMethods.AppendLine(@$"
						  services.AddTransient<{typeName}.{delegateName}>(cc =>
						  {{
								return ({parameterDeclarations}) => {{
								{serviceAssignmentsText}
								return {typeName}.{method.Name}({allParameterIdentifiers});
							}};
						  }});");

					}
				}

				var partialClassSignature = typeSyntax.ToFullString().Substring(typeSyntax.Modifiers.FullSpan.Start - typeSyntax.FullSpan.Start, typeSyntax.Identifier.FullSpan.End - typeSyntax.Modifiers.FullSpan.Start).Trim();

				source = $@"
						  #nullable enable

                    {WithStringBuilder(usingDirectives)}

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
*/
                    namespace {namespaceName}
                    {{

								 {partialClassSignature} {{

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

			context.AddSource($"{SafeHintName(semanticModel, $"{namespaceName}.{typeSyntax.Identifier.Text}")}Factory.g.cs", source);
		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("NT0002", "Error", ex.Message, "FactoryGenerator.GenerateExecute", DiagnosticSeverity.Error, true), Location.None));
		}

	}

	private static void GenerateInterfaceFactory(SourceProductionContext context, InterfaceDeclarationSyntax interfaceSyntax, SemanticModel semanticModel)
	{
		var messages = new List<string>();
		var source = string.Empty;
		var usingDirectives = new List<string>() { "using Neatoo.RemoteFactory;",
																 "using Neatoo.RemoteFactory.Internal;",
																 "using Microsoft.Extensions.DependencyInjection;" };

		try
		{
			var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceSyntax) ?? throw new Exception($"Cannot get named symbol for {interfaceSyntax}");
			var methodNames = new List<string>();
			var serviceTypeName = interfaceSyntax.Identifier.Text;
			var implementationTypeName = serviceTypeName.TrimStart('I');
			var factoryText = new FactoryText();

			// Generate the source code for the found method
			var namespaceName = FindNamespace(interfaceSyntax) ?? "MissingNamespace";

			try
			{
				UsingStatements(usingDirectives, interfaceSyntax, semanticModel, namespaceName, messages);

				messages.Add($"Class: {interfaceSymbol.ToDisplayString()} Name: {interfaceSymbol.Name}");
				var typeMethods = GetMethodsRecursive(interfaceSymbol);
				var typeFactoryMethods = TypeFactoryMethods(interfaceSymbol, typeMethods, [FactoryOperation.Execute], messages);
				var typeAuthMethods = TypeAuthMethods(semanticModel, interfaceSymbol, messages);

				MatchAuthMethods(typeFactoryMethods, typeAuthMethods, messages);

				var factoryMethods = new List<FactoryMethod>();

				foreach (var typeFactoryMethod in typeFactoryMethods)
				{
					factoryMethods.Add(new InterfaceFactoryMethod(typeFactoryMethod.ReturnTypeName!, serviceTypeName, typeFactoryMethod));
				}

				foreach (var factoryMethod in factoryMethods.ToList())
				{
					if (factoryMethod.HasAuth)
					{
						var canMethod = new CanFactoryMethod(serviceTypeName, implementationTypeName, factoryMethod.Name, factoryMethod.AuthCallMethods, factoryMethod.AspAuthorizeCalls);
						factoryMethods.Add(canMethod);
					}
				}

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

				                    {WithStringBuilder(usingDirectives)}

				/*
											READONLY - DO NOT EDIT!!!!
											Generated by Neatoo.RemoteFactory
				*/
				                    namespace {namespaceName}
				                    {{
												public interface {serviceTypeName}Factory : {serviceTypeName}
												{{
													 {factoryText.InterfaceMethods}
												}}

				                        internal class {implementationTypeName}Factory : {serviceTypeName}Factory
				                        {{

				                            private readonly IServiceProvider ServiceProvider;  
				                            private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

													  // Delegates
													  {factoryText.Delegates}
													  // Delegate Properties to provide Local or Remote fork in execution
													  {factoryText.PropertyDeclarations}

													 public {implementationTypeName}Factory(IServiceProvider serviceProvider)
													 {{
																this.ServiceProvider = serviceProvider;
																{factoryText.ConstructorPropertyAssignmentsLocal}
													 }}

													 public {implementationTypeName}Factory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate)
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
																	services.AddScoped<{serviceTypeName}, {implementationTypeName}Factory>();
																	services.AddScoped<{serviceTypeName}Factory, {implementationTypeName}Factory>();
															}}

															// On the server the Delegates are registered
															// {implementationTypeName}Factory is not used
															// {serviceTypeName} must be registered to actual implementation
															if(remoteLocal == NeatooFactory.Server)
															{{
																	services.AddScoped<{serviceTypeName}Factory, {implementationTypeName}Factory>();
																	services.AddScoped<{implementationTypeName}Factory>();
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

			context.AddSource($"{SafeHintName(semanticModel, $"{namespaceName}.{serviceTypeName}")}Factory.g.cs", source);
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

	private static List<CallFactoryMethod> TypeFactoryMethods(INamedTypeSymbol typeSymbol, List<IMethodSymbol> methods, List<FactoryOperation> defaultFactoryOperations, List<string> messages)
	{
		var callFactoryMethods = new List<CallFactoryMethod>();

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

			CallFactoryMethod factoryMethod;

			var attributes = methodSymbol.GetAttributes().ToList();
			var attributeNames = methodSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).ToList();

			attributeNames.AddRange(defaultFactoryOperations.Select(o => o.ToString()));

			var aspAuthorizeAttributes = attributes.Where(a => a.AttributeClass?.Name == "AspAuthorizeAttribute").ToList();
			List<AspAuthorizeCall> aspAuthorizeCalls = [];

			foreach (var aspAuthorizeAttribute in aspAuthorizeAttributes)
			{
				aspAuthorizeCalls.Add(new AspAuthorizeCall(aspAuthorizeAttribute));
				attributes.Remove(aspAuthorizeAttribute);
			}

			foreach (var attributeName in attributeNames.Where(a => a != null))
			{
				if (Enum.TryParse<FactoryOperation>(attributeName, out var factoryOperation))
				{
					if (methodSymbol.ReturnType.ToDisplayString().Contains(typeSymbol.Name))
					{
						if (methodType == typeSymbol.ToDisplayString())
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
								&& typeSymbol.TypeKind != TypeKind.Interface)
					{
						if (!methodSymbol.IsStatic || !typeSymbol.IsStatic)
						{
							messages.Add($"Ignoring {methodSymbol.Name}. Execute Operations must be a static method in a static class");
							continue;
						}
					}

					factoryMethod = new CallFactoryMethod(factoryOperation, methodSymbol, methodSyntax);
				}
				else
				{
					messages.Add($"Ignoring [{methodSymbol.Name}] method with attribute [{attributeName}]. Not a FactoryOperation attribute.");
					continue;
				}

				foreach (var targetParam in methodSymbol.Parameters.Where(p => p.Type == typeSymbol))
				{
					factoryMethod.Parameters.Where(p => p.Name == targetParam.Name).ToList().ForEach(p => p.IsTarget = true);
				}

				factoryMethod.AspAuthorizeCalls = aspAuthorizeCalls;

				callFactoryMethods.Add(factoryMethod);
			}
		}
		return callFactoryMethods;
	}

	private static List<CallAuthorizeFactoryMethod> TypeAuthMethods(SemanticModel semanticModel, INamedTypeSymbol typeSymbol, List<string> messages)
	{

		var authorizeAttribute = ClassOrBaseClassHasAttribute(typeSymbol, "AuthorizeFactoryAttribute");
		var callAuthMethods = new List<CallAuthorizeFactoryMethod>();

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

							callAuthMethods.Add(new CallAuthorizeFactoryMethod(authorizeFactoryOperation, methodSymbol, methodSyntax));
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

		return callAuthMethods;
	}

	private static void MatchAuthMethods(IEnumerable<CallFactoryMethod> factoryMethods, List<CallAuthorizeFactoryMethod> authMethods, List<string> messages)
	{
		if (factoryMethods is null) { throw new ArgumentNullException(nameof(factoryMethods)); }
		if (authMethods is null) { throw new ArgumentNullException(nameof(authMethods)); }
		if (messages is null) { throw new ArgumentNullException(nameof(messages)); }

		foreach (var method in factoryMethods)
		{
			foreach (var authMethod in authMethods)
			{
				if (((int?)authMethod.AuthorizeFactoryOperation & (int)method.FactoryOperation) != 0)
				{
					method.AuthCallMethods.Add(authMethod);
				}
			}
		}
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
		if(maxLength == null)
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