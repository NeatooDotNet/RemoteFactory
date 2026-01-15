using System.Collections.Generic;

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
        IReadOnlyList<ParameterModel>? parameters = null)
    {
        ClassName = className;
        MethodName = methodName;
        IsTask = isTask;
        IsRemote = isRemote;
        Parameters = parameters ?? System.Array.Empty<ParameterModel>();
    }

    public string ClassName { get; }
    public string MethodName { get; }
    public bool IsTask { get; }
    public bool IsRemote { get; }
    public IReadOnlyList<ParameterModel> Parameters { get; }
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
}
