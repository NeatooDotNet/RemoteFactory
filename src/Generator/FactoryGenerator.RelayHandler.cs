using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.RemoteFactory.FactoryGenerator;
using Neatoo.RemoteFactory.Generator;
using Neatoo.RemoteFactory.Generator.Model;
using System.Collections.Generic;
using System.Linq;

namespace Neatoo;

public partial class Factory
{
    /// <summary>
    /// Transforms a class decorated with [FactoryEventHandler&lt;T&gt;] into a RelayHandlerModel.
    /// Extracts event types from the generic attribute and finds matching handler methods.
    /// A matching method: non-private, returns Task, first non-[Service]/non-CT parameter is T.
    /// Static methods → server-side handler. Instance methods → client-side relay handler.
    /// </summary>
    private static RelayHandlerModel? TransformRelayHandler(ClassDeclarationSyntax classDecl, SemanticModel semanticModel)
    {
        var symbol = semanticModel.GetDeclaredSymbol(classDecl);
        if (symbol == null)
            return null;

        var diagnostics = new List<DiagnosticInfo>();
        var entries = new List<EventHandlerEntry>();

        // Class-level dedupe for event-type trimming preservation. Shared visited set so a
        // nested type reachable from multiple event roots is emitted exactly once.
        var preservationVisited = new HashSet<string>();
        var eventDtoTypesList = new List<string>();
        var eventRecordTypesList = new List<string>();

        var classLocation = classDecl.Identifier.GetLocation();
        var classLineSpan = classLocation.GetLineSpan();

        // Check partial
        if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            diagnostics.Add(new DiagnosticInfo(
                "NF0101",
                classLineSpan.Path ?? "",
                classLineSpan.StartLinePosition.Line,
                classLineSpan.StartLinePosition.Character,
                classLineSpan.EndLinePosition.Line,
                classLineSpan.EndLinePosition.Character,
                classLocation.SourceSpan.Start,
                classLocation.SourceSpan.Length,
                symbol.Name));
        }

        // Find all [FactoryEventHandler<T>] attributes on the class
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass == null || !attr.AttributeClass.IsGenericType)
                continue;

            var originalDef = attr.AttributeClass.OriginalDefinition;
            if (originalDef.Name != "FactoryEventHandlerAttribute" || originalDef.TypeParameters.Length != 1)
                continue;

            if (originalDef.ContainingNamespace?.ToDisplayString() != "Neatoo.RemoteFactory")
                continue;

            var eventType = attr.AttributeClass.TypeArguments[0];
            var eventTypeName = eventType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (eventTypeName.StartsWith("global::"))
                eventTypeName = eventTypeName.Substring("global::".Length);

            // Gather trimming-preservation types BEFORE method-match — preservation must still
            // be emitted when the handler is diagnostic-broken (NF0501/NF0502) because the event
            // type crosses the serialization boundary regardless of whether this class has a
            // valid handler method.
            DtoTypeWalker.WalkEventRoot(eventType, eventDtoTypesList, eventRecordTypesList, preservationVisited);

            // Find matching methods: non-private, returns Task,
            // first non-[Service]/non-CT parameter is of event type T
            var matchingMethods = new List<IMethodSymbol>();
            foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Ordinary)
                    continue;
                // Allow all accessibility levels — the generator controls invocation

                // Must return Task
                var returnType = member.ReturnType.ToDisplayString();
                if (returnType != "System.Threading.Tasks.Task")
                    continue;

                // Find first non-[Service], non-CancellationToken parameter
                IParameterSymbol? eventParam = null;
                foreach (var p in member.Parameters)
                {
                    if (p.Type.ToDisplayString() == "System.Threading.CancellationToken")
                        continue;
                    if (p.GetAttributes().Any(a => a.AttributeClass?.Name == "ServiceAttribute"))
                        continue;
                    eventParam = p;
                    break;
                }

                if (eventParam == null)
                    continue;

                var paramTypeName = eventParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (paramTypeName.StartsWith("global::"))
                    paramTypeName = paramTypeName.Substring("global::".Length);

                if (paramTypeName == eventTypeName)
                    matchingMethods.Add(member);
            }

            if (matchingMethods.Count == 0)
            {
                diagnostics.Add(new DiagnosticInfo(
                    "NF0501",
                    classLineSpan.Path ?? "",
                    classLineSpan.StartLinePosition.Line,
                    classLineSpan.StartLinePosition.Character,
                    classLineSpan.EndLinePosition.Line,
                    classLineSpan.EndLinePosition.Character,
                    classLocation.SourceSpan.Start,
                    classLocation.SourceSpan.Length,
                    symbol.Name,
                    eventTypeName));
                continue;
            }

            if (matchingMethods.Count > 1)
            {
                diagnostics.Add(new DiagnosticInfo(
                    "NF0502",
                    classLineSpan.Path ?? "",
                    classLineSpan.StartLinePosition.Line,
                    classLineSpan.StartLinePosition.Character,
                    classLineSpan.EndLinePosition.Line,
                    classLineSpan.EndLinePosition.Character,
                    classLocation.SourceSpan.Start,
                    classLocation.SourceSpan.Length,
                    symbol.Name,
                    eventTypeName));
                continue;
            }

            var method = matchingMethods[0];

            // Build parameter lists. AllParameters preserves declaration order so the
            // renderer can emit invocation arguments in the order the user wrote them
            // (e.g. (evt, ct, [Service] svc) must bind correctly, not be reshuffled).
            var parameters = new List<ParameterModel>();
            var serviceParameters = new List<ParameterModel>();
            var allParameters = new List<ParameterModel>();
            foreach (var p in method.Parameters)
            {
                var isService = p.GetAttributes().Any(a => a.AttributeClass?.Name == "ServiceAttribute");
                var isCT = p.Type.ToDisplayString() == "System.Threading.CancellationToken";
                var pType = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (pType.StartsWith("global::"))
                    pType = pType.Substring("global::".Length);

                var pm = new ParameterModel(p.Name, pType, isService, false, isCT, false);
                allParameters.Add(pm);
                if (isService)
                    serviceParameters.Add(pm);
                else
                    parameters.Add(pm);
            }

            bool isAsync = method.IsAsync ||
                (method.ReturnType.ToDisplayString() == "System.Threading.Tasks.Task" && method.IsAsync);

            entries.Add(new EventHandlerEntry(
                eventTypeName: eventTypeName,
                methodName: method.Name,
                isStatic: method.IsStatic,
                isAsync: method.IsAsync,
                parameters: parameters,
                serviceParameters: serviceParameters,
                allParameters: allParameters));
        }

        if (entries.Count == 0 && diagnostics.Count == 0 && eventDtoTypesList.Count == 0 && eventRecordTypesList.Count == 0)
            return null;

        var ns = FindNamespace(classDecl) ?? "MissingNamespace";
        var signatureText = classDecl.ToFullString()
            .Substring(classDecl.Modifiers.FullSpan.Start - classDecl.FullSpan.Start,
                classDecl.Identifier.FullSpan.End - classDecl.Modifiers.FullSpan.Start)
            .Trim();

        var usings = new List<string>
        {
            "using Neatoo.RemoteFactory;",
            "using Neatoo.RemoteFactory.Internal;",
            "using Microsoft.Extensions.DependencyInjection;"
        };

        // Collect usings from the source file.
        // Use ToString() (not ToFullString()) to avoid capturing trivia like leading comments.
        // Normalize by trimming trailing semicolons/whitespace before comparing.
        var root = classDecl.SyntaxTree.GetRoot();
        foreach (var u in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var usingText = u.ToString().Trim();
            if (!usingText.EndsWith(";"))
                usingText += ";";
            usings.Add(usingText);
        }

        var hintName = $"{ns}.{symbol.Name}";
        if (MaxHintNameLength.HasValue && hintName.Length > MaxHintNameLength.Value)
        {
            hintName = hintName.Substring(0, MaxHintNameLength.Value);
        }

        // Preservation FQNs include the "global::" prefix (as produced by SymbolDisplayFormat.FullyQualifiedFormat)
        // to match the rendering used by ClassFactoryRenderer/StaticFactoryRenderer/InterfaceFactoryRenderer
        // for their own DtoConstructorRegistry.Register<> emissions.
        var eventDtoTypes = new EquatableArray<string>([.. eventDtoTypesList]);
        var eventRecordTypes = new EquatableArray<string>([.. eventRecordTypesList]);

        return new RelayHandlerModel(
            className: symbol.Name,
            classSignatureText: signatureText,
            @namespace: ns,
            usings: usings.Distinct().ToList(),
            hintName: hintName,
            entries: entries,
            diagnostics: diagnostics,
            eventDtoTypes: eventDtoTypes,
            eventRecordTypes: eventRecordTypes);
    }
}
