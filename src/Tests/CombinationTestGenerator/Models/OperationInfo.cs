using System;
using System.Collections.Generic;

namespace CombinationTestGenerator.Models;

/// <summary>
/// Represents a factory operation type (Create, Fetch, Insert, Update, Delete, Execute, Event).
/// </summary>
public sealed class OperationInfo : IEquatable<OperationInfo>
{
    public string Name { get; set; } = string.Empty;
    public string Attribute { get; set; } = string.Empty;
    public List<string> ValidReturnTypes { get; set; } = new();
    public List<string> ValidParameters { get; set; } = new();
    public List<string> ValidExecutionModes { get; set; } = new();
    public List<string> SignatureTypes { get; set; } = new();
    public List<string> Constraints { get; set; } = new();
    public bool GenerateTestTargets { get; set; } = true;

    public bool Equals(OperationInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name &&
               Attribute == other.Attribute &&
               ListEquals(ValidReturnTypes, other.ValidReturnTypes) &&
               ListEquals(ValidParameters, other.ValidParameters) &&
               ListEquals(ValidExecutionModes, other.ValidExecutionModes) &&
               ListEquals(SignatureTypes, other.SignatureTypes) &&
               ListEquals(Constraints, other.Constraints) &&
               GenerateTestTargets == other.GenerateTestTargets;
    }

    public override bool Equals(object? obj) => Equals(obj as OperationInfo);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (Name?.GetHashCode() ?? 0);
            hash = hash * 31 + (Attribute?.GetHashCode() ?? 0);
            hash = hash * 31 + GetListHashCode(ValidReturnTypes);
            hash = hash * 31 + GetListHashCode(ValidParameters);
            hash = hash * 31 + GetListHashCode(ValidExecutionModes);
            hash = hash * 31 + GetListHashCode(SignatureTypes);
            hash = hash * 31 + GetListHashCode(Constraints);
            hash = hash * 31 + GenerateTestTargets.GetHashCode();
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
