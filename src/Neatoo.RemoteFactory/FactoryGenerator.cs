using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Neatoo.RemoteFactory;

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

	private static List<string> dataMapperSaveAttributes = Enum.GetValues(typeof(FactoryOperation)).Cast<int>().Where(v => (v & (int)AuthorizeOperation.Write) != 0).Select(v => Enum.GetName(typeof(FactoryOperation), v)).ToList();

	internal class CallMethodInfo
	{
		public CallMethodInfo(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, MethodDeclarationSyntax methodDeclaration)
		{
			var attributes = methodSymbol.GetAttributes().Select(a => a.AttributeClass?.Name.Replace("Attribute", "")).Where(a => a != null).ToList();

			this.Name = methodSymbol.Name;
			this.ClassName = methodSymbol.ContainingType.Name;
			this.IsBool = methodSymbol.ReturnType.ToString().Contains("bool");
			this.IsTask = methodSymbol.ReturnType.ToString().Contains("Task");
			this.IsRemote = attributes.Any(a => a == "Remote");
			this.IsSave = attributes.Any(a => dataMapperSaveAttributes.Contains(a!));

			foreach (var attribute in attributes)
			{
				if (Enum.TryParse<FactoryOperation>(attribute, out var dmm))
				{
					this.FactoryOperation = dmm;
					break;
				}
				if (attribute == "Authorize")
				{
					var attr = methodDeclaration.AttributeLists.SelectMany(a => a.Attributes)
						 .Where(a => a.Name.ToString() == "Authorize")
						 .SingleOrDefault()?
						 .ArgumentList?.Arguments.ToFullString();

					var pattern = @"DataMapperMethodType\.(\w+)";

					// Use Regex.Matches to find all matches in the attr string
					var matches = Regex.Matches(attr, pattern);
					var dataMapperMethodTypes = new List<AuthorizeOperation>();

					foreach (Match match in matches)
					{
						// Extract the matched value (e.g., "Read", "Write")
						var value = match.Groups[1].Value;

						// Try to parse the value into the DataMapperMethodType enum
						if (Enum.TryParse<AuthorizeOperation>(value, out var dmType))
						{
							// Successfully parsed the value into the DataMapperMethodType enum
							dataMapperMethodTypes.Add(dmType);
						}
					}

					this.AuthorizeOperation = dataMapperMethodTypes.Aggregate((a, b) => a | b);
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
			if (methodDeclaration != null)
			{
				this.Parameters = methodDeclaration.ParameterList.Parameters.Select(p => new ParameterInfo()
				{
					Name = p.Identifier.Text,
					Type = p.Type!.ToFullString(),
					IsService = p.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.ToFullString() == "Service"),
				}).ToList();

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
		public WriteFactoryMethod(string targetType, string concreteType, CallMethodInfo callMethodInfo) : base(targetType, concreteType, callMethodInfo)
		{
			this.Parameters.Insert(0, new ParameterInfo() { Name = "target", Type = $"{targetType}", IsService = false, IsTarget = true });
		}

		public override StringBuilder PublicMethod(ClassText classText)
		{
			return new StringBuilder();
		}

		public override StringBuilder RemoteMethod(ClassText classText)
		{
			return new StringBuilder();
		}

		public override StringBuilder LocalMethod()
		{
			var methodBuilder = base.LocalMethodStart();

			methodBuilder.AppendLine($"var cTarget = ({this.ConcreteType}) target ?? throw new Exception(\"{this.TargetType} must implement {this.ConcreteType}\");");
			methodBuilder.AppendLine($"{this.ServiceAssignmentsText}");
			methodBuilder.AppendLine($"return {this.DoMapperMethodCall.Replace("target", "cTarget")};");
			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("");

			return methodBuilder;
		}
	}

	internal class SaveFactoryMethod : FactoryMethod
	{
		public SaveFactoryMethod(string targetType, string concreteType, List<WriteFactoryMethod> dataMapperSaveMethods) : base(targetType, concreteType)
		{
			var dataMapperSaveMethod = dataMapperSaveMethods.First();
			this.Name = $"Save{dataMapperSaveMethod.NamePostfix}";
			this.UniqueName = this.Name;
			this.DataMapperSaveMethods = dataMapperSaveMethods;
			this.Parameters = dataMapperSaveMethods.First().Parameters;
		}

		public bool IsDefault { get; set; } = false;
		public override bool IsSave => true;
		public override bool IsBool => true;
		public override bool IsRemote => this.DataMapperSaveMethods.Any(m => m.IsRemote);
		public override bool IsTask => this.IsRemote || this.DataMapperSaveMethods.Any(m => m.IsTask);
		public override bool IsAsync => this.DataMapperSaveMethods.Any(m => m.IsTask) && this.DataMapperSaveMethods.Any(m => !m.IsTask);
		public override bool HasAuth => this.DataMapperSaveMethods.Any(m => m.HasAuth);

		public List<WriteFactoryMethod> DataMapperSaveMethods { get; }

		public override StringBuilder PublicMethod(ClassText classText)
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
			methodBuilder.AppendLine("throw new NotAuthorizedException(authorized.Message);");
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
				methodBuilder.AppendLine($"async Task<IEditBase?> IFactoryEditBase<{this.ConcreteType}>.Save({this.ConcreteType} target)");
				methodBuilder.AppendLine("{");

				if (this.IsTask)
				{
					methodBuilder.AppendLine($"return (IEditBase?) await {this.Name}(target);");
				}
				else
				{
					methodBuilder.AppendLine($"return await Task.FromResult((IEditBase?) {this.Name}(target));");
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

			methodBuilder.AppendLine($@"
                                            if (target.IsDeleted)
                                    {{
                                        if (target.IsNew)
                                        {{
                                            return null;
                                        }}
                                        {DoInsertUpdateDeleteMethodCall(this.DataMapperSaveMethods.Where(s => s.FactoryOperation == RemoteFactory.FactoryOperation.Delete).FirstOrDefault())};
                                    }}
                                    else if (target.IsNew)
                                    {{
                                        {DoInsertUpdateDeleteMethodCall(this.DataMapperSaveMethods.Where(s => s.FactoryOperation == RemoteFactory.FactoryOperation.Insert).FirstOrDefault())};
                                    }}
                                    else
                                    {{
                                         {DoInsertUpdateDeleteMethodCall(this.DataMapperSaveMethods.Where(s => s.FactoryOperation == RemoteFactory.FactoryOperation.Update).FirstOrDefault())};
                                    }}
                            ");

			methodBuilder.AppendLine("}");

			return methodBuilder;
		}
	}

	internal class ReadFactoryMethod : FactoryMethod
	{
		public ReadFactoryMethod(string targetType, string concreteType, CallMethodInfo callMethod) : base(targetType, concreteType)
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

		public CallMethodInfo CallMethod { get; set; }

		public string DoMapperMethodCall
		{
			get
			{
				var methodCall = "DoMapperMethodCall";

				if (this.CallMethod.IsBool)
				{
					methodCall += "Bool";
				}

				if (this.CallMethod.IsTask)
				{
					methodCall += "Async";
				}

				methodCall += $"<{this.TargetType}>";

				methodCall = $"{methodCall}(target, DataMapperMethod.{this.FactoryOperation}, () => target.{this.Name} ({this.ParameterIdentifiersText(includeServices: true, includeTarget: false)}))";

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

			methodBuilder.AppendLine($"var target = ServiceProvider.GetRequiredService<{this.ConcreteType}>();");
			methodBuilder.AppendLine($"{this.ServiceAssignmentsText}");
			methodBuilder.AppendLine($"return {this.DoMapperMethodCall};");
			methodBuilder.AppendLine("}");
			methodBuilder.AppendLine("");

			return methodBuilder;
		}
	}

	internal class CanFactoryMethod : FactoryMethod
	{
		public CanFactoryMethod(string targetType, string concreteType, ReadFactoryMethod readFactoryMethod) : base(targetType, concreteType)
		{
			this.Name = $"Can{readFactoryMethod.Name}";
			this.UniqueName = this.Name;
			this.NamePostfix = readFactoryMethod.NamePostfix;
			this.AuthCallMethods.AddRange(readFactoryMethod.AuthCallMethods);
			this.Parameters = readFactoryMethod.Parameters.Where(p => !p.IsTarget).ToList();
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

		public override StringBuilder PublicMethod(ClassText classText)
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

	internal abstract class FactoryMethod
	{
		public FactoryMethod(string targetType, string concreteType)
		{
			this.TargetType = targetType;
			this.ConcreteType = concreteType;
		}

		public string TargetType { get; }
		public string ConcreteType { get; set; }
		public string Name { get; protected set; } = null!;
		public string UniqueName { get; set; } = null!;
		public string? NamePostfix { get; protected set; }
		public string DelegateName => $"{this.UniqueName}Delegate";

		public List<CallMethodInfo> AuthCallMethods { get; set; } = new List<CallMethodInfo>();
		public virtual bool HasAuth => this.AuthCallMethods.Count > 0;
		public FactoryOperation? FactoryOperation { get; set; }
		public AuthorizeOperation? AuthorizeOperation { get; set; }
		public List<ParameterInfo> Parameters { get; set; } = null!;
		public virtual bool IsSave => false;
		public virtual bool IsBool => false;
		public virtual bool IsTask => this.IsRemote || this.AuthCallMethods.Any(m => m.IsTask);
		public virtual bool IsRemote => this.AuthCallMethods.Any(m => m.IsRemote);
		public virtual bool IsAsync => this.AuthCallMethods.Any(m => m.IsTask);
		public virtual string AsyncKeyword => this.IsAsync ? "async" : "";
		public virtual string AwaitKeyword => this.IsAsync ? "await" : "";
		public string ServiceAssignmentsText => string.Join("\n", this.Parameters.Where(p => p.IsService).Select(p => $"\t\t\tvar {p.Name} = ServiceProvider.GetService<{p.Type}>();"));
		public virtual string ReturnType(bool includeTask = true, bool includeAuth = true, bool includeBool = true)
		{
			var returnType = this.TargetType;

			if (this.HasAuth && includeAuth)
			{
				returnType = $"Authorized<{returnType}>";
			}
			else if (this.IsBool && includeBool)
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

		public virtual StringBuilder PublicMethod(ClassText classText)
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

		public virtual StringBuilder RemoteMethod(ClassText classText)
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
				methodBuilder.AppendLine($" return await DoRemoteRequest.ForDelegate<{this.ReturnType(includeTask: false)}>(typeof({this.DelegateName}), [{this.ParameterIdentifiersText()}]);");
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
		public bool IsService { get; set; }
		public bool IsTarget { get; set; }
	}

	internal class ClassText
	{
		public StringBuilder Delegates { get; set; } = new StringBuilder();
		public StringBuilder ConstructorPropertyAssignmentsLocal { get; set; } = new StringBuilder();
		public StringBuilder ConstructorPropertyAssignmentsRemote { get; set; } = new StringBuilder();
		public StringBuilder PropertyDeclarations { get; set; } = new StringBuilder();
		public StringBuilder MethodsBuilder { get; set; } = new StringBuilder();
		public StringBuilder SaveMethods { get; set; } = new StringBuilder();
		public StringBuilder InterfaceMethods { get; set; } = new StringBuilder();
		public StringBuilder ServiceRegistrations { get; set; } = new StringBuilder();
		public List<string> ConstructorParametersLocal { get; set; } = [];
		public List<string> ConstructorParametersRemote { get; set; } = [];
	}

	private static void Execute(SourceProductionContext context, ClassDeclarationSyntax classDeclarationSyntax, SemanticModel semanticModel)
	{
		var messages = new List<string>();
		string source;

		try
		{
			var classNamedSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax) ?? throw new Exception($"Cannot get named symbol for {classDeclarationSyntax}");

			var usingDirectives = new List<string>() { "using Neatoo;", "using Neatoo.Portal;" };
			var methodNames = new List<string>();
			var className = classDeclarationSyntax.Identifier.Text;
			var returnType = $"{className}";
			var concreteType = $"{className}";
			var classText = new ClassText();

			if (classNamedSymbol.Interfaces.Any(i => i.Name == $"I{classNamedSymbol.Name}"))
			{
				returnType = $"I{classNamedSymbol.Name}";
				classText.ServiceRegistrations.AppendLine($@"services.AddTransient<I{classNamedSymbol.Name}, {classNamedSymbol.Name}>();");
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


				var dataMapperCallMethods = FindMethods(returnType, classNamedSymbol, messages);
				var authCallMethods = FindAuthMethods(semanticModel, returnType, classNamedSymbol, messages);

				var factoryMethods = new List<FactoryMethod>();

				foreach (var dataMapperMethod in dataMapperCallMethods)
				{
					if (dataMapperMethod.IsSave)
					{
						factoryMethods.Add(new WriteFactoryMethod(returnType, concreteType, dataMapperMethod));
					}
					else
					{
						factoryMethods.Add(new ReadFactoryMethod(returnType, concreteType, dataMapperMethod));
					}
				}

				MatchAuthMethods(factoryMethods, authCallMethods, messages);

				foreach (var factoryMethod in factoryMethods.OfType<ReadFactoryMethod>().ToList())
				{
					if (factoryMethod.HasAuth)
					{
						if (factoryMethod.AuthCallMethods.Any(m => m.Parameters.Any(p => p.IsTarget)))
						{
							messages.Add($"Factory Can{factoryMethod.Name} not created because it matches to an auth method with a {returnType} parameter");
						}
						else
						{
							factoryMethods.Add(new CanFactoryMethod(returnType, concreteType, factoryMethod));
						}
					}
				}

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

							factoryMethods.Add(new SaveFactoryMethod(returnType, concreteType, [.. byNameMethod]));
						}
					}
					else
					{
						factoryMethods.Add(new SaveFactoryMethod(returnType, concreteType, [.. saveMethod]));
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
						classText.ConstructorParametersRemote.Add(constructorParameter);
						classText.ConstructorPropertyAssignmentsRemote.AppendLine(propertyAssignment);
					}
					classText.ConstructorParametersLocal.Add(constructorParameter);
					classText.ConstructorPropertyAssignmentsLocal.AppendLine(propertyAssignment);

					classText.PropertyDeclarations.AppendLine($"public {authMethod.ClassName} {authMethod.ClassName} {{ get; }}");
				}

				foreach (var factoryMethod in factoryMethods)
				{
					var methodBuilder = new StringBuilder();
					methodBuilder.Append(factoryMethod.PublicMethod(classText));
					methodBuilder.Append(factoryMethod.RemoteMethod(classText));
					methodBuilder.Append(factoryMethod.LocalMethod());
					classText.MethodsBuilder.Append(methodBuilder);
				}

				var isEdit = saveMethods.Any();
				var editText = isEdit ? "Edit" : "";
				if (isEdit)
				{
					classText.ServiceRegistrations.AppendLine($@"services.AddScoped<IFactoryEditBase<{className}>, {className}Factory>();");
				}

				source = $@"
                    using Microsoft.Extensions.DependencyInjection;
                    using Neatoo.Portal.Internal;
                    {string.Join("\n", usingDirectives)}
                    /*
                    Debugging Messages:
                    {string.Join("\n", messages)}
                    */
                    namespace {namespaceName}
                    {{

                        public interface I{className}Factory
                        {{
                    {classText.InterfaceMethods}
                        }}

                        internal class {className}Factory : Factory{editText}Base{(isEdit ? $"<{className}>, IFactoryEditBase<{className}>" : "")}, I{className}Factory
                        {{

                            private readonly IServiceProvider ServiceProvider;  
                            private readonly IDoRemoteRequest DoRemoteRequest;

                    // Delegates
                    {classText.Delegates}
                    // Delegate Properties to provide Local or Remote fork in execution
                    {classText.PropertyDeclarations}

                            public {className}Factory(IServiceProvider serviceProvider, {string.Join("\n,", classText.ConstructorParametersLocal)})
                            {{
                                    this.ServiceProvider = serviceProvider;
                                    {classText.ConstructorPropertyAssignmentsLocal}
                            }}

                            public {className}Factory(IServiceProvider serviceProvider, IDoRemoteRequest remoteMethodDelegate, {string.Join("\n,", classText.ConstructorParametersLocal)})
                            {{
                                    this.ServiceProvider = serviceProvider;
                                    this.DoRemoteRequest = remoteMethodDelegate;
                                    {classText.ConstructorPropertyAssignmentsRemote}
                            }}

                    {classText.MethodsBuilder}
                    {classText.SaveMethods}

                            public static void FactoryServiceRegistrar(IServiceCollection services)
                            {{
                                services.AddTransient<{className}>();
                                services.AddScoped<{className}Factory>();
                                services.AddScoped<I{className}Factory, {className}Factory>();
                    {classText.ServiceRegistrations}
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

			context.AddSource($"{namespaceName}.{className}Factory.g.cs", source);
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

	private static List<CallMethodInfo> FindMethods(string targetType, INamedTypeSymbol namedTypeSymbol, List<string> messages)
	{
		var methods = GetMethodsRecursive(namedTypeSymbol);
		var callMethodInfos = new List<CallMethodInfo>();

		foreach (var method in methods.Where(m => m.GetAttributes().Any()).ToList())
		{
		 if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax methodDeclaration)
		 {
			messages.Add($"No MethodDeclarationSyntax for {method.Name}");
			continue;
		 }

		 var methodInfo = new CallMethodInfo(namedTypeSymbol, method, methodDeclaration);
			if (methodInfo.FactoryOperation != null || methodInfo.AuthorizeOperation != null)
			{
				callMethodInfos.Add(methodInfo);
			}
			else
			{
				messages.Add($"No DataMapperMethod or Authorized attribute for {methodInfo.Name}");
			}
		}
		return callMethodInfos;
	}

	private static List<CallMethodInfo> FindAuthMethods(SemanticModel semanticModel, string returnType, INamedTypeSymbol classNamedSymbol, List<string> messages)
	{
		var authorizeAttribute = ClassOrBaseClassHasAttribute(classNamedSymbol, "AuthorizeAttribute");
		var callMethodInfos = new List<CallMethodInfo>();

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

			   var callMethodInfo = new CallMethodInfo(classNamedSymbol, method, methodDeclaration);

			   if (callMethodInfo.AuthorizeOperation != null)
			   {
				  callMethodInfos.Add(callMethodInfo);
			   }
			   else
			   {
				  messages.Add($"No DataMapperMethodType for {authorizeAttribute}");
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

		return callMethodInfos;
	}

	private static void MatchAuthMethods(IEnumerable<FactoryMethod> factoryMethods, List<CallMethodInfo> authCallMethods, List<string> messages)
	{
		foreach (var method in factoryMethods)
		{
			foreach (var authMethod in authCallMethods)
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
						if (authMethodParameter.Current.Type != methodParameter.Current?.Type)
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
}