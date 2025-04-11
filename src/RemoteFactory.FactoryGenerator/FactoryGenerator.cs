using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;

namespace Neatoo.RemoteFactory.FactoryGenerator;

[Generator(LanguageNames.CSharp)]
public class FactoryGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context) =>
		// Register the source output
		context.RegisterSourceOutput(context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
			transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
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
				return (classDeclaration, context.SemanticModel);
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

	private static List<string> factorySaveOperationAttributes = [.. Enum.GetValues(typeof(FactoryOperation)).Cast<int>().Where(v => (v & (int)AuthorizeOperation.Write) != 0).Select(v => Enum.GetName(typeof(FactoryOperation), v))];

	internal class CallMethod
	{
		public CallMethod(AttributeData attribute, INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, ConstructorDeclarationSyntax constructorDeclarationSyntax, List<string> messages) : this(attribute, classSymbol, methodSymbol, constructorDeclarationSyntax.AttributeLists, constructorDeclarationSyntax.ParameterList, messages)
		{
			this.Name = this.FactoryOperation.ToString();
			this.IsConstructor = true;
		}

		public CallMethod(AttributeData attribute, INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, MethodDeclarationSyntax methodDeclarationSyntax, List<string> messages) : this(attribute, classSymbol, methodSymbol, methodDeclarationSyntax.AttributeLists, methodDeclarationSyntax.ParameterList, messages)
		{
			if (this.Ignore) { return; }

			var methodType = methodSymbol.ReturnType.ToDisplayString();

			if (methodType.Contains(@"Task<"))
			{
				methodType = Regex.Match(methodType, @"Task<(.*?)>").Groups[1].Value;
			}

			if (methodSymbol.IsStatic)
			{
				messages.Add($"Static Method {methodSymbol.Name} Method Return Type: {methodType} Class Type: {classSymbol.ToDisplayString()}");
			}

			if (methodSymbol.ReturnType.ToDisplayString().Contains(classSymbol.Name))
			{

				if (methodType.EndsWith("?"))
				{
					this.IsNullable = true;
					methodType = methodType.Substring(0, methodType.Length - 1);
				}

				if (methodType == classSymbol.ToDisplayString())
				{
					if (((int?)this.FactoryOperation & (int)RemoteFactory.FactoryGenerator.AuthorizeOperation.Read) == 0)
					{
						messages.Add($"Ignoring {this.Name}, Only Fetch and Create methods can return the target type");
						this.Ignore = true;
					}
					if (!methodSymbol.IsStatic)
					{
						messages.Add($"Ignoring {methodSymbol.Name}; it must be static. Only static factories are allowed.");
						this.Ignore = true;
					}

					this.IsStaticFactory = true;
				}

			}
			else if (this.AuthorizeOperation != null && !(methodType == "bool" || methodType == "string" || methodType == "string?"))
			{
				messages.Add($"Ignoring {methodSymbol.Name}; wrong return type of {methodType} for an Authorize method");
				this.Ignore = true;
			}
			else if (this.FactoryOperation == RemoteFactory.FactoryGenerator.FactoryOperation.Execute)
			{
				if (!methodSymbol.IsStatic || !classSymbol.IsStatic)
				{
					messages.Add($"Ignoring {methodSymbol.Name}. Execute Operations must be a static method in a static class");
					this.Ignore = true;
				}
			}
			//else if (this.FactoryOperation != null && !(methodType == "void" || methodType == "bool" || methodType == "System.Threading.Tasks.Task"))
			//{
			//	messages.Add($"Ignoring {methodSymbol.Name}; wrong return type of {methodType} for a Factory method");
			//	this.Ignore = true;
			//}
		}

		private CallMethod(AttributeData attribute_, INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, SyntaxList<AttributeListSyntax> attributeLists, ParameterListSyntax? parameterListSyntax, List<string> messages)
		{
			this.MethodSymbol = methodSymbol;
			this.AttributeData = attribute_;
			var otherAttributes = methodSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).Where(a => a != null).ToList();
			var attributeName = attribute_.AttributeClass?.Name.Replace("Attribute", "");

			this.Name = methodSymbol.Name;
			this.ClassName = methodSymbol.ContainingType.Name;
			this.IsBool = methodSymbol.ReturnType.ToString().Contains("bool");
			this.IsTask = methodSymbol.ReturnType.ToString().Contains("Task");
			this.IsRemote = otherAttributes.Any(a => a == "Remote");
			this.IsSave = factorySaveOperationAttributes.Contains(attributeName!);

			if (Enum.TryParse<FactoryOperation>(attributeName, out var dmm))
			{
				this.FactoryOperation = dmm;
			}
			else if (attributeName == "Authorize")
			{
				var attr = attributeLists.SelectMany(a => a.Attributes)
					 .Where(a => a.Name.ToString() == "Authorize")
					 .SingleOrDefault()?
					 .ArgumentList?.Arguments.ToFullString();

				var pattern = @"AuthorizeOperation\.(\w+)";

				// Use Regex.Matches to find all matches in the attr string
				var matches = Regex.Matches(attr, pattern);
				var authorizeOperationList = new List<AuthorizeOperation>();

				foreach (Match match in matches)
				{
					// Extract the matched value (e.g., "Read", "Write")
					var value = match.Groups[1].Value;

					// Try to parse the value into the AuthorizeOperation enum
					if (Enum.TryParse<AuthorizeOperation>(value, out var dmType))
					{
						// Successfully parsed the value into the AuthorizeOperation enum
						authorizeOperationList.Add(dmType);
					}
				}

				this.AuthorizeOperation = authorizeOperationList.Aggregate((a, b) => a | b);
			}
			else
			{
				this.Parameters = [];
				this.Ignore = true;
				messages.Add($"Ignoring [{methodSymbol.Name}] method with attribute [{attributeName}]. Not a Factory or Authorize attribute.");
				return;
			}

			//if (methodSymbol.IsGenericMethod)
			//{
			//    Parameters = methodSymbol.Parameters.Select(p => new ParameterInfo()
			//    {
			//        Name = p.Name,
			//        Type = p.Type.ToString(),
			//        IsService = p.GetAttributes().Any(a => a.AttributeClass?.Name.ToString() == "ServiceAttribute"),
			//    }).ToList();

			//    Parameters.ForEach(p =>
			//    {
			//        p.Type = Regex.Replace(p.Type, @"\w+\.", "");
			//        p.IsTarget = p.Type.ToString() == targetType;
			//    });
			//}
			//else
			//{
			if (parameterListSyntax != null)
			{
				this.Parameters = [.. parameterListSyntax.Parameters.Select(p => new ParameterInfo(p, methodSymbol))];
			}
			else
			{
				this.Parameters = [];
			}

			foreach (var targetParam in methodSymbol.Parameters.Where(p => p.Type == classSymbol))
			{
				this.Parameters.Where(p => p.Name == targetParam.Name).ToList().ForEach(p => p.IsTarget = true);
			}

		}


		public string Name { get; set; }
		public string ClassName { get; set; }
		public bool Ignore { get; set; }
		public string NamePostfix => this.Name.Replace(this.FactoryOperation?.ToString() ?? "", "");
		public bool IsConstructor { get; } = false;
		public bool IsStaticFactory { get; } = false;
		public bool IsNullable { get; } = false;
		public bool IsBool { get; private set; }
		public bool IsTask { get; private set; }
		public bool IsRemote { get; private set; }
		public bool IsSave { get; private set; }
		public IMethodSymbol MethodSymbol { get; protected set; }
		public AttributeData AttributeData { get; protected set; }
		public FactoryOperation? FactoryOperation { get; private set; }
		public AuthorizeOperation? AuthorizeOperation { get; private set; }
		public List<ParameterInfo> Parameters { get; private set; }

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
			methodBuilder.AppendLine("}");
		}
	}

	/// <summary>
	/// Insert, Update and Delete
	/// </summary>
	internal class WriteFactoryMethod : ReadFactoryMethod
	{
		public WriteFactoryMethod(string targetType, string concreteType, CallMethod callMethodInfo) : base(targetType, concreteType, callMethodInfo)
		{
			this.Parameters.Insert(0, new ParameterInfo() { Name = "target", Type = $"{targetType}", IsService = false, IsTarget = true });
			// this.TargetType = $"{targetType}?"; breaks auth
		}

		public override StringBuilder PublicMethod(FactoryText classText)
		{
			return new StringBuilder();
		}

		public override StringBuilder RemoteMethod(FactoryText classText)
		{
			return new StringBuilder();
		}

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = base.LocalMethodStart();

			methodBuilder.AppendLine($"var cTarget = ({this.ConcreteType}) target ?? throw new Exception(\"{this.TargetType} must implement {this.ConcreteType}\");");
			methodBuilder.AppendLine($"{this.ServiceAssignmentsText}");
			methodBuilder.AppendLine($"return {this.DoFactoryMethodCall.Replace("target", "cTarget")};");
			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("");

			return methodBuilder;
		}
	}

	internal class SaveFactoryMethod : FactoryMethod
	{
		public SaveFactoryMethod(string? nameOverride, string targetType, string concreteType, List<WriteFactoryMethod> writeFactoryMethods) : base(targetType, concreteType)
		{
			var writeFactoryMethod = writeFactoryMethods.OrderByDescending(w => w.FactoryOperation!).First();
			this.Name = $"Save{writeFactoryMethod.NamePostfix}";
			this.UniqueName = this.Name;
			this.WriteFactoryMethods = writeFactoryMethods;
			this.Parameters = writeFactoryMethods.First().Parameters;
			this.AuthCallMethods.AddRange(writeFactoryMethods.SelectMany(m => m.AuthCallMethods).Distinct());
		}

		public bool IsDefault { get; set; } = false;
		public override bool IsSave => true;
		public override bool IsRemote => this.WriteFactoryMethods.Any(m => m.IsRemote);
		public override bool IsTask => this.IsRemote || this.WriteFactoryMethods.Any(m => m.IsTask);
		public override bool IsAsync => this.WriteFactoryMethods.Any(m => m.IsTask);
		public override bool HasAuth => this.WriteFactoryMethods.Any(m => m.HasAuth);
		public override bool IsNullable => this.WriteFactoryMethods.Any(m => m.FactoryOperation == RemoteFactory.FactoryGenerator.FactoryOperation.Delete || m.IsNullable);

		public List<WriteFactoryMethod> WriteFactoryMethods { get; }

		public override StringBuilder PublicMethod(FactoryText classText)
		{
			if (!this.HasAuth)
			{
				return base.PublicMethod(classText);
			}

			var methodBuilder = new StringBuilder();

			var asyncKeyword = this.IsTask && this.HasAuth ? "async" : "";
			var awaitKeyword = this.IsTask && this.HasAuth ? "await" : "";

			classText.InterfaceMethods.AppendLine($"{this.ReturnType(includeAuth: false)} {this.Name}({this.ParameterDeclarationsText()});");

			methodBuilder.AppendLine($"public virtual {asyncKeyword} {this.ReturnType(includeAuth: false)} {this.Name}({this.ParameterDeclarationsText()})");
			methodBuilder.AppendLine("{");

			methodBuilder.AppendLine($"var authorized = ({awaitKeyword} Local{this.UniqueName}({this.ParameterIdentifiersText()}));");

			methodBuilder.AppendLine("if (!authorized.HasAccess)");
			methodBuilder.AppendLine("{");
			methodBuilder.AppendLine("throw new NotAuthorizedException(authorized);");
			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("return authorized.Result;");
			methodBuilder.AppendLine("}");

			classText.InterfaceMethods.AppendLine($"{this.ReturnType()} Try{this.Name}({this.ParameterDeclarationsText()});");

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

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = new StringBuilder();

			if (this.IsDefault)
			{
				methodBuilder.AppendLine($"async Task<IFactorySaveMeta?> IFactorySave<{this.ConcreteType}>.Save({this.ConcreteType} target)");
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

			var defaultReturn = $"default({this.TargetType})";
			if (this.HasAuth)
			{
				defaultReturn = $"new Authorized<{this.TargetType}>()";
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
					methodCall = $"new Authorized<{this.TargetType}>({methodCall})";
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
		public ReadFactoryMethod(string targetType, string concreteType, CallMethod callMethod) : base(targetType, concreteType)
		{
			this.ConcreteType = concreteType;
			this.CallMethod = callMethod;
			this.Name = callMethod.Name;
			this.UniqueName = callMethod.Name;
			this.NamePostfix = callMethod.NamePostfix;
			this.FactoryOperation = callMethod.FactoryOperation;
			this.AuthorizeOperation = callMethod.AuthorizeOperation;
			this.Parameters = callMethod.Parameters;
		}

		public override bool IsSave => this.CallMethod.IsSave;
		public override bool IsBool => this.CallMethod.IsBool;
		public override bool IsTask => this.IsRemote || this.CallMethod.IsTask || this.AuthCallMethods.Any(m => m.IsTask);
		public override bool IsRemote => this.CallMethod.IsRemote || this.AuthCallMethods.Any(m => m.IsRemote);
		public override bool IsAsync => (this.HasAuth && this.CallMethod.IsTask) || this.AuthCallMethods.Any(m => m.IsTask);
		public override bool IsNullable => this.CallMethod.IsNullable || this.HasAuth || this.IsBool;

		public CallMethod CallMethod { get; set; }

		public string DoFactoryMethodCall
		{
			get
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
					methodCall = $"{methodCall}(FactoryOperation.{this.FactoryOperation}, () => new {this.ConcreteType}({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";
				}
				else if (this.CallMethod.IsStaticFactory)
				{
					methodCall = $"{methodCall}(FactoryOperation.{this.FactoryOperation}, () => {this.ConcreteType}.{this.Name}({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";
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
					methodCall = $"new Authorized<{this.TargetType}>({methodCall})";
				}

				if (!this.CallMethod.IsTask && this.IsTask && !this.IsAsync)
				{
					methodCall = $"Task.FromResult({methodCall})";
				}

				return methodCall;
			}
		}

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = base.LocalMethodStart();

			if (!this.CallMethod.IsConstructor && !this.CallMethod.IsStaticFactory)
			{
				methodBuilder.AppendLine($"var target = ServiceProvider.GetRequiredService<{this.ConcreteType}>();");
			}

			methodBuilder.AppendLine($"{this.ServiceAssignmentsText}");
			methodBuilder.AppendLine($"return {this.DoFactoryMethodCall};");
			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("");

			return methodBuilder;
		}
	}

	internal class CanFactoryMethod : FactoryMethod
	{
		public CanFactoryMethod(string targetType, string concreteType, string methodName, ICollection<CallMethod> authMethods) : base(targetType, concreteType)
		{
			this.Name = $"Can{methodName}";
			this.UniqueName = this.Name;
			this.AuthCallMethods.AddRange(authMethods);
			this.Parameters = authMethods.SelectMany(m => m.Parameters).Distinct().ToList();
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

		public override StringBuilder PublicMethod(FactoryText classText)
		{

			classText.InterfaceMethods.AppendLine($"{this.ReturnType()} {this.Name}({this.ParameterDeclarationsText(includeServices: false)});");

			var methodBuilder = new StringBuilder();

			methodBuilder.AppendLine($"public virtual {this.ReturnType()} {this.Name}({this.ParameterDeclarationsText(includeServices: false)})");
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

	internal abstract class FactoryMethod(string targetType, string concreteType)
	{
		public string TargetType { get; protected set; } = targetType;
		public string ConcreteType { get; set; } = concreteType;
		public string Name { get; protected set; } = null!;
		public string UniqueName { get; set; } = null!;
		public string? NamePostfix { get; protected set; }
		public string DelegateName => $"{this.UniqueName}Delegate";

		public List<CallMethod> AuthCallMethods { get; set; } = [];
		public virtual bool HasAuth => this.AuthCallMethods.Count > 0;
		public FactoryOperation? FactoryOperation { get; set; }
		public AuthorizeOperation? AuthorizeOperation { get; set; }
		public List<ParameterInfo> Parameters { get; set; } = null!;
		public virtual bool IsSave => false;
		public virtual bool IsBool => false;
		public virtual bool IsNullable => false;// (this.IsBool || this.IsSave) && (!this.IsSave || !this.HasAuth);
		public virtual bool IsTask => this.IsRemote || this.AuthCallMethods.Any(m => m.IsTask);
		public virtual bool IsRemote => this.AuthCallMethods.Any(m => m.IsRemote);
		public virtual bool IsAsync => this.AuthCallMethods.Any(m => m.IsTask);
		public virtual string AsyncKeyword => this.IsAsync ? "async" : "";
		public virtual string AwaitKeyword => this.IsAsync ? "await" : "";
		public string ServiceAssignmentsText => WithStringBuilder(this.Parameters.Where(p => p.IsService).Select(p => $"var {p.Name} = ServiceProvider.GetRequiredService<{p.Type}>();"));
		public virtual string ReturnType(bool includeTask = true, bool includeAuth = true, bool includeBool = true)
		{
			var returnType = this.TargetType;

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

		public virtual StringBuilder PublicMethod(FactoryText classText)
		{
			var asyncKeyword = this.IsTask && this.HasAuth ? "async" : "";
			var awaitKeyword = this.IsTask && this.HasAuth ? "await" : "";

			classText.InterfaceMethods.AppendLine($"{this.ReturnType(includeAuth: false)} {this.Name}({this.ParameterDeclarationsText(includeServices: false)});");

			var methodBuilder = new StringBuilder();

			methodBuilder.AppendLine($"public virtual {asyncKeyword} {this.ReturnType(includeAuth: false)} {this.Name}({this.ParameterDeclarationsText(includeServices: false)})");
			methodBuilder.AppendLine("{");

			if (!this.HasAuth)
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

		public virtual StringBuilder RemoteMethod(FactoryText classText)
		{
			var methodBuilder = new StringBuilder();
			if (this.IsRemote)
			{
				var nullableText = this.ReturnType(includeTask: false).EndsWith("?") ? "Nullable" : "";

				classText.Delegates.AppendLine($"public delegate {this.ReturnType()} {this.DelegateName}({this.ParameterDeclarationsText()});");
				classText.PropertyDeclarations.AppendLine($"public {this.DelegateName} {this.UniqueName}Property {{ get; }}");
				classText.ConstructorPropertyAssignmentsLocal.AppendLine($"{this.UniqueName}Property = Local{this.UniqueName};");
				classText.ConstructorPropertyAssignmentsRemote.AppendLine($"{this.UniqueName}Property = Remote{this.UniqueName};");

				methodBuilder.AppendLine($"public virtual async {this.ReturnType()} Remote{this.UniqueName}({this.ParameterDeclarationsText()})");
				methodBuilder.AppendLine("{");
				methodBuilder.AppendLine($" return (await MakeRemoteDelegateRequest!.ForDelegate{nullableText}<{this.ReturnType(includeTask: false)}>(typeof({this.DelegateName}), [{this.ParameterIdentifiersText()}]))!;");
				methodBuilder.AppendLine("}");
				methodBuilder.AppendLine("");

				classText.ServiceRegistrations.AppendLine($@"services.AddScoped<{this.DelegateName}>(cc => {{
                                                    var factory = cc.GetRequiredService<{this.ConcreteType}Factory>();
                                                    return ({this.ParameterDeclarationsText()}) => factory.Local{this.UniqueName}({this.ParameterIdentifiersText()});
                                                }});");
			}
			return methodBuilder;
		}

		protected virtual StringBuilder LocalMethodStart()
		{
			var methodBuilder = new StringBuilder();

			methodBuilder.AppendLine($"public {this.AsyncKeyword} {this.ReturnType()} Local{this.UniqueName}({this.ParameterDeclarationsText()})");
			methodBuilder.AppendLine("{");

			if (this.AuthCallMethods.Count > 0)
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
			var targetSymbol = concreteSymbol;

			var interfaceSymbol = concreteSymbol.Interfaces.FirstOrDefault(i => i.Name == $"I{concreteSymbol.Name}");
			if (interfaceSymbol != null)
			{
				targetType = $"I{concreteSymbol.Name}";
				targetSymbol = interfaceSymbol;
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
				var concreteMethods = GetMethodsRecursive(concreteSymbol);
				var targetCallMethods = CreateCallMethods(targetSymbol, concreteMethods, messages);
				var authCallMethods = FindAuthMethods(semanticModel, targetType, concreteSymbol, messages);

				var factoryMethods = new List<FactoryMethod>();

				foreach (var targetCallMethod in targetCallMethods)
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

				MatchAuthMethods(factoryMethods, authCallMethods, messages);

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

				if (authCallMethods.Any())
				{
					foreach (var factoryOperation in factoryMethods.Where(f => f.FactoryOperation != null)
																					.Select(f => f.FactoryOperation!.Value)
																					.Distinct().ToList())
					{
						var authMethods = authCallMethods.Where(a => ((int?)a.AuthorizeOperation & (int?)factoryOperation) == (int?)a.AuthorizeOperation).ToList();

						if (authMethods.Any())
						{
							// Two-steps to avoid having a 'CanInsert' method just because there is a 'AuthorizeOperation.Write' method
							// But, be sure to include the 'AuthorizeOperation.Write' method if there is a CanInsert method
							authMethods = authCallMethods.Where(a => ((int?)a.AuthorizeOperation & (int?)factoryOperation) != 0).ToList();

							if (!authMethods.Any(m => m.Parameters.Any(p => p.IsTarget)))
							{
								var canMethod = new CanFactoryMethod(targetType, targetConcreteType, factoryOperation.ToString(), authMethods);
								factoryMethods.Add(canMethod);
							}
						}
					}

					if (factoryMethods.Any(f => f.IsSave))
					{
						var allWrite = new[] { AuthorizeOperation.Write, AuthorizeOperation.Insert, AuthorizeOperation.Update, AuthorizeOperation.Delete }.Aggregate((a, b) => a | b);
						var writeAuthMethods = authCallMethods.Where(a => ((int?)a.AuthorizeOperation & (int?)allWrite) != 0).ToList();

						if (writeAuthMethods.Any() && !writeAuthMethods.Any(m => m.Parameters.Any(p => p.IsTarget)))
						{
							var canMethod = new CanFactoryMethod(targetType, targetConcreteType, "Save", writeAuthMethods);
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
					var methodBuilder = new StringBuilder();
					methodBuilder.Append(factoryMethod.PublicMethod(factoryText));
					methodBuilder.Append(factoryMethod.RemoteMethod(factoryText));
					methodBuilder.Append(factoryMethod.LocalMethod());
					factoryText.MethodsBuilder.Append(methodBuilder);
				}

				// We only need the target registered if we do a fetch or create that is not the constructor
				if (factoryMethods.OfType<ReadFactoryMethod>().Any(f => !(f.CallMethod.IsConstructor || f.CallMethod.IsStaticFactory)))
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

			context.AddSource($"{namespaceName}.{targetClassName}Factory.g.cs", source);
		}
		catch (Exception ex)
		{
			source = $"// Error: {ex.Message}";
		}

	}

	private static void GenerateExecute(SourceProductionContext context, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
	{
		var messages = new List<string>();
		string source;
		var usingDirectives = new List<string>() { "using Neatoo.RemoteFactory;",
																 "using Neatoo.RemoteFactory.Internal;",
																 "using Microsoft.Extensions.DependencyInjection;" };

		try
		{
			var staticSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax) ?? throw new Exception($"Cannot get named symbol for {classDeclarationSyntax}");
			var methodNames = new List<string>();
			var delegates = new StringBuilder();
			var remoteMethods = new StringBuilder();
			var localMethods = new StringBuilder();
			var className = classDeclarationSyntax.Identifier.Text;

			// Generate the source code for the found method
			var namespaceName = FindNamespace(classDeclarationSyntax) ?? "MissingNamespace";

			try
			{
				UsingStatements(usingDirectives, classDeclarationSyntax, semanticModel, namespaceName, messages);

				messages.Add($"Static Class: {staticSymbol.ToDisplayString()} Name: {staticSymbol.Name}");

				if (!classDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
				{
					messages.Add($"Class {className} is not partial. Cannot generate factory.");
					return;
				}

				var concreteMethods = GetMethodsRecursive(staticSymbol);

				foreach (var method in concreteMethods)
				{
					if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax methodDeclaration)
					{
						messages.Add($"No MethodDeclarationSyntax for {method.Name}");
						continue;
					}

					if (!methodDeclaration.ReturnType.ToString().StartsWith("Task"))
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

						if (semanticModel.GetTypeInfo(methodDeclaration.ReturnType).Type is not INamedTypeSymbol returnTypeSymbol)
						{
							messages.Add($"No INamedTypeSymbol for {methodDeclaration.Identifier}");
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

						var delegateName = methodDeclaration.Identifier.Text;

						if(delegateName.StartsWith("Execute"))
						{
							delegateName = delegateName.Substring("Execute".Length);
						}
						if (delegateName.StartsWith("_"))
						{
							delegateName = delegateName.Substring(1);
						}

						var parameters = methodDeclaration.ParameterList.Parameters.Select(p => new ParameterInfo(p, method)).ToList();

						var parameterDeclarations = string.Join(", ", parameters.Where(p => !p.IsService)
																		.Select(p => $"{p.Type} {p.Name}"));
						var parameterIdentifiers = string.Join(", ", parameters.Where(p => !p.IsService).Select(p => p.Name));
						var allParameterIdentifiers = string.Join(", ", parameters.Select(p => p.Name));
						var serviceAssignmentsText = WithStringBuilder(parameters.Where(p => p.IsService).Select(p => $"var {p.Name} = cc.GetRequiredService<{p.Type}>();"));

						delegates.AppendLine($"public delegate {methodDeclaration.ReturnType} {delegateName}({parameterDeclarations});");

						remoteMethods.AppendLine(@$"
						  services.AddTransient<{className}.{delegateName}>(cc =>
						  {{
								return ({parameterIdentifiers}) => cc.GetRequiredService<IMakeRemoteDelegateRequest>().ForDelegate{nullableText}<{returnType}>(typeof({className}.{delegateName}), [{parameterIdentifiers}]);
						  }});");

						localMethods.AppendLine(@$"
						  services.AddTransient<{className}.{delegateName}>(cc =>
						  {{
								return ({parameterDeclarations}) => {{
								{serviceAssignmentsText}
								return {className}.{method.Name}({allParameterIdentifiers});
							}};
						  }});");

					}
				}

				var partialClassSignature = classDeclarationSyntax.ToFullString().Substring(classDeclarationSyntax.Modifiers.FullSpan.Start - classDeclarationSyntax.FullSpan.Start, classDeclarationSyntax.Identifier.FullSpan.End - classDeclarationSyntax.Modifiers.FullSpan.Start).Trim();

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

						if(remoteLocal == NeatooFactory.Local)
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

			context.AddSource($"{namespaceName}.{classDeclarationSyntax.Identifier.Text}Factory.g.cs", source);
		}
		catch (Exception ex)
		{
			source = $"// Error: {ex.Message}";
		}

	}
	private static List<IMethodSymbol> GetMethodsRecursive(INamedTypeSymbol? classNamedSymbol, bool includeConst = true)
	{
		var methods = classNamedSymbol?.GetMembers().OfType<IMethodSymbol>()
						.Where(m => includeConst || m.MethodKind != MethodKind.Constructor) // Only include top-level constructors
						.ToList() ?? [];
		if (classNamedSymbol?.BaseType != null)
		{
			methods.AddRange(GetMethodsRecursive(classNamedSymbol.BaseType, false));
		}
		return methods;
	}

	private static List<CallMethod> CreateCallMethods(INamedTypeSymbol namedTypeSymbol, List<IMethodSymbol> methods, List<string> messages)
	{
		var callMethodInfoList = new List<CallMethod>();

		foreach (var method in methods.Where(m => m.GetAttributes().Any()).ToList())
		{
			CallMethod callMethod;

			var attributes = method.GetAttributes().ToList();
			foreach (var attribute in attributes.Where(a => a != null))
			{
				if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ConstructorDeclarationSyntax constructorDeclaration)
				{
					callMethod = new CallMethod(attribute!, namedTypeSymbol, method, constructorDeclaration, messages);
				}
				else if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is MethodDeclarationSyntax methodDeclaration)
				{
					callMethod = new CallMethod(attribute!, namedTypeSymbol, method, methodDeclaration, messages);
				}
				else
				{
					messages.Add($"No MethodDeclarationSyntax for {method.Name}");
					continue;
				}

				if (callMethod.Ignore)
				{
					messages.Add($"No Factory or Authorize attribute for {callMethod.Name} attribute {attribute.AttributeClass?.Name.ToString()}");
					continue;
				}

				callMethodInfoList.Add(callMethod);
			}
		}
		return callMethodInfoList;
	}

	private static List<CallMethod> FindAuthMethods(SemanticModel semanticModel, string returnType, INamedTypeSymbol classNamedSymbol, List<string> messages)
	{
		if (returnType is null)
		{
			throw new ArgumentNullException(nameof(returnType));
		}

		var authorizeAttribute = ClassOrBaseClassHasAttribute(classNamedSymbol, "AuthorizeAttribute");
		var callMethods = new List<CallMethod>();

		if (authorizeAttribute != null)
		{
			var authorizationRuleType = authorizeAttribute.AttributeClass?.TypeArguments[0];

			if (authorizationRuleType?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is TypeDeclarationSyntax syntax)
			{
				var authSemanticModel = semanticModel.Compilation.GetSemanticModel(syntax.SyntaxTree);
				var authSymbol = authSemanticModel.GetDeclaredSymbol(syntax);

				var methods = GetMethodsRecursive(authSymbol);

				foreach (var method in methods)
				{
					var attributes = method.GetAttributes().ToList();

					if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax methodDeclaration)
					{
						messages.Add($"No MethodDeclarationSyntax for {method.Name}");
						continue;
					}

					foreach (var attribute in attributes)
					{
						var callMethod = new CallMethod(attribute, classNamedSymbol, method, methodDeclaration, messages);

						if (!callMethod.Ignore && callMethod.AuthorizeOperation != null)
						{
							callMethods.Add(callMethod);
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
			messages.Add("No AuthorizeAttribute");
		}

		return callMethods;
	}

	private static void MatchAuthMethods(IEnumerable<FactoryMethod> factoryMethods, List<CallMethod> authMethods, List<string> messages)
	{
		if (factoryMethods is null) { throw new ArgumentNullException(nameof(factoryMethods)); }
		if (authMethods is null) { throw new ArgumentNullException(nameof(authMethods)); }
		if (messages is null) { throw new ArgumentNullException(nameof(messages)); }

		foreach (var method in factoryMethods)
		{
			foreach (var authMethod in authMethods)
			{
				var assignAuthMethod = false;

				if (method.FactoryOperation != null)
				{
					if (((int?)authMethod.AuthorizeOperation & (int)method.FactoryOperation) != 0)
					{
						assignAuthMethod = true;
					}
				}

				if (method.AuthorizeOperation != null)
				{
					if (((int?)authMethod.AuthorizeOperation & (int)method.AuthorizeOperation) != 0)
					{
						assignAuthMethod = true;
					}
				}

				if (assignAuthMethod)
				{
					method.AuthCallMethods.Add(authMethod);
				}
			}
		}
	}

	public static void UsingStatements(List<string> usingDirectives, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel, string namespaceName, List<string> messages)
	{
		var parentClassDeclaration = classDeclarationSyntax.Parent as ClassDeclarationSyntax;
		var parentClassUsingText = "";

		while (parentClassDeclaration != null)
		{
			messages.Add("Parent class: " + parentClassDeclaration.Identifier.Text);
			parentClassUsingText = $"{parentClassDeclaration.Identifier.Text}.{parentClassUsingText}";
			parentClassDeclaration = parentClassDeclaration.Parent as ClassDeclarationSyntax;
		}

		if (!string.IsNullOrEmpty(parentClassUsingText))
		{
			usingDirectives.Add($"using static {namespaceName}.{parentClassUsingText.TrimEnd('.')};");
		}

		var recurseClassDeclaration = classDeclarationSyntax;

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
			recurseClassDeclaration = GetBaseClassDeclarationSyntax(semanticModel, recurseClassDeclaration, messages);
		}
	}

	private static ClassDeclarationSyntax? GetBaseClassDeclarationSyntax(SemanticModel semanticModel, ClassDeclarationSyntax classDeclaration, List<string> messages)
	{
		try
		{
			var correctSemanticModel = semanticModel.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);

			var classSymbol = correctSemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

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

			var baseTypeSyntaxNode = baseTypeSyntaxReference.GetSyntax() as ClassDeclarationSyntax;

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
}