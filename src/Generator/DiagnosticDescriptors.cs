using Microsoft.CodeAnalysis;

namespace Neatoo;

/// <summary>
/// Contains all diagnostic descriptors for the RemoteFactory source generator.
/// </summary>
internal static class DiagnosticDescriptors
{
    // Category constants
    private const string CategoryUsage = "RemoteFactory.Usage";

    /// <summary>
    /// NF0101: Static class must be declared as partial to generate Execute delegates.
    /// </summary>
    public static readonly DiagnosticDescriptor ClassMustBePartial = new(
        id: "NF0101",
        title: "Class must be partial for factory generation",
        messageFormat: "Static class '{0}' must be declared as partial to generate Execute delegates",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When using static Execute operations, the containing class must be partial so the generator can add delegate definitions.");

    /// <summary>
    /// NF0102: Execute method must return Task or Task&lt;T&gt;.
    /// </summary>
    public static readonly DiagnosticDescriptor ExecuteMustReturnTask = new(
        id: "NF0102",
        title: "Execute method must return Task",
        messageFormat: "Execute method '{0}' must return Task or Task<T>, not '{1}'",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Execute operations are designed for remote execution and must be asynchronous. Change the return type to Task or Task<TResult>.");

    /// <summary>
    /// NF0103: Execute method must be in a static class.
    /// </summary>
    public static readonly DiagnosticDescriptor ExecuteRequiresStaticClass = new(
        id: "NF0103",
        title: "Execute method requires static class",
        messageFormat: "Execute method '{0}' must be in a static class. Either make the containing class static, or use a non-Execute factory operation.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Static Execute operations require the containing class to be static. The Execute pattern is designed for stateless remote procedure calls.");

    /// <summary>
    /// NF0104: Hint name truncated due to length limit.
    /// </summary>
    public static readonly DiagnosticDescriptor HintNameTruncated = new(
        id: "NF0104",
        title: "Hint name truncated - potential collision risk",
        messageFormat: "Type '{0}' has a fully qualified name that exceeds the {1} character limit. Name was truncated from '{2}' to '{3}'. This may cause collisions with other types. Consider using [assembly: FactoryHintNameLength({4})] to increase the limit, or shorten your namespace/type names.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Generated file hint names must be kept short to avoid file system path limits. When truncation occurs, types in different namespaces with the same class name may collide, causing build failures. Use the FactoryHintNameLengthAttribute to increase the limit if your file system supports longer paths.");

    // Category constant for configuration warnings
    private const string CategoryConfiguration = "RemoteFactory.Configuration";

    // ============================================================================
    // Warning Diagnostics (NF0200 range)
    // ============================================================================

    /// <summary>
    /// NF0201: Factory method returning target type must be static.
    /// </summary>
    public static readonly DiagnosticDescriptor FactoryMethodMustBeStatic = new(
        id: "NF0201",
        title: "Factory method returning target type must be static",
        messageFormat: "Factory method '{0}' returns target type '{1}' but is not static. Method will be skipped.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Factory methods that return the target type must be static. Make the method static or change the return type.");

    /// <summary>
    /// NF0202: Authorization method has invalid return type.
    /// </summary>
    public static readonly DiagnosticDescriptor AuthMethodWrongReturnType = new(
        id: "NF0202",
        title: "Authorization method has invalid return type",
        messageFormat: "Authorization method '{0}' must return bool, string, Task<bool>, or Task<string> - found '{1}'. Method will be skipped.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Authorization methods must return bool, string, string?, Task<bool>, Task<string>, or Task<string?>. Change the return type to indicate authorization status.");

    /// <summary>
    /// NF0203: Ambiguous save operations.
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousSaveOperations = new(
        id: "NF0203",
        title: "Ambiguous save operations",
        messageFormat: "Multiple {0} methods found with matching parameters: {1}. Only the first will be used in Save.",
        category: CategoryConfiguration,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Multiple Insert, Update, or Delete methods with the same parameter signature were found. Rename methods or use different parameter signatures to disambiguate.");

    /// <summary>
    /// NF0204: Write operation should not return target type.
    /// </summary>
    public static readonly DiagnosticDescriptor WriteReturnsTargetType = new(
        id: "NF0204",
        title: "Write operation should not return target type",
        messageFormat: "Method '{0}' has [{1}] attribute but returns target type '{2}'. Only [Fetch] and [Create] can return target type. Method will be skipped.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Insert, Update, Delete, and Execute operations should not return the target type. Only Fetch and Create operations can return the target type.");

    /// <summary>
    /// NF0205: [Create] on type requires record with primary constructor.
    /// </summary>
    public static readonly DiagnosticDescriptor CreateOnTypeRequiresRecordWithPrimaryConstructor = new(
        id: "NF0205",
        title: "[Create] on type requires record with primary constructor",
        messageFormat: "[Create] attribute on type '{0}' requires a record with a primary constructor. Either add a primary constructor to the record, use [Create] on an explicit constructor, or remove [Create] from the type declaration.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When [Create] is placed on a type declaration rather than a constructor, the type must be a record with a primary constructor (e.g., 'record Address(string Street, string City)'). This allows the generator to use the primary constructor for factory creation.");

    /// <summary>
    /// NF0206: record struct not supported.
    /// </summary>
    public static readonly DiagnosticDescriptor RecordStructNotSupported = new(
        id: "NF0206",
        title: "record struct not supported",
        messageFormat: "Type '{0}' is a record struct, which is not supported by RemoteFactory. Value types cannot be used with RemoteFactory due to serialization and reference tracking limitations. Use 'record class' or 'record' instead.",
        category: CategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "RemoteFactory does not support record struct (value types) because: 1) Value types are copied on assignment, breaking identity tracking. 2) Interface boxing loses type fidelity. 3) JSON $ref reference preservation doesn't work with value types. 4) Factory operations expect nullable return types. Use 'record class' or 'record' instead.");

    // ============================================================================
    // Info Diagnostics (NF0300 range) - Opt-in for debugging
    // ============================================================================

    /// <summary>
    /// NF0301: Method has no factory operation attribute.
    /// This is an opt-in diagnostic for debugging why methods don't generate.
    /// Enable via .editorconfig: dotnet_diagnostic.NF0301.severity = suggestion
    /// </summary>
    public static readonly DiagnosticDescriptor MethodSkippedNoAttribute = new(
        id: "NF0301",
        title: "Method has no factory operation attribute",
        messageFormat: "Method '{0}' in factory class '{1}' has no factory operation attribute and will not be generated",
        category: CategoryConfiguration,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: false,  // Opt-in only
        description: "This diagnostic helps debug why a method is not included in factory generation. Enable it via .editorconfig when troubleshooting.");
}
