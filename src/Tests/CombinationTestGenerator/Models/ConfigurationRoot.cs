using System;
using System.Collections.Generic;

namespace CombinationTestGenerator.Models;

/// <summary>
/// Root configuration object for CombinationDimensions.json deserialization.
/// </summary>
public sealed class ConfigurationRoot : IEquatable<ConfigurationRoot>
{
    public List<OperationInfo> Operations { get; set; } = new();
    public Dictionary<string, ReturnTypeInfo> ReturnTypes { get; set; } = new();
    public Dictionary<string, List<ParameterDefinition>> Parameters { get; set; } = new();
    public List<InvalidCombinationInfo> InvalidCombinations { get; set; } = new();
    public List<AuthorizationModeInfo> AuthorizationModes { get; set; } = new();

    public bool Equals(ConfigurationRoot? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        // For incremental generator caching, a basic reference check is sufficient
        // since the configuration is loaded once from embedded resource
        return false; // Force regeneration on any change
    }

    public override bool Equals(object? obj) => Equals(obj as ConfigurationRoot);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Operations.Count;
            hash = hash * 31 + ReturnTypes.Count;
            hash = hash * 31 + Parameters.Count;
            hash = hash * 31 + InvalidCombinations.Count;
            hash = hash * 31 + AuthorizationModes.Count;
            return hash;
        }
    }
}

/// <summary>
/// Represents an authorization mode configuration.
/// </summary>
public sealed class AuthorizationModeInfo : IEquatable<AuthorizationModeInfo>
{
    public string Name { get; set; } = string.Empty;
    public List<string> Attributes { get; set; } = new();
    public List<string> Methods { get; set; } = new();
    public string? Description { get; set; }

    public bool Equals(AuthorizationModeInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name &&
               Description == other.Description &&
               ListEquals(Attributes, other.Attributes) &&
               ListEquals(Methods, other.Methods);
    }

    public override bool Equals(object? obj) => Equals(obj as AuthorizationModeInfo);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Name?.GetHashCode() ?? 0);
            hash = hash * 31 + (Description?.GetHashCode() ?? 0);
            hash = hash * 31 + GetListHashCode(Attributes);
            hash = hash * 31 + GetListHashCode(Methods);
            return hash;
        }
    }

    private static bool ListEquals(List<string>? a, List<string>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private static int GetListHashCode(List<string>? list)
    {
        if (list is null) return 0;
        unchecked
        {
            var hash = 17;
            foreach (var item in list)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
