using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
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
		public CallMethod(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, ConstructorDeclarationSyntax constructorDeclarationSyntax) : this(classSymbol, methodSymbol, constructorDeclarationSyntax.AttributeLists, constructorDeclarationSyntax.ParameterList)
		{
			if (this.FactoryOperation != RemoteFactory.FactoryGenerator.FactoryOperation.Create)
			{
				throw new InvalidOperationException($"Only Create can be a constructor. Method {methodSymbol.Name}");
			}

			this.Name = "Create";
			this.IsConstructor = true;
		}

		public CallMethod(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, MethodDeclarationSyntax methodDeclarationSyntax, List<string> messages) : this(classSymbol, methodSymbol, methodDeclarationSyntax.AttributeLists, methodDeclarationSyntax.ParameterList)
		{
			if (methodSymbol.ReturnType.ToDisplayString().Contains(classSymbol.Name))
			{
				var methodType = methodSymbol.ReturnType.ToDisplayString();

				if (methodType.Contains(@"Task<"))
				{
					methodType = Regex.Match(methodType, @"Task<(.*?)>").Groups[1].Value;
					messages.Add($"Method {methodSymbol.Name} had Task removed {methodType}");
				}

				if (methodType.EndsWith("?"))
				{
					this.IsNullable = true;
					methodType = methodType.Substring(0, methodType.Length - 1);
					messages.Add($"Method {methodSymbol.Name} had ? removed {methodType}");
				}

				if (methodType == classSymbol.ToDisplayString())
				{
					if (((int?)this.FactoryOperation & (int)RemoteFactory.FactoryGenerator.AuthorizeOperation.Read) == 0)
					{
						throw new InvalidOperationException("Only Fetch and Create can return the target type");
					}
					if (!methodSymbol.IsStatic)
					{
						throw new InvalidOperationException($"{methodSymbol.Name} must be static. Only static factories are allowed.");
					}
					this.IsStaticFactory = true;
				}
			}
		}

		private CallMethod(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, SyntaxList<AttributeListSyntax> attributeLists, ParameterListSyntax? parameterListSyntax)
		{
			var attributes = methodSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).Where(a => a != null).ToList();

			this.Name = methodSymbol.Name;
			this.ClassName = methodSymbol.ContainingType.Name;
			this.IsBool = methodSymbol.ReturnType.ToString().Contains("bool");
			this.IsTask = methodSymbol.ReturnType.ToString().Contains("Task");
			this.IsRemote = attributes.Any(a => a == "Remote");
			this.IsSave = attributes.Any(a => factorySaveOperationAttributes.Contains(a!));

			foreach (var attribute in attributes)
			{
				if (Enum.TryParse<FactoryOperation>(attribute, out var dmm))
				{
					this.FactoryOperation = dmm;
					break;
				}
				if (attribute == "Authorize")
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
				this.Parameters = [.. parameterListSyntax.Parameters.Select(p => new ParameterInfo()
				{
					Name = p.Identifier.Text,
					Type = p.Type!.ToFullString(),
					ParameterSymbol = methodSymbol.Parameters.Where(mp => mp.Name == p.Identifier.Text).FirstOrDefault(),
					IsService = p.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.ToFullString() == "Service"),
				})];

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
		public string NamePostfix => this.Name.Replace(this.FactoryOperation?.ToString() ?? "", "");
		public bool IsConstructor { get; } = false;
		public bool IsStaticFactory { get; } = false;
		public bool IsNullable { get; } = false;
		public bool IsBool { get; private set; }
		public bool IsTask { get; private set; }
		public bool IsRemote { get; private set; }
		public bool IsSave { get; private set; }
		public FactoryOperation? FactoryOperation { get; private set; }
		public AuthorizeOperation? AuthorizeOperation { get; private set; }
		public List<ParameterInfo> Parameters { get; private set; }

		public void MakeAuthCall(FactoryMethod inMethod, StringBuilder methodBuilder)
		{
			var parameters = inMethod.Parameters.ToList();

			if (!this.Parameters.Any(p => p.IsTarget))
			{
				parameters.RemoveAll(p => p.IsTarget);
			}

			var parameterText = string.Join(", ", parameters.Select(a => a.Name).Take(this.Parameters.Count));

			var callText = $"{this.ClassName}.{this.Name}({parameterText})";

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

		// Match the nullability return of Save methods to the return of the individual Insert/Update/Delete methods
		// Even though they always return a value
		public override bool IsNullable => true;

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
		public SaveFactoryMethod(string targetType, string concreteType, List<WriteFactoryMethod> writeFactoryMethods) : base(targetType, concreteType)
		{
			var writeFactoryMethod = writeFactoryMethods.First();
			this.Name = $"Save{writeFactoryMethod.NamePostfix}";
			this.UniqueName = this.Name;
			this.WriteFactoryMethods = writeFactoryMethods;
			this.Parameters = writeFactoryMethods.First().Parameters;
			this.AuthCallMethods.AddRange(writeFactoryMethods.SelectMany(m => m.AuthCallMethods).Distinct());
		}

		public bool IsDefault { get; set; } = false;
		public override bool IsSave => true;
		public override bool IsBool => true;
		public override bool IsRemote => this.WriteFactoryMethods.Any(m => m.IsRemote);
		public override bool IsTask => this.IsRemote || this.WriteFactoryMethods.Any(m => m.IsTask);
		public override bool IsAsync => this.WriteFactoryMethods.Any(m => m.IsTask) && this.WriteFactoryMethods.Any(m => !m.IsTask);
		public override bool HasAuth => this.WriteFactoryMethods.Any(m => m.HasAuth);

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

				return $"return {methodCall}";
			}

			var defaultReturn = $"default({this.TargetType})";
			if (this.HasAuth)
			{
				defaultReturn = $"new Authorized<{this.TargetType}>()";
			}

			if (this.IsTask && !this.IsAsync)
			{
				defaultReturn = $"Task.FromResult({defaultReturn})";
			}

			methodBuilder.AppendLine($@"
                                            if (target.IsDeleted)
                                    {{
                                        if (target.IsNew)
                                        {{
                                            return {defaultReturn};
                                        }}
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
		public override bool IsNullable => this.CallMethod.IsNullable;

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
				}

				var targetType = this.TargetType;

				if (this.IsNullable)
				{
					targetType = $"{targetType}?";
				}

				methodCall += $"<{targetType}>";

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

			if (!this.CallMethod.IsConstructor)
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
		public CanFactoryMethod(string targetType, string concreteType, FactoryMethod factoryMethod) : base(targetType, concreteType)
		{
			this.Name = $"Can{factoryMethod.Name}";
			this.UniqueName = this.Name;
			this.NamePostfix = factoryMethod.NamePostfix;
			this.AuthCallMethods.AddRange(factoryMethod.AuthCallMethods);
			this.Parameters = [.. factoryMethod.Parameters.Where(p => !p.IsTarget)];
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
				methodBuilder.AppendLine($"return ({awaitKeyword} Local{this.UniqueName}({this.ParameterIdentifiersText(includeServices: false)})).Result!;");
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
				classText.Delegates.AppendLine($"public delegate {this.ReturnType()} {this.DelegateName}({this.ParameterDeclarationsText()});");
				classText.PropertyDeclarations.AppendLine($"public {this.DelegateName} {this.UniqueName}Property {{ get; }}");
				classText.ConstructorPropertyAssignmentsLocal.AppendLine($"{this.UniqueName}Property = Local{this.UniqueName};");
				classText.ConstructorPropertyAssignmentsRemote.AppendLine($"{this.UniqueName}Property = Remote{this.UniqueName};");

				methodBuilder.AppendLine($"public virtual async {this.ReturnType()} Remote{this.UniqueName}({this.ParameterDeclarationsText()})");
				methodBuilder.AppendLine("{");
				methodBuilder.AppendLine($" return (await MakeRemoteDelegateRequest!.ForDelegate<{this.ReturnType(includeTask: false)}>(typeof({this.DelegateName}), [{this.ParameterIdentifiersText()}]))!;");
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
				foreach (var authMethod in this.AuthCallMethods)
				{
					authMethod.MakeAuthCall(this, methodBuilder);
				}
			}

			return methodBuilder;
		}

		public abstract StringBuilder LocalMethod();
	}

	public class ParameterInfo
	{
		public string Name { get; set; } = null!;
		public string Type { get; set; } = null!;
		public IParameterSymbol? ParameterSymbol { get; set; } = null!;
		public bool IsService { get; set; }
		public bool IsTarget { get; set; }
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
		public StringBuilder ConstructorServiceParameters { get; set; } = new();
	}

	private static void Execute(SourceProductionContext context, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
	{
		var messages = new List<string>();
		string source;

		try
		{
			var classNamedSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax) ?? throw new Exception($"Cannot get named symbol for {classDeclarationSyntax}");

			var usingDirectives = new List<string>() { "using Neatoo.RemoteFactory;", "using Microsoft.Extensions.DependencyInjection;" };
			var methodNames = new List<string>();
			var targetClassName = classDeclarationSyntax.Identifier.Text;
			var targetType = $"{targetClassName}";
			var targetConcreteType = $"{targetClassName}";
			var factoryText = new FactoryText();

			if (classNamedSymbol.Interfaces.Any(i => i.Name == $"I{classNamedSymbol.Name}"))
			{
				targetType = $"I{classNamedSymbol.Name}";
				factoryText.ServiceRegistrations.AppendLine($@"services.AddTransient<I{classNamedSymbol.Name}, {classNamedSymbol.Name}>();");
			}

			// Generate the source code for the found method
			var namespaceName = FindNamespace(classDeclarationSyntax);

			try
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

				messages.Add($"Class: {classNamedSymbol.ToDisplayString()} Name: {classNamedSymbol.Name}");
				var targetCallMethods = FindTargetMethods(targetType, classNamedSymbol, messages);
				var authCallMethods = FindAuthMethods(semanticModel, targetType, classNamedSymbol, messages);

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

				var saveMethods = factoryMethods
									 .OfType<WriteFactoryMethod>()
									 .Where(m => m.IsSave)
									 .GroupBy(m => string.Join(",", m.Parameters.Where(m => !m.IsTarget && !m.IsService)
																	 .Select(m => m.Type.ToString())))
									 .ToList();

				foreach (var saveMethod in saveMethods)
				{
					if (saveMethod.Count(m => m.FactoryOperation == FactoryOperation.Insert) > 1
						 || saveMethod.Count(m => m.FactoryOperation == FactoryOperation.Update) > 1
						 || saveMethod.Count(m => m.FactoryOperation == FactoryOperation.Delete) > 1)
					{
						var byName = saveMethod.GroupBy(m => m.NamePostfix).ToList();

						foreach (var byNameMethod in byName)
						{
							if (byNameMethod.Count(m => m.FactoryOperation == FactoryOperation.Insert) > 1
									  || byNameMethod.Count(m => m.FactoryOperation == FactoryOperation.Update) > 1
									  || byNameMethod.Count(m => m.FactoryOperation == FactoryOperation.Delete) > 1)
							{
								messages.Add($"Multiple Insert/Update/Delete methods with the same name: {saveMethod.First().Name}");
								break;
							}

							factoryMethods.Add(new SaveFactoryMethod(targetType, targetConcreteType, [.. byNameMethod]));
						}
					}
					else
					{
						factoryMethods.Add(new SaveFactoryMethod(targetType, targetConcreteType, [.. saveMethod]));
					}
				}

				foreach (var factoryMethod in factoryMethods.ToList())
				{
					if (factoryMethod.HasAuth)
					{
						if (factoryMethod.AuthCallMethods.Any(m => m.Parameters.Any(p => p.IsTarget)))
						{
							messages.Add($"Factory Can{factoryMethod.Name} not created because it matches to an auth method with a {targetType} parameter");
						}
						else
						{
							if (factoryMethod is WriteFactoryMethod writeFactoryMethod)
							{
								// Don't add a CanInsert, CanDelete and CanUpdate when the Auth is the same for each
								// In cases where there is only an AuthorizedOperation.Write
								if (factoryMethod.AuthCallMethods.Any(m => ((int?)m.AuthorizeOperation & (int?)factoryMethod.FactoryOperation) == (int?)m.AuthorizeOperation))
								{
									factoryMethods.Add(new CanFactoryMethod(targetType, targetConcreteType, factoryMethod));
								}
							}
							else
							{
								factoryMethods.Add(new CanFactoryMethod(targetType, targetConcreteType, factoryMethod));
							}
						}
					}
				}

				var defaultSaveMethod = factoryMethods.OfType<SaveFactoryMethod>()
									 .Where(s => s.Parameters.Where(p => !p.IsTarget && !p.IsService).Count() == 0 && s.Parameters.First().IsTarget)
									 .FirstOrDefault();
				if (defaultSaveMethod != null) { defaultSaveMethod.IsDefault = true; }

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

				foreach (var authMethods in authCallMethods.GroupBy(a => a.ClassName))
				{
					var authMethod = authMethods.First();
					var constructorParameter = $"{authMethod.ClassName} {authMethod.ClassName.ToLower()}";
					var propertyAssignment = $"this.{authMethod.ClassName} = {authMethod.ClassName.ToLower()};";

					if (authMethods.Any(a => !a.IsRemote))
					{
						factoryText.ConstructorPropertyAssignmentsRemote.AppendLine(propertyAssignment);
					}
					factoryText.ConstructorServiceParameters.AppendLine(',' + constructorParameter);
					factoryText.ConstructorPropertyAssignmentsLocal.AppendLine(propertyAssignment);
					factoryText.ConstructorPropertyAssignmentsRemote.AppendLine(propertyAssignment);

					factoryText.PropertyDeclarations.AppendLine($"public {authMethod.ClassName} {authMethod.ClassName} {{ get; }}");
				}

				foreach (var factoryMethod in factoryMethods)
				{
					var methodBuilder = new StringBuilder();
					methodBuilder.Append(factoryMethod.PublicMethod(factoryText));
					methodBuilder.Append(factoryMethod.RemoteMethod(factoryText));
					methodBuilder.Append(factoryMethod.LocalMethod());
					factoryText.MethodsBuilder.Append(methodBuilder);
				}

				var isSave = saveMethods.Any();
				var editText = isSave ? "Save" : "";
				if (isSave)
				{
					factoryText.ServiceRegistrations.AppendLine($@"services.AddScoped<IFactorySave<{targetClassName}>, {targetClassName}Factory>();");
				}

				source = $@"
						  #nullable enable

                    using Neatoo.RemoteFactory.Internal;
                    {WithStringBuilder(usingDirectives)}
                    /*
                    Debugging Messages:
                    {WithStringBuilder(messages)}
                    */
                    namespace {namespaceName}
                    {{

                        public interface I{targetClassName}Factory
                        {{
                    {factoryText.InterfaceMethods}
                        }}

                        internal class {targetClassName}Factory : Factory{editText}Base{(isSave ? $"<{targetClassName}>, IFactorySave<{targetClassName}>" : "")}, I{targetClassName}Factory
                        {{

                            private readonly IServiceProvider ServiceProvider;  
                            private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

                    // Delegates
                    {factoryText.Delegates}
                    // Delegate Properties to provide Local or Remote fork in execution
                    {factoryText.PropertyDeclarations}

                            public {targetClassName}Factory(IServiceProvider serviceProvider, {factoryText.ConstructorServiceParameters.ToString().Trim(',')})
                            {{
                                    this.ServiceProvider = serviceProvider;
                                    {factoryText.ConstructorPropertyAssignmentsLocal}
                            }}

                            public {targetClassName}Factory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, {factoryText.ConstructorServiceParameters.ToString().Trim(',')})
                            {{
                                    this.ServiceProvider = serviceProvider;
                                    this.MakeRemoteDelegateRequest = remoteMethodDelegate;
                                    {factoryText.ConstructorPropertyAssignmentsRemote}
                            }}

                    {factoryText.MethodsBuilder}
                    {factoryText.SaveMethods}

                            public static void FactoryServiceRegistrar(IServiceCollection services)
                            {{
                                services.AddTransient<{targetClassName}>();
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
				source = $"/* Error: {ex.GetType().FullName} {ex.Message} */";
			}

			context.AddSource($"{namespaceName}.{targetClassName}Factory.g.cs", source);
		}
		catch (Exception ex)
		{
			source = $"// Error: {ex.Message}";
		}

	}

	private static List<IMethodSymbol> GetMethodsRecursive(INamedTypeSymbol? classNamedSymbol)
	{
		var methods = classNamedSymbol?.GetMembers().OfType<IMethodSymbol>().ToList() ?? [];
		if (classNamedSymbol?.BaseType != null)
		{
			methods.AddRange(GetMethodsRecursive(classNamedSymbol.BaseType));
		}
		return methods;
	}

	private static List<CallMethod> FindTargetMethods(string targetType, INamedTypeSymbol namedTypeSymbol, List<string> messages)
	{
		if (targetType is null)
		{
			throw new ArgumentNullException(nameof(targetType));
		}

		var methods = GetMethodsRecursive(namedTypeSymbol);
		var callMethodInfoList = new List<CallMethod>();

		foreach (var method in methods.Where(m => m.GetAttributes().Any()).ToList())
		{
			CallMethod callMethod;
			messages.Add($"Method: {method.Name} ReturnType: {method.ReturnType.ToDisplayString()}");

			if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ConstructorDeclarationSyntax constructorDeclaration)
			{
				callMethod = new CallMethod(namedTypeSymbol, method, constructorDeclaration);
			}
			else if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is MethodDeclarationSyntax methodDeclaration)
			{
				callMethod = new CallMethod(namedTypeSymbol, method, methodDeclaration, messages);
			}
			else
			{
				messages.Add($"No MethodDeclarationSyntax for {method.Name}");
				continue;
			}


			if (callMethod.FactoryOperation != null || callMethod.AuthorizeOperation != null)
			{
				callMethodInfoList.Add(callMethod);
			}
			else
			{
				messages.Add($"No Factory or Authorize attribute for {callMethod.Name}");
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
					if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax methodDeclaration)
					{
						messages.Add($"No MethodDeclarationSyntax for {method.Name}");
						continue;
					}

					var callMethod = new CallMethod(classNamedSymbol, method, methodDeclaration, messages);

					if (callMethod.AuthorizeOperation != null)
					{
						callMethods.Add(callMethod);
					}
					else
					{
						messages.Add($"No AuthorizeOperation for {authorizeAttribute}");
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

				var methodParameter = method.Parameters.GetEnumerator();
				var authMethodParameter = authMethod.Parameters.GetEnumerator();

				methodParameter.MoveNext();
				authMethodParameter.MoveNext();

				// Don't disqualify an auth method we're in a write method and the first parameter is the target
				// But also accept auth methods that have a first parameter of target
				if (methodParameter.Current?.IsTarget ?? false)
				{
					methodParameter.MoveNext();
					if (method.IsSave && authMethodParameter.Current != null && authMethodParameter.Current.IsTarget)
					{
						authMethodParameter.MoveNext();
					}
				}

				if (authMethodParameter.Current != null)
				{
					do
					{
						if (authMethodParameter.Current.ParameterSymbol != null && methodParameter.Current?.ParameterSymbol != null
							&& authMethodParameter.Current.ParameterSymbol.Type.ToDisplayString() != methodParameter.Current.ParameterSymbol.Type.ToDisplayString())
						{
							messages.Add($"Parameter type mismatch for {authMethod.Name} and {method.Name} parameter {authMethodParameter.Current.Name} {authMethodParameter.Current.Type}");
							messages.Add($"{authMethodParameter.Current.ParameterSymbol.Type.ToDisplayString()} != {methodParameter.Current.ParameterSymbol.Type.ToDisplayString()}");
							assignAuthMethod = false;
							break;
						}
						else if (authMethodParameter.Current.Type != methodParameter.Current?.Type)
						{
							assignAuthMethod = false;
							break;
						}
					} while (authMethodParameter.MoveNext() && methodParameter.MoveNext());
				}

				if (assignAuthMethod)
				{
					method.AuthCallMethods.Add(authMethod);
				}
			}
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