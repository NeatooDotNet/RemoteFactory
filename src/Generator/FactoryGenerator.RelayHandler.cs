using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neatoo.RemoteFactory.FactoryGenerator;
using Neatoo.RemoteFactory.Generator;
using Neatoo.RemoteFactory.Generator.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Neatoo;

public partial class Factory
{
    /// <summary>
    /// Reads the <c>DispatchPhase</c> argument off a <c>[FactoryEventHandler&lt;T&gt;]</c>
    /// attribute, as the member's name plus its numeric value. No argument means the
    /// attribute's parameterless constructor, which is <c>Immediate</c>.
    /// </summary>
    /// <remarks>
    /// Two deliberate choices here. The member name is looked up on the enum symbol instead of
    /// being mapped from a hardcoded table, so a phase added to the runtime enum flows through
    /// with no generator change. And only primitives escape this method — the
    /// <c>TypedConstant</c> and the <c>IFieldSymbol</c> stay local, because either one stored
    /// on the transform output would root a <c>Compilation</c> in the incremental cache.
    /// <para>
    /// An empty name means the declared value matches no member — an explicit cast to an
    /// undefined value. Rendered faithfully as a cast rather than diagnosed or silently
    /// coerced; such a handler registers and never drains, since the scheduler sweeps only
    /// defined phases.
    /// </para>
    /// </remarks>
    private static (string Name, int Value) ReadDispatchPhase(AttributeData attr)
    {
        const string ImmediateName = "Immediate";
        const int ImmediateValue = 0;

        if (attr.ConstructorArguments.Length == 0)
            return (ImmediateName, ImmediateValue);

        // Pattern match rather than Convert.ToInt32, matching the idiom already used for
        // attribute constructor arguments in FactoryGenerator.cs. Convert throws on a
        // non-IConvertible, a non-numeric string, or an out-of-range value, and a throw inside
        // a transform surfaces as CS8785 "generator failed to generate source" — taking every
        // other generated file in the compilation down with it, not just this one. No shape
        // that compiles is known to reach those, but the crash class is not worth carrying for
        // a construct that costs nothing to write safely.
        var arg = attr.ConstructorArguments[0];
        if (arg.Kind == TypedConstantKind.Error || arg.Value is not int value)
            return (ImmediateName, ImmediateValue);

        var member = (arg.Type as INamedTypeSymbol)?
            .GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.HasConstantValue && f.ConstantValue is int fieldValue && fieldValue == value);

        return (member?.Name ?? "", value);
    }

    /// <summary>
    /// Reads the <c>Coalesce</c> named argument off a <c>[FactoryEventHandler&lt;T&gt;]</c>
    /// attribute. Absent means <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Named arguments only — <c>Coalesce</c> is a property, never a constructor parameter,
    /// deliberately: <c>ReadDispatchPhase</c> treats any non-<c>int</c> first constructor
    /// argument as <c>Immediate</c>, so a constructor overload starting with a <c>bool</c>
    /// would silently register at the wrong phase with no diagnostic. Same primitives-only
    /// rule as the phase: the <c>TypedConstant</c> stays local.
    /// </remarks>
    private static bool ReadCoalesce(AttributeData attr)
    {
        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "Coalesce" && named.Value.Kind != TypedConstantKind.Error && named.Value.Value is bool coalesce)
                return coalesce;
        }

        return false;
    }

    /// <summary>
    /// True for a matching <c>[FactoryEventHandler&lt;T&gt;]</c>, by metadata shape rather
    /// than by type reference — the attribute cannot be referenced from this
    /// netstandard2.0 project without duplicating a public runtime type.
    /// </summary>
    private static bool IsFactoryEventHandlerAttribute(AttributeData attr)
    {
        if (attr.AttributeClass == null || !attr.AttributeClass.IsGenericType)
            return false;

        var originalDef = attr.AttributeClass.OriginalDefinition;
        return originalDef.Name == "FactoryEventHandlerAttribute"
            && originalDef.TypeParameters.Length == 1
            && originalDef.ContainingNamespace?.ToDisplayString() == "Neatoo.RemoteFactory";
    }

    /// <summary>
    /// True when <paramref name="classDecl"/> is the declaration chosen to represent
    /// <paramref name="symbol"/> — the first ATTRIBUTED one by file path, then by position.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ranges over the attributed declarations, never over
    /// <c>symbol.DeclaringSyntaxReferences</c>. That distinction is the whole correctness
    /// of this method: <c>ForAttributeWithMetadataName</c> only ever hands the transform an
    /// ATTRIBUTED node, so a canonical choice drawn from all declarations can land on one
    /// the pipeline never yields — and then nothing matches it, nothing is emitted, and no
    /// diagnostic says so. A partial class carrying the attribute on its second declaration
    /// is the ordinary shape that hits it.
    /// </para>
    /// <para>
    /// That failure would be strictly worse than the CS8785 this guard exists to prevent:
    /// CS8785 is loud and immediate, whereas a silently dropped registration surfaces only
    /// as a handler that never runs. Caught by the PHASE-008 test-review gate as a
    /// code-read finding and confirmed red before this fix.
    /// </para>
    /// <para>
    /// Compares by file path and span rather than by node reference: node identity holds
    /// within one compilation but is not something to lean on across the incremental
    /// pipeline's re-parses, and the ordinal comparison is what makes the choice stable
    /// between runs.
    /// </para>
    /// <para>
    /// <b>Two claims here are unpinned, and are recorded as such rather than left implicit.</b>
    /// The suite's split-partial fixtures live in one syntax tree, so only the span leg of the
    /// ordering runs — the file-path leg, which is the ordinary shape for real partials, is
    /// untested. And the stability claim above is not observable from
    /// <c>IncrementalCacheTests</c>, whose fixture has no split partial. Both noted at the
    /// PHASE-008 gate.
    /// </para>
    /// <para>
    /// One consequence of choosing a single declaration IS worth stating, because it is not
    /// obvious: <c>usings</c>, <c>classSignatureText</c>, and the namespace are all read from
    /// the canonical declaration's syntax tree, so a partial in another file contributes none
    /// of its <c>using</c> directives to the emitted output. That is benign as of PHASE-008 —
    /// the renderer injects the three usings the generated body needs, and every
    /// consumer-derived token it emits is now <c>global::</c>-qualified, so the emitted code
    /// depends on no consumer <c>using</c> at all. It would stop being benign if an
    /// unqualified consumer-derived token were ever reintroduced.
    /// </para>
    /// </remarks>
    private static bool IsCanonicalDeclaration(INamedTypeSymbol symbol, ClassDeclarationSyntax classDecl)
    {
        if (symbol.DeclaringSyntaxReferences.Length <= 1)
            return true;

        string? canonicalPath = null;
        var canonicalStart = 0;

        foreach (var attr in symbol.GetAttributes())
        {
            if (!IsFactoryEventHandlerAttribute(attr))
                continue;

            var declaration = attr.ApplicationSyntaxReference?.GetSyntax()
                .Ancestors()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            if (declaration == null)
                continue;

            var path = declaration.SyntaxTree.FilePath;
            var start = declaration.Span.Start;

            if (canonicalPath == null)
            {
                canonicalPath = path;
                canonicalStart = start;
                continue;
            }

            var pathOrder = System.StringComparer.Ordinal.Compare(path, canonicalPath);
            if (pathOrder < 0 || (pathOrder == 0 && start < canonicalStart))
            {
                canonicalPath = path;
                canonicalStart = start;
            }
        }

        // No attributed declaration resolved to a syntax node — nothing to deduplicate
        // against, so let this one through rather than dropping the class silently.
        if (canonicalPath == null)
            return true;

        return canonicalPath == classDecl.SyntaxTree.FilePath
            && canonicalStart == classDecl.Span.Start;
    }

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

        // One model per SYMBOL, not per attributed declaration.
        //
        // ForAttributeWithMetadataName yields a value per attributed syntax NODE, while this
        // transform reads symbol.GetAttributes() — every attribute on every partial — and the
        // hint name is derived from the symbol. So a class whose attributes are split across
        // two partial declarations produced two identical models with one hint name, and the
        // second AddSource threw ArgumentException. That surfaces as CS8785, which does not
        // fail just this class: the generator "will not contribute to the output," so EVERY
        // factory in the assembly silently disappears and the consumer sees a cascade of
        // missing-type errors pointing nowhere near the cause.
        //
        // PHASE-002 inferred this collision and left it unmeasured; PHASE-008 measured it and
        // it was real. Skipping the non-canonical declarations also stops the diagnostics on
        // such a class being reported once per partial, since the surviving model already
        // carries all of them.
        //
        // Ordered rather than taking DeclaringSyntaxReferences[0] as given: the emitted output
        // is identical from any declaration (the model reads the symbol), but the incremental
        // cache compares transform outputs, so the CHOICE has to be stable across runs or the
        // pipeline churns. File path then span start is a total order over partials.
        if (!IsCanonicalDeclaration(symbol, classDecl))
            return null;

        var diagnostics = new List<DiagnosticInfo>();
        var entries = new List<EventHandlerEntry>();

        var classLocation = classDecl.Identifier.GetLocation();
        var classLineSpan = classLocation.GetLineSpan();

        // Event types that already produced an entry, mapped to the phase that entry
        // registered at. Stacking [FactoryEventHandler<T>] is for several event TYPES; a
        // repeat of the same type resolves to the same handler method (the scan below is a
        // pure function of the event type, not of which attribute is being processed), so a
        // duplicate could only re-register what the first declaration already registered —
        // and FactoryEventHandlerRegistry keeps the first. NF0504 reports it and the entry is
        // skipped, so the emitted code matches what the diagnostic says.
        //
        // Populated only on SUCCESS, deliberately: when the first declaration failed with
        // NF0501/NF0502 nothing is registered, so there is no surviving phase to name and no
        // duplicate to report. The second declaration then repeats the scan and repeats that
        // diagnostic, exactly as it did before NF0504 existed.
        //
        // One count DOES change, in a shape no test covers: a class with a duplicate attribute
        // AND a matching instance method now emits NF0503 once instead of twice, because the
        // duplicate short-circuits before the instance-method scan. Reporting one migration
        // warning per class instead of one per redundant attribute is the better outcome, so
        // this is recorded rather than worked around.
        var registeredPhaseByEventType = new Dictionary<string, string>();

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

            if (registeredPhaseByEventType.TryGetValue(eventTypeName, out var survivingPhase))
            {
                diagnostics.Add(new DiagnosticInfo(
                    "NF0504",
                    classLineSpan.Path ?? "",
                    classLineSpan.StartLinePosition.Line,
                    classLineSpan.StartLinePosition.Character,
                    classLineSpan.EndLinePosition.Line,
                    classLineSpan.EndLinePosition.Character,
                    classLocation.SourceSpan.Start,
                    classLocation.SourceSpan.Length,
                    symbol.Name,
                    eventTypeName,
                    survivingPhase));
                continue;
            }

            // Find matching STATIC methods. Instance-method handlers are the former
            // client-side relay pattern (now replaced by IFactoryEventRelay) — they are
            // silently skipped at codegen time but emit NF0503 (Warning) so consumers
            // who didn't migrate get a loud signal at compile time.
            //
            // Method match: static, returns Task, first non-[Service]/non-CT parameter is of event type T.
            var matchingMethods = new List<IMethodSymbol>();
            // Track instance methods that match the OLD client-relay shape so we can warn (NF0503).
            var ignoredInstanceMethods = new List<IMethodSymbol>();
            foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Ordinary)
                    continue;

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

                if (paramTypeName != eventTypeName)
                    continue;

                if (member.IsStatic)
                {
                    matchingMethods.Add(member);
                }
                else
                {
                    ignoredInstanceMethods.Add(member);
                }
            }

            // NF0503: warn for each instance method that matches the old client-relay shape.
            foreach (var ignored in ignoredInstanceMethods)
            {
                var methodLocation = ignored.Locations.FirstOrDefault() ?? classLocation;
                var methodLineSpan = methodLocation.GetLineSpan();
                diagnostics.Add(new DiagnosticInfo(
                    "NF0503",
                    methodLineSpan.Path ?? "",
                    methodLineSpan.StartLinePosition.Line,
                    methodLineSpan.StartLinePosition.Character,
                    methodLineSpan.EndLinePosition.Line,
                    methodLineSpan.EndLinePosition.Character,
                    methodLocation.SourceSpan.Start,
                    methodLocation.SourceSpan.Length,
                    symbol.Name,
                    ignored.Name,
                    eventTypeName));
            }

            if (matchingMethods.Count == 0)
            {
                // Only diagnose when the class declares any static-looking handler method
                // but none matched the required shape. If no static candidates exist at all
                // (e.g. instance-only handler class), stay silent per Rule 16.
                var hasStaticCandidate = symbol.GetMembers().OfType<IMethodSymbol>().Any(m =>
                    m.MethodKind == MethodKind.Ordinary
                    && m.IsStatic);
                if (hasStaticCandidate)
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
                }
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

                // The type token is FINAL here — the renderer emits it verbatim. Two cases:
                //
                // Resolved: global::-qualified, so the generated file binds to the consumer's
                // real type even when their namespace shadows every unqualified route to it
                // (PHASE-011; pinned by the shadowing fixtures).
                //
                // Error type: a name the semantic model cannot resolve at generation time. In
                // practice that is a factory interface THIS run is about to generate — the
                // consumer's handler takes [Service] IPlanFactory, and IPlanFactory does not
                // exist in the input compilation. The symbol carries only the bare identifier,
                // so qualifying it puts it in the global namespace: CS0400 in the consumer's
                // build (v1.8.0 regression, reported against zTreatment). Emit it exactly as
                // the consumer wrote it — qualifier and all, which the symbol has already
                // lost — and let the usings copied into the generated file resolve it once
                // the interface exists. That is how every other renderer's [Service]
                // parameters have always been emitted; the class-factory path captures from
                // syntax and never asks the symbol, which is why it never regressed.
                string pType;
                if (p.Type.TypeKind == TypeKind.Error)
                {
                    pType = p.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax paramSyntax
                        && paramSyntax.Type is not null
                        ? paramSyntax.Type.ToString()
                        : p.Type.ToDisplayString();
                }
                else
                {
                    pType = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }

                var pm = new ParameterModel(p.Name, pType, isService, false, isCT, false);
                allParameters.Add(pm);
                if (isService)
                    serviceParameters.Add(pm);
                else
                    parameters.Add(pm);
            }

            bool isAsync = method.IsAsync ||
                (method.ReturnType.ToDisplayString() == "System.Threading.Tasks.Task" && method.IsAsync);

            var (phaseName, phaseValue) = ReadDispatchPhase(attr);
            var coalesce = ReadCoalesce(attr);

            // NF0505: Coalesce on an Immediate-declared registration. Immediate dispatches
            // are never queued, so there is nothing to coalesce — the registration is still
            // emitted faithfully (flag inert at runtime), the warning is the loudness.
            // Scoped to the DECLARED Immediate phase (no argument, or the explicit member):
            // an undefined-value cast has an empty name and never drains at all, which is
            // its own recorded hazard, not this diagnostic's claim. Runtime fall-throughs
            // (no scheduler / no entry call) are unreachable at compile time and are
            // documented instead.
            if (coalesce && phaseName == "Immediate")
            {
                var attrLocation = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? classLocation;
                var attrLineSpan = attrLocation.GetLineSpan();
                diagnostics.Add(new DiagnosticInfo(
                    "NF0505",
                    attrLineSpan.Path ?? "",
                    attrLineSpan.StartLinePosition.Line,
                    attrLineSpan.StartLinePosition.Character,
                    attrLineSpan.EndLinePosition.Line,
                    attrLineSpan.EndLinePosition.Character,
                    attrLocation.SourceSpan.Start,
                    attrLocation.SourceSpan.Length,
                    symbol.Name,
                    eventTypeName));
            }

            entries.Add(new EventHandlerEntry(
                eventTypeName: eventTypeName,
                methodName: method.Name,
                isStatic: method.IsStatic,
                isAsync: method.IsAsync,
                parameters: parameters,
                serviceParameters: serviceParameters,
                allParameters: allParameters,
                phaseName: phaseName,
                phaseValue: phaseValue,
                coalesce: coalesce));

            // The survivor string NF0504 names covers the whole registration — phase and,
            // when set, the coalesce flag — so the diagnostic stays complete as the
            // registration grows settings (PHASE-006 plan review A-V1).
            var registeredPhaseToken =
                phaseName.Length > 0 ? phaseName : phaseValue.ToString(CultureInfo.InvariantCulture);
            registeredPhaseByEventType[eventTypeName] = coalesce
                ? $"DispatchPhase.{registeredPhaseToken}, Coalesce = true"
                : $"DispatchPhase.{registeredPhaseToken}";
        }

        if (entries.Count == 0 && diagnostics.Count == 0)
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

        return new RelayHandlerModel(
            className: symbol.Name,
            classSignatureText: signatureText,
            @namespace: ns,
            usings: usings.Distinct().ToList(),
            hintName: hintName,
            entries: entries,
            diagnostics: diagnostics);
    }
}
