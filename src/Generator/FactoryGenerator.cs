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
			predicate: static (s, _) => s is TypeDeclarationSyntax typeDecl
				&& typeDecl is (ClassDeclarationSyntax or RecordDeclarationSyntax)
				&& !(typeDecl.TypeParameterList?.Parameters.Any() ?? false || typeDecl.Modifiers.Any(SyntaxKind.AbstractKeyword))
				&& !(typeDecl.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == "SuppressFactory")),
			transform: static (ctx, _) =>
			{
				var typeDeclaration = (TypeDeclarationSyntax)ctx.TargetNode;
				var semanticModel = ctx.SemanticModel;
				return TransformTypeFactory(typeDeclaration, semanticModel);
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
				// Generate ordinal serialization support
				GenerateOrdinalSerialization(spc, typeInfo);
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
				var eventDelegates = new StringBuilder();
				var eventRegistrations = new StringBuilder();
				var remoteEventRegistrations = new StringBuilder();
				bool hasEvents = false;

				foreach (var targetCallMethod in typeInfo.FactoryMethods)
				{
					// Handle [Event] methods separately
					if (targetCallMethod.FactoryOperation == FactoryOperation.Event)
					{
						var eventResult = GenerateEventMethodForNonStatic(context, typeInfo, targetCallMethod, messages);
						if (!eventResult.HasError)
						{
							hasEvents = true;
							eventDelegates.AppendLine(eventResult.DelegateDeclaration);
							eventRegistrations.AppendLine(eventResult.EventRegistration);
							remoteEventRegistrations.AppendLine(eventResult.RemoteRegistration);
						}
						continue;
					}

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
									 .GroupBy(m => string.Join(",", m.Parameters.Where(m => !m.IsTarget && !m.IsService && !m.IsCancellationToken)
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
									 .Where(s => s.Parameters.Where(p => !p.IsTarget && !p.IsService && !p.IsCancellationToken).Count() == 0 && s.Parameters.First().IsTarget)
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

				// Register AOT-compatible ordinal converter if ordinal serialization was generated
				// Must match the condition in GenerateOrdinalSerialization:
				// - Has ordinal properties
				// - Is partial
				// - Not nested
				// - If record, must have primary constructor
				// - Does not require DI for instantiation
				var shouldGenerateOrdinal = typeInfo.OrdinalProperties.Any() &&
					typeInfo.IsPartial &&
					!typeInfo.IsNested &&
					(!typeInfo.IsRecord || typeInfo.HasPrimaryConstructor) &&
					!typeInfo.RequiresServiceInstantiation;

				if (shouldGenerateOrdinal)
				{
					factoryText.ServiceRegistrations.AppendLine($@"
								// Register AOT-compatible ordinal converter
								global::Neatoo.RemoteFactory.Internal.NeatooOrdinalConverterFactory.RegisterConverter(
									{typeInfo.ImplementationTypeName}.CreateOrdinalConverter());");
				}

				// Generate event partial class if there are events
				var eventPartialClass = "";
				var hostingUsing = "";
				if (hasEvents && typeInfo.IsPartial)
				{
					hostingUsing = "using Microsoft.Extensions.Hosting;";
					eventPartialClass = $@"
                        // Event delegates for {typeInfo.ImplementationTypeName}
                        public partial class {typeInfo.ImplementationTypeName}
                        {{
{eventDelegates}
                        }}";
				}

				source = $@"
						  #nullable enable

                    {WithStringBuilder(typeInfo.UsingStatements)}
					{hostingUsing}

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
*/
                    namespace {typeInfo.Namespace}
                    {{
{eventPartialClass}

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

                                // Event registrations
                                if(remoteLocal == NeatooFactory.Remote)
                                {{
{remoteEventRegistrations}
                                }}
                                if(remoteLocal == NeatooFactory.Logical || remoteLocal == NeatooFactory.Server)
                                {{
{eventRegistrations}
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
			var eventMethods = new StringBuilder();

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
						var hasCancellationToken = parameters.Any(p => p.IsCancellationToken);

						// Build parameter declarations excluding services and existing CancellationToken, then add optional CancellationToken
						var paramsWithoutServiceAndCt = parameters.Where(p => !p.IsService && !p.IsCancellationToken).ToList();
						var parameterDeclarations = string.Join(", ", paramsWithoutServiceAndCt.Select(p => $"{p.Type} {p.Name}"));
						if (!string.IsNullOrEmpty(parameterDeclarations))
						{
							parameterDeclarations += ", ";
						}
						parameterDeclarations += "CancellationToken cancellationToken = default";

						// Parameter identifiers for factory signature (always includes cancellationToken)
						var parameterIdentifiers = string.Join(", ", paramsWithoutServiceAndCt.Select(p => p.Name));
						if (!string.IsNullOrEmpty(parameterIdentifiers))
						{
							parameterIdentifiers += ", ";
						}
						parameterIdentifiers += "cancellationToken";

						// For serialization, exclude CancellationToken - it flows through HTTP layer instead
						var serializedParameterIdentifiers = string.Join(", ", paramsWithoutServiceAndCt.Select(p => p.Name));

						// Build parameter identifiers for domain method invocation (include CT only if method has it)
						var domainMethodParams = parameters.Where(p => !p.IsCancellationToken).ToList();
						var allParameterIdentifiers = string.Join(", ", domainMethodParams.Select(p => p.Name));
						if (hasCancellationToken)
						{
							if (!string.IsNullOrEmpty(allParameterIdentifiers))
							{
								allParameterIdentifiers += ", ";
							}
							allParameterIdentifiers += "cancellationToken";
						}

						var serviceAssignmentsText = WithStringBuilder(parameters.Where(p => p.IsService).Select(p => $"var {p.Name} = cc.GetRequiredService<{p.Type}>();"));

						delegates.AppendLine($"public delegate Task<{method.ReturnType}> {delegateName}({parameterDeclarations});");

						remoteMethods.AppendLine(@$"
						  services.AddTransient<{typeInfo.Name}.{delegateName}>(cc =>
						  {{
								return ({parameterIdentifiers}) => cc.GetRequiredService<IMakeRemoteDelegateRequest>().ForDelegate{nullableText}<{method.ReturnType}>(typeof({typeInfo.Name}.{delegateName}), [{serializedParameterIdentifiers}], cancellationToken);
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
					else if (method.FactoryOperation == FactoryOperation.Event)
					{
						// Generate event delegate with scope isolation
						var eventResult = GenerateEventMethod(context, typeInfo, method, messages);
						if (eventResult.HasError)
						{
							hasErrors = true;
							continue;
						}

						delegates.AppendLine(eventResult.DelegateDeclaration);
						eventMethods.AppendLine(eventResult.EventRegistration);
						remoteMethods.AppendLine(eventResult.RemoteRegistration);
					}
				}

				// Skip code generation if there are errors
				if (hasErrors)
				{
					return;
				}

				// Add hosting using only if there are event methods
				var hasEventMethods = typeInfo.FactoryMethods.Any(m => m.FactoryOperation == FactoryOperation.Event);
				var hostingUsing = hasEventMethods ? "using Microsoft.Extensions.Hosting;" : "";

				source = $@"
						  #nullable enable

                    {WithStringBuilder(typeInfo.UsingStatements)}
					{hostingUsing}

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
{eventMethods}
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

	/// <summary>
	/// Result of generating an event method.
	/// </summary>
	private readonly struct EventMethodResult
	{
		public bool HasError { get; }
		public string DelegateDeclaration { get; }
		public string EventRegistration { get; }
		public string RemoteRegistration { get; }

		private EventMethodResult(bool hasError, string delegateDeclaration = "", string eventRegistration = "", string remoteRegistration = "")
		{
			HasError = hasError;
			DelegateDeclaration = delegateDeclaration;
			EventRegistration = eventRegistration;
			RemoteRegistration = remoteRegistration;
		}

		public static EventMethodResult Error() => new(true);
		public static EventMethodResult Success(string delegateDeclaration, string eventRegistration, string remoteRegistration)
			=> new(false, delegateDeclaration, eventRegistration, remoteRegistration);
	}

	/// <summary>
	/// Generates event delegate and registration code for a method with [Event] attribute in a non-static class.
	/// Events use scope isolation and fire-and-forget semantics.
	/// </summary>
	private static EventMethodResult GenerateEventMethodForNonStatic(
		SourceProductionContext context,
		TypeInfo typeInfo,
		TypeFactoryMethodInfo method,
		List<string> messages)
	{
		var parameters = method.Parameters;
		var returnType = method.ReturnType ?? "void";

		// For event methods, valid returns are:
		// - void: isVoid=true, isTask=false, returnType="void"
		// - Task (non-generic): isTask=true, returnType could be "void", "Task", or full Task type name
		// - Invalid: Task<T> where returnType is T, or any other non-void/non-Task type
		var isVoidOrTaskReturn = returnType == "void" ||
								 string.IsNullOrEmpty(returnType) ||
								 returnType == "Task" ||
								 returnType.EndsWith("Task") ||
								 returnType == "System.Threading.Tasks.Task";
		var isTask = method.IsTask;
		var isVoid = !isTask; // If not async Task, it must be void (validated below)

		// NF0401: Event method must return void or Task (not Task<T> or ValueTask)
		// If isTask is true but returnType is not a recognized Task pattern, it's Task<T>
		if (isTask && !isVoidOrTaskReturn)
		{
			var diagnostic = new DiagnosticInfo(
				"NF0401",
				method.MethodFilePath,
				method.MethodStartLine,
				method.MethodStartColumn,
				method.MethodEndLine,
				method.MethodEndColumn,
				method.MethodTextSpanStart,
				method.MethodTextSpanLength,
				method.Name,
				$"Task<{returnType}>");
			ReportDiagnostic(context, diagnostic);
			messages.Add($"{method.Name} skipped. Event methods must return void or Task, not Task<{returnType}>");
			return EventMethodResult.Error();
		}

		// If not a Task and not void, it's an invalid return type
		if (!isTask && !isVoidOrTaskReturn)
		{
			var diagnostic = new DiagnosticInfo(
				"NF0401",
				method.MethodFilePath,
				method.MethodStartLine,
				method.MethodStartColumn,
				method.MethodEndLine,
				method.MethodEndColumn,
				method.MethodTextSpanStart,
				method.MethodTextSpanLength,
				method.Name,
				returnType);
			ReportDiagnostic(context, diagnostic);
			messages.Add($"{method.Name} skipped. Event methods must return void or Task, not {returnType}");
			return EventMethodResult.Error();
		}

		// NF0404: Event method must have CancellationToken as final parameter
		var lastParam = parameters.LastOrDefault();
		if (lastParam == null || !lastParam.IsCancellationToken)
		{
			var diagnostic = new DiagnosticInfo(
				"NF0404",
				method.MethodFilePath,
				method.MethodStartLine,
				method.MethodStartColumn,
				method.MethodEndLine,
				method.MethodEndColumn,
				method.MethodTextSpanStart,
				method.MethodTextSpanLength,
				method.Name);
			ReportDiagnostic(context, diagnostic);
			messages.Add($"{method.Name} skipped. Event methods must have CancellationToken as final parameter");
			return EventMethodResult.Error();
		}

		// NF0403: Warning if no non-service parameters (excluding CancellationToken)
		var dataParameters = parameters.Where(p => !p.IsService && !p.IsCancellationToken).ToList();
		if (!dataParameters.Any())
		{
			var diagnostic = new DiagnosticInfo(
				"NF0403",
				method.MethodFilePath,
				method.MethodStartLine,
				method.MethodStartColumn,
				method.MethodEndLine,
				method.MethodEndColumn,
				method.MethodTextSpanStart,
				method.MethodTextSpanLength,
				method.Name);
			ReportDiagnostic(context, diagnostic);
			// This is just a warning, don't return error
		}

		// Generate delegate name with Event suffix
		var delegateName = method.Name;
		if (!delegateName.EndsWith("Event"))
		{
			delegateName = $"{delegateName}Event";
		}

		// Parameter declarations for delegate (exclude [Service] and CancellationToken)
		var delegateParameterDeclarations = string.Join(", ", parameters
			.Where(p => !p.IsService && !p.IsCancellationToken)
			.Select(p => $"{p.Type} {p.Name}"));

		// Parameter identifiers for delegate invocation
		var delegateParameterIdentifiers = string.Join(", ", parameters
			.Where(p => !p.IsService && !p.IsCancellationToken)
			.Select(p => p.Name));

		// All parameter identifiers for actual method call (including services and CT)
		var allParameterIdentifiers = string.Join(", ", parameters.Select(p => p.Name));

		// Service assignments in the isolated scope
		var serviceAssignmentsText = WithStringBuilder(parameters
			.Where(p => p.IsService)
			.Select(p => $"var {p.Name} = scope.ServiceProvider.GetRequiredService<{p.Type}>();"));

		// Method invocation - handle void vs Task
		string methodInvocation;
		if (isVoid)
		{
			methodInvocation = $@"handler.{method.Name}({allParameterIdentifiers});";
		}
		else
		{
			methodInvocation = $@"await handler.{method.Name}({allParameterIdentifiers});";
		}

		// Generate delegate declaration
		var delegateDeclaration = $"public delegate Task {delegateName}({delegateParameterDeclarations});";

		// Generate event registration with scope isolation
		var eventRegistration = $@"
						  services.AddScoped<{typeInfo.ImplementationTypeName}.{delegateName}>(sp =>
						  {{
								var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
								var tracker = sp.GetRequiredService<IEventTracker>();
								var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
								return ({delegateParameterIdentifiers}) =>
								{{
									var task = Task.Run(async () =>
									{{
										using var scope = scopeFactory.CreateScope();
										var ct = lifetime.ApplicationStopping;
										var handler = scope.ServiceProvider.GetRequiredService<{typeInfo.ImplementationTypeName}>();
										{serviceAssignmentsText}
										{methodInvocation}
									}});
									tracker.Track(task);
									return task;
								}};
						  }});";

		// For serialization, exclude CancellationToken - it flows through HTTP layer instead
		var serializedParameterIdentifiers = string.Join(", ", parameters
			.Where(p => !p.IsService && !p.IsCancellationToken)
			.Select(p => p.Name));

		// Generate remote registration
		var remoteRegistration = $@"
						  services.AddTransient<{typeInfo.ImplementationTypeName}.{delegateName}>(cc =>
						  {{
								return ({delegateParameterIdentifiers}) => cc.GetRequiredService<IMakeRemoteDelegateRequest>().ForDelegateEvent(typeof({typeInfo.ImplementationTypeName}.{delegateName}), [{serializedParameterIdentifiers}]);
						  }});";

		return EventMethodResult.Success(delegateDeclaration, eventRegistration, remoteRegistration);
	}

	/// <summary>
	/// Generates event delegate and registration code for a method with [Event] attribute.
	/// Events use scope isolation and fire-and-forget semantics.
	/// </summary>
	private static EventMethodResult GenerateEventMethod(
		SourceProductionContext context,
		TypeInfo typeInfo,
		TypeFactoryMethodInfo method,
		List<string> messages)
	{
		var parameters = method.Parameters;
		var returnType = method.ReturnType ?? "void";

		// For event methods, valid returns are:
		// - void: isVoid=true, isTask=false, returnType="void"
		// - Task (non-generic): isTask=true, returnType could be "void", "Task", or full Task type name
		// - Invalid: Task<T> where returnType is T, or any other non-void/non-Task type
		var isVoidOrTaskReturn = returnType == "void" ||
								 string.IsNullOrEmpty(returnType) ||
								 returnType == "Task" ||
								 returnType.EndsWith("Task") ||
								 returnType == "System.Threading.Tasks.Task";
		var isTask = method.IsTask;
		var isVoid = !isTask; // If not async Task, it must be void (validated below)

		// NF0401: Event method must return void or Task (not Task<T> or ValueTask)
		// If isTask is true but returnType is not a recognized Task pattern, it's Task<T>
		if (isTask && !isVoidOrTaskReturn)
		{
			var diagnostic = new DiagnosticInfo(
				"NF0401",
				method.MethodFilePath,
				method.MethodStartLine,
				method.MethodStartColumn,
				method.MethodEndLine,
				method.MethodEndColumn,
				method.MethodTextSpanStart,
				method.MethodTextSpanLength,
				method.Name,
				$"Task<{returnType}>");
			ReportDiagnostic(context, diagnostic);
			messages.Add($"{method.Name} skipped. Event methods must return void or Task, not Task<{returnType}>");
			return EventMethodResult.Error();
		}

		// If not a Task and not void, it's an invalid return type
		if (!isTask && !isVoidOrTaskReturn)
		{
			var diagnostic = new DiagnosticInfo(
				"NF0401",
				method.MethodFilePath,
				method.MethodStartLine,
				method.MethodStartColumn,
				method.MethodEndLine,
				method.MethodEndColumn,
				method.MethodTextSpanStart,
				method.MethodTextSpanLength,
				method.Name,
				returnType);
			ReportDiagnostic(context, diagnostic);
			messages.Add($"{method.Name} skipped. Event methods must return void or Task, not {returnType}");
			return EventMethodResult.Error();
		}

		// NF0404: Event method must have CancellationToken as final parameter
		var lastParam = parameters.LastOrDefault();
		if (lastParam == null || !lastParam.IsCancellationToken)
		{
			var diagnostic = new DiagnosticInfo(
				"NF0404",
				method.MethodFilePath,
				method.MethodStartLine,
				method.MethodStartColumn,
				method.MethodEndLine,
				method.MethodEndColumn,
				method.MethodTextSpanStart,
				method.MethodTextSpanLength,
				method.Name);
			ReportDiagnostic(context, diagnostic);
			messages.Add($"{method.Name} skipped. Event methods must have CancellationToken as final parameter");
			return EventMethodResult.Error();
		}

		// NF0403: Warning if no non-service parameters (excluding CancellationToken)
		var dataParameters = parameters.Where(p => !p.IsService && !p.IsCancellationToken).ToList();
		if (!dataParameters.Any())
		{
			var diagnostic = new DiagnosticInfo(
				"NF0403",
				method.MethodFilePath,
				method.MethodStartLine,
				method.MethodStartColumn,
				method.MethodEndLine,
				method.MethodEndColumn,
				method.MethodTextSpanStart,
				method.MethodTextSpanLength,
				method.Name);
			ReportDiagnostic(context, diagnostic);
			// This is just a warning, don't return error
		}

		// Generate delegate name with Event suffix
		var delegateName = method.Name;
		if (!delegateName.EndsWith("Event"))
		{
			delegateName = $"{delegateName}Event";
		}

		// Parameter declarations for delegate (exclude [Service] and CancellationToken)
		var delegateParameterDeclarations = string.Join(", ", parameters
			.Where(p => !p.IsService && !p.IsCancellationToken)
			.Select(p => $"{p.Type} {p.Name}"));

		// Parameter identifiers for delegate invocation
		var delegateParameterIdentifiers = string.Join(", ", parameters
			.Where(p => !p.IsService && !p.IsCancellationToken)
			.Select(p => p.Name));

		// All parameter identifiers for actual method call (including services and CT)
		var allParameterIdentifiers = string.Join(", ", parameters.Select(p => p.Name));

		// Service assignments in the isolated scope
		var serviceAssignmentsText = WithStringBuilder(parameters
			.Where(p => p.IsService)
			.Select(p => $"var {p.Name} = scope.ServiceProvider.GetRequiredService<{p.Type}>();"));

		// Method invocation for static classes - call directly on type, not via handler instance
		string methodInvocation;
		if (isVoid)
		{
			methodInvocation = $@"{typeInfo.Name}.{method.Name}({allParameterIdentifiers});";
		}
		else
		{
			methodInvocation = $@"await {typeInfo.Name}.{method.Name}({allParameterIdentifiers});";
		}

		// Generate delegate declaration
		var delegateDeclaration = $"public delegate Task {delegateName}({delegateParameterDeclarations});";

		// Generate event registration with scope isolation (no handler resolution for static class)
		var eventRegistration = $@"
						  services.AddScoped<{typeInfo.Name}.{delegateName}>(sp =>
						  {{
								var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
								var tracker = sp.GetRequiredService<IEventTracker>();
								var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
								return ({delegateParameterIdentifiers}) =>
								{{
									var task = Task.Run(async () =>
									{{
										using var scope = scopeFactory.CreateScope();
										var ct = lifetime.ApplicationStopping;
										{serviceAssignmentsText}
										{methodInvocation}
									}});
									tracker.Track(task);
									return task;
								}};
						  }});";

		// For serialization, exclude CancellationToken - it flows through HTTP layer instead
		var serializedParameterIdentifiers = string.Join(", ", parameters
			.Where(p => !p.IsService && !p.IsCancellationToken)
			.Select(p => p.Name));

		// Generate remote registration
		var remoteRegistration = $@"
						  services.AddTransient<{typeInfo.Name}.{delegateName}>(cc =>
						  {{
								return ({delegateParameterIdentifiers}) => cc.GetRequiredService<IMakeRemoteDelegateRequest>().ForDelegateEvent(typeof({typeInfo.Name}.{delegateName}), [{serializedParameterIdentifiers}]);
						  }});";

		return EventMethodResult.Success(delegateDeclaration, eventRegistration, remoteRegistration);
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
			"NF0205" => DiagnosticDescriptors.CreateOnTypeRequiresRecordWithPrimaryConstructor,
			"NF0206" => DiagnosticDescriptors.RecordStructNotSupported,
			"NF0207" => DiagnosticDescriptors.NestedTypeOrdinalSkipped,
			"NF0301" => DiagnosticDescriptors.MethodSkippedNoAttribute,
			"NF0401" => DiagnosticDescriptors.EventMustReturnVoidOrTask,
			"NF0402" => DiagnosticDescriptors.EventRequiresFactoryClass,
			"NF0403" => DiagnosticDescriptors.EventNoNonServiceParameters,
			"NF0404" => DiagnosticDescriptors.EventMustHaveCancellationToken,
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

	/// <summary>
	/// Generates the IOrdinalSerializable implementation for a type.
	/// This enables compact JSON serialization using arrays instead of objects with property names.
	/// Also generates a strongly-typed ordinal converter to eliminate runtime reflection.
	/// </summary>
	private static void GenerateOrdinalSerialization(SourceProductionContext context, TypeInfo typeInfo)
	{
		// Skip if no properties or if type is not partial
		if (!typeInfo.OrdinalProperties.Any() || !typeInfo.IsPartial)
		{
			return;
		}

		// Skip nested types - they require special code generation that we don't support yet
		// Report an info diagnostic so users know why ordinal serialization was skipped
		if (typeInfo.IsNested)
		{
			var diagnostic = new DiagnosticInfo(
				"NF0207",
				typeInfo.ClassFilePath,
				typeInfo.ClassStartLine,
				typeInfo.ClassStartColumn,
				typeInfo.ClassEndLine,
				typeInfo.ClassEndColumn,
				typeInfo.ClassTextSpanStart,
				typeInfo.ClassTextSpanLength,
				typeInfo.Name);
			ReportDiagnostic(context, diagnostic);
			return;
		}

		// Skip types that require DI for instantiation (have constructors with non-service parameters)
		// These types cannot be deserialized using object initializer syntax
		if (typeInfo.RequiresServiceInstantiation)
		{
			return;
		}

		try
		{
			var properties = typeInfo.OrdinalProperties.ToList();
			var propertyNamesArray = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
			// Strip nullable annotation from types for typeof() operator (CS8639)
			var propertyTypesArray = string.Join(", ", properties.Select(p =>
			{
				var typeName = p.Type;
				if (typeName.EndsWith("?") && !typeName.Contains("<"))
				{
					// Simple nullable type like "string?" -> "string"
					typeName = typeName.TrimEnd('?');
				}
				return $"typeof({typeName})";
			}));
			var toArrayValues = string.Join(", ", properties.Select(p => $"this.{p.Name}"));
			var fullTypeName = $"{typeInfo.Namespace}.{typeInfo.Name}";
			var constructorParams = typeInfo.PrimaryConstructorParameterNames.ToList();
			var hasPrimaryConstructor = typeInfo.IsRecord && typeInfo.HasPrimaryConstructor && constructorParams.Count > 0;

			// For records without primary constructors, skip ordinal serialization
			// (they have required constructor arguments that we can't populate)
			if (typeInfo.IsRecord && !hasPrimaryConstructor)
			{
				return;
			}

			// Build a mapping from property name to ordinal index
			var propertyToIndex = new Dictionary<string, int>();
			for (int i = 0; i < properties.Count; i++)
			{
				propertyToIndex[properties[i].Name] = i;
			}

			// Build a mapping from property name to its info
			var propertyByName = properties.ToDictionary(p => p.Name, p => p);

			// Generate FromOrdinalArray code based on whether type has primary constructor
			string fromArrayCode;
			if (hasPrimaryConstructor)
			{
				// For records with primary constructors, use constructor syntax
				// Arguments must be in constructor parameter order, not alphabetical order
				var constructorArgs = new List<string>();
				foreach (var paramName in constructorParams)
				{
					if (propertyToIndex.TryGetValue(paramName, out var idx))
					{
						var prop = properties[idx];
						var cast = $"({prop.Type})";
						var isEffectivelyNullable = prop.IsNullable || prop.Type.EndsWith("?");
						var nullForgiving = isEffectivelyNullable ? "" : "!";
						constructorArgs.Add($"{cast}values[{idx}]{nullForgiving}");
					}
				}
				fromArrayCode = $"return new {typeInfo.Name}({string.Join(", ", constructorArgs)});";
			}
			else
			{
				// For classes and records without primary constructors, use object initializer
				var fromArrayAssignments = new StringBuilder();
				for (int i = 0; i < properties.Count; i++)
				{
					var prop = properties[i];
					var cast = $"({prop.Type})";
					var isEffectivelyNullable = prop.IsNullable || prop.Type.EndsWith("?");
					var nullForgiving = isEffectivelyNullable ? "" : "!";
					fromArrayAssignments.AppendLine($"\t\t\t\t{prop.Name} = {cast}values[{i}]{nullForgiving}{(i < properties.Count - 1 ? "," : "")}");
				}
				fromArrayCode = $@"return new {typeInfo.Name}
			{{
{fromArrayAssignments}
			}};";
			}

			// Generate converter Read method property deserialization
			var converterReadStatements = new StringBuilder();
			for (int i = 0; i < properties.Count; i++)
			{
				var prop = properties[i];
				converterReadStatements.AppendLine($"\t\t\t// {prop.Name} ({prop.Type}) - position {i}");
				converterReadStatements.AppendLine($"\t\t\tvar prop{i} = global::System.Text.Json.JsonSerializer.Deserialize<{prop.Type}>(ref reader, options);");
				converterReadStatements.AppendLine($"\t\t\treader.Read();");
			}

			// Generate converter object construction
			string converterConstructCode;
			if (hasPrimaryConstructor)
			{
				// For records with primary constructors, use constructor syntax
				var constructorArgs = new List<string>();
				foreach (var paramName in constructorParams)
				{
					if (propertyToIndex.TryGetValue(paramName, out var idx))
					{
						var prop = properties[idx];
						var isEffectivelyNullable = prop.IsNullable || prop.Type.EndsWith("?");
						var nullForgiving = isEffectivelyNullable ? "" : "!";
						constructorArgs.Add($"prop{idx}{nullForgiving}");
					}
				}
				converterConstructCode = $"return new {fullTypeName}({string.Join(", ", constructorArgs)});";
			}
			else
			{
				// For classes and records without primary constructors, use object initializer
				var converterConstructAssignments = new StringBuilder();
				for (int i = 0; i < properties.Count; i++)
				{
					var prop = properties[i];
					var isEffectivelyNullable = prop.IsNullable || prop.Type.EndsWith("?");
					var nullForgiving = isEffectivelyNullable ? "" : "!";
					converterConstructAssignments.AppendLine($"\t\t\t\t\t{prop.Name} = prop{i}{nullForgiving}{(i < properties.Count - 1 ? "," : "")}");
				}
				converterConstructCode = $@"return new {fullTypeName}
			{{
{converterConstructAssignments}
			}};";
			}

			// Generate converter Write method property serialization
			var converterWriteStatements = new StringBuilder();
			for (int i = 0; i < properties.Count; i++)
			{
				var prop = properties[i];
				converterWriteStatements.AppendLine($"\t\t\tglobal::System.Text.Json.JsonSerializer.Serialize(writer, value.{prop.Name}, options);");
			}

			var recordKeyword = typeInfo.IsRecord ? "record" : "class";

			var source = $@"
#nullable enable

{WithStringBuilder(typeInfo.UsingStatements)}

/*
	READONLY - DO NOT EDIT!!!!
	Generated by Neatoo.RemoteFactory - Ordinal Serialization Support
*/
namespace {typeInfo.Namespace}
{{
	/// <summary>
	/// Strongly-typed ordinal converter for {typeInfo.Name}. No reflection required.
	/// </summary>
	internal sealed class {typeInfo.Name}OrdinalConverter : global::System.Text.Json.Serialization.JsonConverter<{fullTypeName}>
	{{
		public override {fullTypeName}? Read(
			ref global::System.Text.Json.Utf8JsonReader reader,
			global::System.Type typeToConvert,
			global::System.Text.Json.JsonSerializerOptions options)
		{{
			if (reader.TokenType == global::System.Text.Json.JsonTokenType.Null)
				return null;

			if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartArray)
				throw new global::System.Text.Json.JsonException(
					$""Expected StartArray for {typeInfo.Name} ordinal format, got {{reader.TokenType}}"");

			reader.Read(); // Move past StartArray

{converterReadStatements}
			if (reader.TokenType != global::System.Text.Json.JsonTokenType.EndArray)
				throw new global::System.Text.Json.JsonException(
					$""Too many values in ordinal array for {typeInfo.Name}. Expected {properties.Count}."");

			{converterConstructCode}
		}}

		public override void Write(
			global::System.Text.Json.Utf8JsonWriter writer,
			{fullTypeName} value,
			global::System.Text.Json.JsonSerializerOptions options)
		{{
			if (value == null)
			{{
				writer.WriteNullValue();
				return;
			}}

			writer.WriteStartArray();
{converterWriteStatements}
			writer.WriteEndArray();
		}}
	}}

	partial {recordKeyword} {typeInfo.Name} : global::Neatoo.RemoteFactory.IOrdinalSerializable, global::Neatoo.RemoteFactory.IOrdinalSerializationMetadata, global::Neatoo.RemoteFactory.IOrdinalConverterProvider<{fullTypeName}>
	{{
		/// <summary>
		/// Property names in ordinal order (alphabetical, base properties first).
		/// </summary>
		public static string[] PropertyNames {{ get; }} = new[] {{ {propertyNamesArray} }};

		/// <summary>
		/// Property types in ordinal order.
		/// </summary>
		public static Type[] PropertyTypes {{ get; }} = new[] {{ {propertyTypesArray} }};

		/// <summary>
		/// Converts this instance to an ordinal array for compact JSON serialization.
		/// </summary>
		public object?[] ToOrdinalArray()
		{{
			return new object?[] {{ {toArrayValues} }};
		}}

		/// <summary>
		/// Creates an instance from an ordinal array.
		/// </summary>
		public static object FromOrdinalArray(object?[] values)
		{{
			{fromArrayCode}
		}}

		/// <summary>
		/// Creates an AOT-compatible ordinal converter for this type.
		/// </summary>
		public static global::System.Text.Json.Serialization.JsonConverter<{fullTypeName}> CreateOrdinalConverter()
			=> new {typeInfo.Name}OrdinalConverter();
	}}
}}";

			source = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
			context.AddSource($"{typeInfo.SafeHintName}.Ordinal.g.cs", source);
		}
		catch (Exception ex)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				new DiagnosticDescriptor("NT0005", "Error", ex.Message, "FactoryGenerator.GenerateOrdinalSerialization", DiagnosticSeverity.Error, true),
				Location.None));
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