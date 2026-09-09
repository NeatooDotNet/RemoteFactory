using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests;

/// <summary>
/// Tests for <see cref="NotAuthorizedException"/>'s message construction.
/// </summary>
/// <remarks>
/// WHY THIS EXISTS: the exception previously used <c>authorized?.Message ?? ""</c>, so an
/// authorization method returning a bare <c>false</c> produced an exception whose Message was
/// the empty string. Application Insights rejects exception telemetry with an empty message at
/// ingestion, so a consumer's nightly job failed silently for four months -- the refusal was
/// correct, but no record of it reached telemetry. These tests pin that the message is never
/// empty, and that a message the auth method DID supply is still passed through untouched.
/// </remarks>
public class NotAuthorizedExceptionTests
{
    private const string NoReason = "Authorization denied; no reason was supplied.";

    #region No message supplied -- the defect

    [Fact]
    public void BoolDenial_WithoutContext_HasNonEmptyMessage()
    {
        // Authorized(bool) leaves Message null -- this is what an auth method returning
        // `false` produces, via the implicit bool -> Authorized conversion.
        var ex = new NotAuthorizedException(new Authorized(false));

        Assert.Equal(NoReason, ex.Message);
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Null(ex.Context);
        Assert.NotNull(ex.Authorized);
        Assert.False(ex.Authorized!.HasAccess);
    }

    [Fact]
    public void BoolDenial_ViaImplicitConversion_HasNonEmptyMessage()
    {
        // The path the generator actually takes: the auth method's bool return is assigned
        // into an `Authorized` local, so the conversion runs before the ctor sees it.
        Authorized authorized = false;

        var ex = new NotAuthorizedException(authorized);

        Assert.Equal(NoReason, ex.Message);
    }

    [Fact]
    public void BoolDenial_WithContext_MessageNamesWhatWasRefused()
    {
        var ex = new NotAuthorizedException(new Authorized(false), "operation Save on IVisitV2");

        Assert.Equal("Authorization denied for operation Save on IVisitV2; no reason was supplied.", ex.Message);
        Assert.Equal("operation Save on IVisitV2", ex.Context);
    }

    [Fact]
    public void WhitespaceMessage_IsTreatedAsAbsent()
    {
        // new Authorized("") sets HasAccess=false with an empty Message, and the string
        // conversion's IsNullOrEmpty check lets "   " through as a denial. Neither says
        // anything, so both must fall back to the default rather than produce a blank message.
        Assert.Equal(NoReason, new NotAuthorizedException(new Authorized("")).Message);
        Assert.Equal(NoReason, new NotAuthorizedException(new Authorized("   ")).Message);
    }

    [Fact]
    public void NullAuthorized_DoesNotThrow_AndStillHasAMessage()
    {
        // The ctor is null-tolerant today (authorized?.Message); keep it that way.
        var ex = new NotAuthorizedException((Authorized)null!, "operation Fetch on IThing");

        Assert.Equal("Authorization denied for operation Fetch on IThing; no reason was supplied.", ex.Message);
        Assert.Null(ex.Authorized);
    }

    #endregion

    #region A message WAS supplied -- must be verbatim

    [Fact]
    public void SuppliedMessage_IsUsedVerbatim_WithoutContext()
    {
        var ex = new NotAuthorizedException(new Authorized("User lacks PROVIDER role"));

        Assert.Equal("User lacks PROVIDER role", ex.Message);
    }

    [Fact]
    public void SuppliedMessage_IsUsedVerbatim_EvenWithContext()
    {
        // Context must never prefix or decorate a message the consumer wrote -- it is exposed
        // as a property instead, so structured logging can capture the operation without
        // changing text that consumers may already assert on.
        var ex = new NotAuthorizedException(new Authorized("User lacks PROVIDER role"), "operation Save on IVisitV2");

        Assert.Equal("User lacks PROVIDER role", ex.Message);
        Assert.Equal("operation Save on IVisitV2", ex.Context);
    }

    [Fact]
    public void SuppliedMessage_ViaImplicitStringConversion_IsUsedVerbatim()
    {
        // A non-empty string converts to a denial carrying that message.
        Authorized authorized = "ExecuteDenied";

        var ex = new NotAuthorizedException(authorized, "operation GetData on IThing");

        Assert.False(authorized.HasAccess);
        Assert.Equal("ExecuteDenied", ex.Message);
    }

    #endregion

    #region Authorized<T> shapes reach the ctor the same way

    [Fact]
    public void AuthorizedOfT_CopiesMessage_FromAuthorized()
    {
        var typed = new Authorized<string>(new Authorized("denied by rule"));

        Assert.Equal("denied by rule", new NotAuthorizedException(typed).Message);
    }

    [Fact]
    public void AuthorizedOfT_WithoutMessage_FallsBackToDefault()
    {
        // Both the parameterless ctor and the null-result ctor leave Message null; the Save
        // wrapper receives these from LocalInsert/Update/Delete.
        Assert.Equal(NoReason, new NotAuthorizedException(new Authorized<string>()).Message);
        Assert.Equal(NoReason, new NotAuthorizedException(new Authorized<string>((string?)null)).Message);
    }

    #endregion

    #region The legacy overloads are unchanged

    [Fact]
    public void StringOverload_IsUnchanged()
    {
        Assert.Equal("explicit", new NotAuthorizedException("explicit").Message);
        Assert.Null(new NotAuthorizedException("explicit").Context);
        Assert.Null(new NotAuthorizedException("explicit").Authorized);
    }

    [Fact]
    public void InnerExceptionOverload_IsUnchanged()
    {
        var inner = new InvalidOperationException("inner");

        var ex = new NotAuthorizedException("outer", inner);

        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    #endregion
}
