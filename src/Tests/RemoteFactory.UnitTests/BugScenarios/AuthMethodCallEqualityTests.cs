using Neatoo.RemoteFactory.Generator.Model;

namespace RemoteFactory.UnitTests.BugScenarios;

/// <summary>
/// Unit tests for AuthMethodCall and AspAuthorizeCall value equality.
/// These types are sealed records with IReadOnlyList properties; record-generated
/// equality uses reference equality for collections. Custom Equals/GetHashCode
/// overrides provide structural comparison.
/// </summary>
public class AuthMethodCallEqualityTests
{
    #region AuthMethodCall Equality

    [Fact]
    public void AuthMethodCall_Equals_IdenticalInstances()
    {
        var a = new AuthMethodCall("AuthClass", "CanWrite", isTask: false, isRemote: true, isInternal: true,
            parameters: new[] { new ParameterModel("id", "Guid") });
        var b = new AuthMethodCall("AuthClass", "CanWrite", isTask: false, isRemote: true, isInternal: true,
            parameters: new[] { new ParameterModel("id", "Guid") });

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void AuthMethodCall_NotEqual_DifferentMethodName()
    {
        var a = new AuthMethodCall("AuthClass", "CanWrite");
        var b = new AuthMethodCall("AuthClass", "CanRead");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AuthMethodCall_NotEqual_DifferentParameters()
    {
        var a = new AuthMethodCall("AuthClass", "CanWrite",
            parameters: new[] { new ParameterModel("id", "Guid") });
        var b = new AuthMethodCall("AuthClass", "CanWrite",
            parameters: new[] { new ParameterModel("name", "string") });

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AuthMethodCall_Equal_SeparateListInstances_SameContent()
    {
        // This is the actual bug scenario: Insert, Update, Delete each create
        // separate parameter list instances with the same content.
        var paramsA = new List<ParameterModel> { new("id", "Guid") };
        var paramsB = new List<ParameterModel> { new("id", "Guid") };

        Assert.False(ReferenceEquals(paramsA, paramsB)); // different instances

        var a = new AuthMethodCall("AuthClass", "CanWrite", parameters: paramsA);
        var b = new AuthMethodCall("AuthClass", "CanWrite", parameters: paramsB);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void AuthMethodCall_Distinct_DeduplicatesIdenticalInstances()
    {
        // Simulates BuildSaveMethodFromGroup: 3 identical AuthMethodCall instances
        // from Insert, Update, Delete — each with separate parameter list instances.
        var calls = new List<AuthMethodCall>
        {
            new("AuthClass", "CanWrite", parameters: new[] { new ParameterModel("id", "Guid") }),
            new("AuthClass", "CanWrite", parameters: new[] { new ParameterModel("id", "Guid") }),
            new("AuthClass", "CanWrite", parameters: new[] { new ParameterModel("id", "Guid") }),
        };

        var distinct = calls.Distinct().ToList();

        Assert.Single(distinct);
    }

    [Fact]
    public void AuthMethodCall_Equal_EmptyParameters()
    {
        var a = new AuthMethodCall("AuthClass", "CanWrite");
        var b = new AuthMethodCall("AuthClass", "CanWrite", parameters: null);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void AuthMethodCall_NotEqual_DifferentClassName()
    {
        var a = new AuthMethodCall("AuthClassA", "CanWrite");
        var b = new AuthMethodCall("AuthClassB", "CanWrite");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AuthMethodCall_NotEqual_DifferentIsRemote()
    {
        var a = new AuthMethodCall("AuthClass", "CanWrite", isRemote: true);
        var b = new AuthMethodCall("AuthClass", "CanWrite", isRemote: false);

        Assert.NotEqual(a, b);
    }

    #endregion

    #region AspAuthorizeCall Equality

    [Fact]
    public void AspAuthorizeCall_Equal_IdenticalInstances()
    {
        var a = new AspAuthorizeCall(
            constructorArgs: new[] { "\"Policy1\"" },
            namedArgs: new[] { "Roles = \"Admin\"" });
        var b = new AspAuthorizeCall(
            constructorArgs: new[] { "\"Policy1\"" },
            namedArgs: new[] { "Roles = \"Admin\"" });

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void AspAuthorizeCall_NotEqual_DifferentConstructorArgs()
    {
        var a = new AspAuthorizeCall(constructorArgs: new[] { "\"Policy1\"" });
        var b = new AspAuthorizeCall(constructorArgs: new[] { "\"Policy2\"" });

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AspAuthorizeCall_NotEqual_DifferentNamedArgs()
    {
        var a = new AspAuthorizeCall(namedArgs: new[] { "Roles = \"Admin\"" });
        var b = new AspAuthorizeCall(namedArgs: new[] { "Roles = \"User\"" });

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AspAuthorizeCall_Equal_EmptyArgs()
    {
        var a = new AspAuthorizeCall();
        var b = new AspAuthorizeCall(constructorArgs: null, namedArgs: null);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    #endregion
}
