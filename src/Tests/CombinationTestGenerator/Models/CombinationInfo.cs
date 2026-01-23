using System;
using System.Collections.Generic;

namespace CombinationTestGenerator.Models;

/// <summary>
/// Represents a single valid combination of operation, return type, parameters, etc.
/// This is the core unit that gets generated into a test target class.
/// </summary>
public sealed class CombinationInfo : IEquatable<CombinationInfo>
{
    public string Operation { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = string.Empty;
    public string SignatureType { get; set; } = string.Empty;

    /// <summary>
    /// Gets the generated class name in format: Comb_{Operation}_{SignatureType}_{ReturnType}_{Parameters}_{ExecutionMode}
    /// </summary>
    public string ClassName => $"Comb_{Operation}_{SignatureType}_{ReturnType}_{Parameters}_{ExecutionMode}";

    /// <summary>
    /// Gets the factory interface name.
    /// </summary>
    public string FactoryInterfaceName => $"I{ClassName}Factory";

    public bool Equals(CombinationInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Operation == other.Operation &&
               ReturnType == other.ReturnType &&
               Parameters == other.Parameters &&
               ExecutionMode == other.ExecutionMode &&
               SignatureType == other.SignatureType;
    }

    public override bool Equals(object? obj) => Equals(obj as CombinationInfo);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Operation?.GetHashCode() ?? 0);
            hash = hash * 31 + (ReturnType?.GetHashCode() ?? 0);
            hash = hash * 31 + (Parameters?.GetHashCode() ?? 0);
            hash = hash * 31 + (ExecutionMode?.GetHashCode() ?? 0);
            hash = hash * 31 + (SignatureType?.GetHashCode() ?? 0);
            return hash;
        }
    }

    public override string ToString() => ClassName;
}

/// <summary>
/// Represents an invalid combination that should emit a diagnostic.
/// </summary>
public sealed class InvalidCombinationInfo : IEquatable<InvalidCombinationInfo>
{
    public string? Operation { get; set; }
    public string? ReturnType { get; set; }
    public string? Constraint { get; set; }
    public string Diagnostic { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    public bool Equals(InvalidCombinationInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Operation == other.Operation &&
               ReturnType == other.ReturnType &&
               Constraint == other.Constraint &&
               Diagnostic == other.Diagnostic &&
               Reason == other.Reason;
    }

    public override bool Equals(object? obj) => Equals(obj as InvalidCombinationInfo);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Operation?.GetHashCode() ?? 0);
            hash = hash * 31 + (ReturnType?.GetHashCode() ?? 0);
            hash = hash * 31 + (Constraint?.GetHashCode() ?? 0);
            hash = hash * 31 + (Diagnostic?.GetHashCode() ?? 0);
            hash = hash * 31 + (Reason?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
