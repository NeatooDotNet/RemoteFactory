using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Neatoo.Factory;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo;

[Generator(LanguageNames.CSharp)]
public partial class Factory : IIncrementalGenerator
{
	public static int? MaxHintNameLength { get; set; }

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{

		var classesToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName("Neatoo.RemoteFactory.FactoryAttribute",
			predicate: static (s, _) => s is ClassDeclarationSyntax classDeclarationSyntax
				&& !(classDeclarationSyntax.TypeParameterList?.Parameters.Any() ?? false || classDeclarationSyntax.Modifiers.Any(SyntaxKind.AbstractKeyword))
				&& !(classDeclarationSyntax.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "SuppressFactory")),
			transform: static (ctx, _) =>
			{
				var classDeclaration = (ClassDeclarationSyntax)ctx.TargetNode;
				var semanticModel = ctx.SemanticModel;
				return TransformClassFactory(classDeclaration, semanticModel);
			});

		context.RegisterSourceOutput(classesToGenerate, static (spc, typeInfo) =>
		{
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

	private static void GenerateFactory(SourceProductionContext context, TypeInfo typeInfo)
	{
		var messages = new List<string>();
		string source;

		try
		{
			var factoryText = new FactoryText();

			try
			{
				// Report diagnostics collected during transform phase (warnings like NF0201, NF0202, NF0204)
				foreach (var diag in typeInfo.Diagnostics)
				{
					ReportDiagnostic(context, diag);
				}

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
							// Check for ambiguous save operations within same name postfix
							var insertMethods = byNameMethod.Where(m => m.FactoryOperation == FactoryOperation.Insert).ToList();
							var updateMethods = byNameMethod.Where(m => m.FactoryOperation == FactoryOperation.Update).ToList();
							var deleteMethods = byNameMethod.Where(m => m.FactoryOperation == FactoryOperation.Delete).ToList();

							if (insertMethods.Count > 1 || updateMethods.Count > 1 || deleteMethods.Count > 1)
							{
								// NF0203: Report warning for ambiguous save operations (same name postfix)
								string operationType;
								List<WriteFactoryMethod> conflictingMethods;
								if (insertMethods.Count > 1)
								{
									operationType = "Insert";
									conflictingMethods = insertMethods;
								}
								else if (updateMethods.Count > 1)
								{
									operationType = "Update";
									conflictingMethods = updateMethods;
								}
								else
								{
									operationType = "Delete";
									conflictingMethods = deleteMethods;
								}

								var conflictingMethodNames = string.Join(", ", conflictingMethods.Select(m => m.Name));

								// Report on the first conflicting method
								var firstMethod = conflictingMethods.First();
								var diagnostic = new DiagnosticInfo(
									"NF0203",
									firstMethod.CallMethod.MethodFilePath,
									firstMethod.CallMethod.MethodStartLine,
									firstMethod.CallMethod.MethodStartColumn,
									firstMethod.CallMethod.MethodEndLine,
									firstMethod.CallMethod.MethodEndColumn,
									firstMethod.CallMethod.MethodTextSpanStart,
									firstMethod.CallMethod.MethodTextSpanLength,
									operationType,
									conflictingMethodNames);
								ReportDiagnostic(context, diagnostic);

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
		List<DiagnosticInfo> localDiagnostics = [];
		bool hasErrors = false;

		try
		{
			var delegates = new StringBuilder();
			var remoteMethods = new StringBuilder();
			var localMethods = new StringBuilder();

			try
			{
				// Report diagnostics collected during transform phase (e.g., NF0103)
				foreach (var diag in typeInfo.Diagnostics)
				{
					ReportDiagnostic(context, diag);
					hasErrors = true;
				}

				// NF0101: Class must be partial for factory generation
				if (!typeInfo.IsPartial)
				{
					var diagnostic = new DiagnosticInfo(
						"NF0101",
						typeInfo.ClassFilePath,
						typeInfo.ClassStartLine,
						typeInfo.ClassStartColumn,
						typeInfo.ClassEndLine,
						typeInfo.ClassEndColumn,
						typeInfo.ClassTextSpanStart,
						typeInfo.ClassTextSpanLength,
						typeInfo.Name);
					ReportDiagnostic(context, diagnostic);
					messages.Add($"Class {typeInfo.Name} is not partial. Cannot generate factory.");
					hasErrors = true;
					return;
				}


				foreach (var method in typeInfo.FactoryMethods)
				{

					if (method.FactoryOperation == FactoryOperation.Execute)
					{

						// NF0102: Execute method must return Task
						if (!method.IsTask)
						{
							var diagnostic = new DiagnosticInfo(
								"NF0102",
								method.MethodFilePath,
								method.MethodStartLine,
								method.MethodStartColumn,
								method.MethodEndLine,
								method.MethodEndColumn,
								method.MethodTextSpanStart,
								method.MethodTextSpanLength,
								method.Name,
								method.ReturnType ?? "void");
							ReportDiagnostic(context, diagnostic);
							messages.Add($"{method.Name} skipped. Delegates must return Task not {method.ReturnType}");
							hasErrors = true;
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

				// Skip code generation if there are errors
				if (hasErrors)
				{
					return;
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

	private static DiagnosticDescriptor GetDescriptor(string diagnosticId)
	{
		return diagnosticId switch
		{
			"NF0101" => DiagnosticDescriptors.ClassMustBePartial,
			"NF0102" => DiagnosticDescriptors.ExecuteMustReturnTask,
			"NF0103" => DiagnosticDescriptors.ExecuteRequiresStaticClass,
			"NF0104" => DiagnosticDescriptors.HintNameTruncated,
			"NF0201" => DiagnosticDescriptors.FactoryMethodMustBeStatic,
			"NF0202" => DiagnosticDescriptors.AuthMethodWrongReturnType,
			"NF0203" => DiagnosticDescriptors.AmbiguousSaveOperations,
			"NF0204" => DiagnosticDescriptors.WriteReturnsTargetType,
			"NF0301" => DiagnosticDescriptors.MethodSkippedNoAttribute,
			_ => throw new ArgumentException($"Unknown diagnostic ID: {diagnosticId}")
		};
	}

	private static void ReportDiagnostic(SourceProductionContext context, DiagnosticInfo diagnosticInfo)
	{
		var descriptor = GetDescriptor(diagnosticInfo.DiagnosticId);

		// Create a Location from the stored info
		// We need to create a TextSpan and LinePositionSpan
		var textSpan = new Microsoft.CodeAnalysis.Text.TextSpan(diagnosticInfo.TextSpanStart, diagnosticInfo.TextSpanLength);
		var startLinePosition = new Microsoft.CodeAnalysis.Text.LinePosition(diagnosticInfo.StartLine, diagnosticInfo.StartColumn);
		var endLinePosition = new Microsoft.CodeAnalysis.Text.LinePosition(diagnosticInfo.EndLine, diagnosticInfo.EndColumn);
		var linePositionSpan = new Microsoft.CodeAnalysis.Text.LinePositionSpan(startLinePosition, endLinePosition);

		var location = Location.Create(diagnosticInfo.FilePath, textSpan, linePositionSpan);

		var messageArgs = diagnosticInfo.MessageArgs.GetArray() ?? Array.Empty<string>();
		context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
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

	/// <summary>
	/// Result of SafeHintName processing, including truncation information.
	/// </summary>
	internal readonly struct SafeHintNameResult
	{
		public string ResultName { get; }
		public string OriginalName { get; }
		public int MaxLength { get; }
		public bool WasTruncated => ResultName != OriginalName;

		public SafeHintNameResult(string resultName, string originalName, int maxLength)
		{
			ResultName = resultName;
			OriginalName = originalName;
			MaxLength = maxLength;
		}
	}

	internal static SafeHintNameResult SafeHintName(SemanticModel semanticModel, string hintName, int? maxLength = null)
	{
		var originalName = hintName;

		if (maxLength == null)
		{
			var hintNameLengthAttribute = semanticModel.Compilation.Assembly.GetAttributes()
				.Where(a => a.AttributeClass?.Name == "FactoryHintNameLengthAttribute")
				.FirstOrDefault();

			maxLength = hintNameLengthAttribute?.ConstructorArguments.FirstOrDefault().Value is int length ? length : 50;
		}

		var resultName = TruncateHintName(hintName, maxLength.Value);
		return new SafeHintNameResult(resultName, originalName, maxLength.Value);
	}

	private static string TruncateHintName(string hintName, int maxLength)
	{
		if (hintName.Length > maxLength)
		{
			if (hintName.Contains('.'))
			{
				return TruncateHintName(hintName.Substring(hintName.IndexOf('.') + 1, hintName.Length - hintName.IndexOf('.') - 1), maxLength);
			}
			else
			{
				return hintName.Substring(hintName.Length - maxLength, maxLength);
			}
		}
		return hintName;
	}
}