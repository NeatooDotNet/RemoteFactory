using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a factory generated for a static class with [Execute] methods.
/// </summary>
internal sealed record StaticFactoryModel
{
    public StaticFactoryModel(
        string typeName,
        string signatureText,
        bool isPartial = false,
        IReadOnlyList<ExecuteDelegateModel>? delegates = null,
        IReadOnlyList<string>? dtoReturnTypes = null)
    {
        TypeName = typeName;
        SignatureText = signatureText;
        IsPartial = isPartial;
        Delegates = delegates ?? System.Array.Empty<ExecuteDelegateModel>();
        DtoReturnTypes = dtoReturnTypes ?? System.Array.Empty<string>();
    }

    public string TypeName { get; }
    public string SignatureText { get; }
    public bool IsPartial { get; }
    public IReadOnlyList<ExecuteDelegateModel> Delegates { get; }

    /// <summary>
    /// Plain DTO types that need constructor registration for IL trimming support.
    /// </summary>
    public IReadOnlyList<string> DtoReturnTypes { get; }
}
