using System;

namespace CombinationTestGenerator.Models;

/// <summary>
/// Represents a return type configuration for factory methods.
/// </summary>
public sealed class ReturnTypeInfo : IEquatable<ReturnTypeInfo>
{
    public string CSharpType { get; set; } = string.Empty;
    public string? TaskVariant { get; set; }
    public bool IsAsync { get; set; }
    public bool IsBool { get; set; }
    public bool ReturnsTarget { get; set; }

    public bool Equals(ReturnTypeInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return CSharpType == other.CSharpType &&
               TaskVariant == other.TaskVariant &&
               IsAsync == other.IsAsync &&
               IsBool == other.IsBool &&
               ReturnsTarget == other.ReturnsTarget;
    }

    public override bool Equals(object? obj) => Equals(obj as ReturnTypeInfo);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (CSharpType?.GetHashCode() ?? 0);
            hash = hash * 31 + (TaskVariant?.GetHashCode() ?? 0);
            hash = hash * 31 + IsAsync.GetHashCode();
            hash = hash * 31 + IsBool.GetHashCode();
            hash = hash * 31 + ReturnsTarget.GetHashCode();
            return hash;
        }
    }
}
