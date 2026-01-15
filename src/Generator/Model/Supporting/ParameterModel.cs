// src/Generator/Model/Supporting/ParameterModel.cs
namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a method parameter for code generation.
/// </summary>
internal sealed record ParameterModel
{
    public ParameterModel(
        string name,
        string type,
        bool isService = false,
        bool isTarget = false,
        bool isCancellationToken = false,
        bool isParams = false)
    {
        Name = name;
        Type = type;
        IsService = isService;
        IsTarget = isTarget;
        IsCancellationToken = isCancellationToken;
        IsParams = isParams;
    }

    public string Name { get; }
    public string Type { get; }
    public bool IsService { get; }
    public bool IsTarget { get; }
    public bool IsCancellationToken { get; }
    public bool IsParams { get; }
}
