using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Core;

/// <summary>
/// Tests static [Create] method on a [Factory] class.
/// </summary>
[Factory]
public class StaticCreateTarget
{
    public StaticCreateTarget()
    {
        NullablePropertiesAreOk = "NullablePropertiesAreOk";
    }

    public string NullablePropertiesAreOk { get; set; }
    public bool UsedStaticMethod { get; set; } = false;

    [Create]
    public static StaticCreateTarget Create()
    {
        return new StaticCreateTarget
        {
            UsedStaticMethod = true
        };
    }
}

/// <summary>
/// Tests static async [Fetch] method returning Task&lt;T&gt;.
/// </summary>
[Factory]
public class StaticAsyncFetchTarget
{
    public StaticAsyncFetchTarget()
    {
        NullablePropertiesAreOk = "NullablePropertiesAreOk";
    }

    public string NullablePropertiesAreOk { get; set; }
    public bool UsedStaticMethod { get; set; } = false;

    [Fetch]
    public static Task<StaticAsyncFetchTarget> Fetch()
    {
        var result = new StaticAsyncFetchTarget
        {
            UsedStaticMethod = true
        };
        return Task.FromResult(result);
    }
}

/// <summary>
/// Tests static async [Fetch] method with parameters and [Service] injection.
/// </summary>
[Factory]
public class StaticAsyncFetchWithParamsTarget
{
    public StaticAsyncFetchWithParamsTarget()
    {
        NullablePropertiesAreOk = "NullablePropertiesAreOk";
    }

    public string NullablePropertiesAreOk { get; set; }
    public bool UsedStaticMethod { get; set; } = false;

    [Fetch]
    public static Task<StaticAsyncFetchWithParamsTarget> Fetch(int? a, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        var result = new StaticAsyncFetchWithParamsTarget
        {
            UsedStaticMethod = true
        };
        return Task.FromResult(result);
    }
}

/// <summary>
/// Authorization class for StaticFetchWithAuthTarget.
/// </summary>
public class StaticFetchAuthorizor
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    public bool CanFetch(int? a)
    {
        // Deny access when a == 20
        if (a == 20)
        {
            return false;
        }
        return true;
    }
}

/// <summary>
/// Tests static async [Fetch] with authorization.
/// </summary>
[Factory]
[AuthorizeFactory<StaticFetchAuthorizor>]
public class StaticFetchWithAuthTarget
{
    public StaticFetchWithAuthTarget()
    {
        NullablePropertiesAreOk = "NullablePropertiesAreOk";
    }

    public string NullablePropertiesAreOk { get; set; }
    public bool UsedStaticMethod { get; set; } = false;

    [Fetch]
    public static Task<StaticFetchWithAuthTarget> Fetch(int? a, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");
        var result = new StaticFetchWithAuthTarget
        {
            UsedStaticMethod = true
        };
        return Task.FromResult(result);
    }
}

/// <summary>
/// Tests static async [Fetch] with nullable return type.
/// </summary>
[Factory]
public class StaticFetchNullableTarget
{
    public StaticFetchNullableTarget()
    {
        NullablePropertiesAreOk = "NullablePropertiesAreOk";
    }

    public string NullablePropertiesAreOk { get; set; }
    public bool UsedStaticMethod { get; set; } = false;

    [Fetch]
    public static Task<StaticFetchNullableTarget?> Fetch(int? a, [Service] IService service)
    {
        if (service == null)
            throw new InvalidOperationException("Service was not injected");

        // Return null when a == 20
        if (a == 20)
        {
            return Task.FromResult<StaticFetchNullableTarget?>(null);
        }

        var result = new StaticFetchNullableTarget
        {
            UsedStaticMethod = true
        };
        return Task.FromResult<StaticFetchNullableTarget?>(result);
    }
}
