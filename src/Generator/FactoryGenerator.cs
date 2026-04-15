using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Neatoo.Factory;
using Neatoo.RemoteFactory.FactoryGenerator;
using Neatoo.RemoteFactory.Generator.Builder;
using Neatoo.RemoteFactory.Generator.Renderer;

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
			// Build model from TypeInfo
			var unit = FactoryModelBuilder.Build(typeInfo);

			// Report diagnostics (includes both transform-phase and build-phase diagnostics)
			foreach (var diag in unit.Diagnostics)
			{
				ReportDiagnostic(spc, diag);
			}

			// Render and add source
			var source = FactoryRenderer.Render(unit);
			spc.AddSource($"{unit.HintName}Factory.g.cs", source);

			// Render ordinal serialization if applicable
			var ordinalSource = FactoryRenderer.RenderOrdinalSerialization(unit);
			if (ordinalSource != null)
			{
				spc.AddSource($"{unit.HintName}.Ordinal.g.cs", ordinalSource);
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
			// Build model from TypeInfo
			var unit = FactoryModelBuilder.Build(typeInfo);

			// Report diagnostics (includes both transform-phase and build-phase diagnostics)
			foreach (var diag in unit.Diagnostics)
			{
				ReportDiagnostic(spc, diag);
			}

			// Render and add source
			var source = FactoryRenderer.Render(unit);
			spc.AddSource($"{unit.HintName}Factory.g.cs", source);
		});

		// Pipeline for [FactoryEventHandler<T>] classes — generates relay dispatch registrations
		var relayHandlersToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName(
			"Neatoo.RemoteFactory.FactoryEventHandlerAttribute`1",
			predicate: static (s, _) => s is ClassDeclarationSyntax,
			transform: static (ctx, _) =>
			{
				var classDecl = (ClassDeclarationSyntax)ctx.TargetNode;
				var semanticModel = ctx.SemanticModel;
				return TransformRelayHandler(classDecl, semanticModel);
			});

		context.RegisterSourceOutput(relayHandlersToGenerate, static (spc, model) =>
		{
			if (model == null)
				return;

			foreach (var diag in model.Diagnostics)
			{
				ReportDiagnostic(spc, diag);
			}

			if (model.Entries.Count == 0)
				return;

			var source = RelayHandlerRenderer.Render(model);
			spc.AddSource($"{model.HintName}.FactoryEventHandler.g.cs", source);
		});

	}

	private static DiagnosticDescriptor GetDescriptor(string diagnosticId)
	{
		return diagnosticId switch
		{
			"NF0101" => DiagnosticDescriptors.ClassMustBePartial,
			"NF0102" => DiagnosticDescriptors.ExecuteMustReturnTask,
			"NF0103" => DiagnosticDescriptors.ExecuteRequiresStaticMethod,
			"NF0104" => DiagnosticDescriptors.HintNameTruncated,
			"NF0105" => DiagnosticDescriptors.RemotePublicContradiction,
			"NF0201" => DiagnosticDescriptors.FactoryMethodMustBeStatic,
			"NF0202" => DiagnosticDescriptors.AuthMethodWrongReturnType,
			"NF0203" => DiagnosticDescriptors.AmbiguousSaveOperations,
			"NF0204" => DiagnosticDescriptors.WriteReturnsTargetType,
			"NF0205" => DiagnosticDescriptors.CreateOnTypeRequiresRecordWithPrimaryConstructor,
			"NF0206" => DiagnosticDescriptors.RecordStructNotSupported,
			"NF0207" => DiagnosticDescriptors.NestedTypeOrdinalSkipped,
			"NF0301" => DiagnosticDescriptors.MethodSkippedNoAttribute,
			"NF0405" => DiagnosticDescriptors.FactoryEventHandlerMustBeStatic,
			"NF0501" => DiagnosticDescriptors.RelayHandlerMethodNotFound,
			"NF0502" => DiagnosticDescriptors.RelayHandlerMethodAmbiguous,
			"NF0503" => DiagnosticDescriptors.RelayHandlerInstanceMethodIgnored,
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