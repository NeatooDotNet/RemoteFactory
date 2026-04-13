using System;
using System.Collections.Generic;
using System.Linq;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Authorization configuration for a factory method.
/// </summary>
internal sealed record AuthorizationModel
{
    public AuthorizationModel(
        IReadOnlyList<AuthMethodCall>? authMethods = null,
        IReadOnlyList<AspAuthorizeCall>? aspAuthorize = null,
        bool aspForbid = false)
    {
        AuthMethods = authMethods ?? System.Array.Empty<AuthMethodCall>();
        AspAuthorize = aspAuthorize ?? System.Array.Empty<AspAuthorizeCall>();
        AspForbid = aspForbid;
    }

    public IReadOnlyList<AuthMethodCall> AuthMethods { get; }
    public IReadOnlyList<AspAuthorizeCall> AspAuthorize { get; }
    public bool AspForbid { get; }
    public bool HasAuth => AuthMethods.Count > 0 || AspAuthorize.Count > 0;
}

/// <summary>
/// Represents a call to a custom authorization method.
/// </summary>
internal sealed record AuthMethodCall
{
    public AuthMethodCall(
        string className,
        string methodName,
        bool isTask = false,
        bool isRemote = false,
        bool isInternal = false,
        IReadOnlyList<ParameterModel>? parameters = null,
        string? concreteClassName = null)
    {
        ClassName = className;
        MethodName = methodName;
        IsTask = isTask;
        IsRemote = isRemote;
        IsInternal = isInternal;
        Parameters = parameters ?? System.Array.Empty<ParameterModel>();
        ConcreteClassName = concreteClassName;
    }

    public string ClassName { get; }
    public string MethodName { get; }
    public bool IsTask { get; }
    public bool IsRemote { get; }

    /// <summary>
    /// Whether this auth method is internal (non-public). Used to derive
    /// Can* method guard behavior from auth method accessibility.
    /// </summary>
    public bool IsInternal { get; }
    public IReadOnlyList<ParameterModel> Parameters { get; }

    /// <summary>
    /// When ClassName is an interface, this holds the concrete implementing class name.
    /// Null when ClassName is already a concrete class or no implementation was found.
    /// </summary>
    public string? ConcreteClassName { get; }

    public bool Equals(AuthMethodCall? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ClassName == other.ClassName
            && MethodName == other.MethodName
            && IsTask == other.IsTask
            && IsRemote == other.IsRemote
            && IsInternal == other.IsInternal
            && ConcreteClassName == other.ConcreteClassName
            && Parameters.SequenceEqual(other.Parameters);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(ClassName);
        hash.Add(MethodName);
        hash.Add(IsTask);
        hash.Add(IsRemote);
        hash.Add(IsInternal);
        hash.Add(ConcreteClassName);
        foreach (var p in Parameters)
            hash.Add(p);
        return hash.ToHashCode();
    }
}

/// <summary>
/// Represents an ASP.NET Core [Authorize] attribute configuration.
/// </summary>
internal sealed record AspAuthorizeCall
{
    public AspAuthorizeCall(
        IReadOnlyList<string>? constructorArgs = null,
        IReadOnlyList<string>? namedArgs = null)
    {
        ConstructorArgs = constructorArgs ?? System.Array.Empty<string>();
        NamedArgs = namedArgs ?? System.Array.Empty<string>();
    }

    public IReadOnlyList<string> ConstructorArgs { get; }
    public IReadOnlyList<string> NamedArgs { get; }

    public bool Equals(AspAuthorizeCall? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ConstructorArgs.SequenceEqual(other.ConstructorArgs)
            && NamedArgs.SequenceEqual(other.NamedArgs);
    }

    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (var arg in ConstructorArgs)
            hash.Add(arg);
        foreach (var arg in NamedArgs)
            hash.Add(arg);
        return hash.ToHashCode();
    }
}
