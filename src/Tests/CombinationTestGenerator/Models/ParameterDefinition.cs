using System;
using System.Collections.Generic;

namespace CombinationTestGenerator.Models;

/// <summary>
/// Represents a parameter configuration for factory methods.
/// Named ParameterDefinition to avoid conflict with System.Reflection.ParameterInfo.
/// </summary>
public sealed class ParameterDefinition : IEquatable<ParameterDefinition>
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Attribute { get; set; }
    public bool IsService { get; set; }

    public bool Equals(ParameterDefinition? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type &&
               Name == other.Name &&
               Attribute == other.Attribute &&
               IsService == other.IsService;
    }

    public override bool Equals(object? obj) => Equals(obj as ParameterDefinition);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Type?.GetHashCode() ?? 0);
            hash = hash * 31 + (Name?.GetHashCode() ?? 0);
            hash = hash * 31 + (Attribute?.GetHashCode() ?? 0);
            hash = hash * 31 + IsService.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Represents a parameter variation configuration.
/// </summary>
public sealed class ParameterVariation : IEquatable<ParameterVariation>
{
    public string Name { get; set; } = string.Empty;
    public List<ParameterDefinition> Parameters { get; set; } = new();

    public bool Equals(ParameterVariation? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Name != other.Name) return false;
        if (Parameters.Count != other.Parameters.Count) return false;
        for (var i = 0; i < Parameters.Count; i++)
        {
            if (!Parameters[i].Equals(other.Parameters[i])) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as ParameterVariation);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Name?.GetHashCode() ?? 0);
            foreach (var param in Parameters)
            {
                hash = hash * 31 + param.GetHashCode();
            }
            return hash;
        }
    }
}
