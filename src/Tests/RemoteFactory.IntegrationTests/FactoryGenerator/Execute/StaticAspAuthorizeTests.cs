using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Execute;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Execute;

/// <summary>
/// [AspAuthorize] on a static-factory [Execute] is enforced on the server (#91).
/// </summary>
/// <remarks>
/// The check runs inside the server's delegate, before the method, with forbid set: a refusal
/// never reaches the method body. Over HTTP the real IAspAuthorize answers a refusal with a 403,
/// which the client raises as HttpRequestException, as for an interface factory. Here a stand-in
/// throws <see cref="AspForbidException"/> as the real one does; the in-process containers carry
/// no status code, so what the client sees is not asserted. The denied tests pin the server side:
/// the check ran with forbid set and the method never ran.
/// </remarks>
public class StaticAspAuthorizeTests
{
    /// <summary>
    /// Stands in for the AspNetCore <c>IAspAuthorize</c>: records each check and, when denying a
    /// forbid check, throws <see cref="AspForbidException"/> as the real one does after ForbidAsync.
    /// </summary>
    private sealed class ForbiddingAspAuthorize : IAspAuthorize
    {
        public List<(string? Policy, string? Roles, bool Forbid)> Checks { get; } = [];
        public bool Deny { get; init; }

        public Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)
        {
            foreach (var data in authorizeData)
            {
                Checks.Add((data.Policy, data.Roles, forbid));
            }

            if (!Deny)
            {
                return Task.FromResult<string?>(string.Empty);
            }

            if (forbid)
            {
                throw new AspForbidException("Denied by test");
            }

            return Task.FromResult<string?>("Denied by test");
        }
    }

    private static (IServiceScope client, IServiceScope server, ForbiddingAspAuthorize asp) Scopes(bool deny)
    {
        // Singleton so the instance read back here is the one the server's delegate resolved.
        var asp = new ForbiddingAspAuthorize { Deny = deny };
        var (client, server, _) = ClientServerContainers.Scopes(
            configureServer: services => services.AddSingleton<IAspAuthorize>(asp));
        return (client, server, asp);
    }

    private static IReadOnlyCollection<string> Recorded(IServiceScope server) =>
        server.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded;

    [Fact]
    public async Task StaticExecute_AspAuthorize_Allowed_RunsOnServer_AfterTheCheck()
    {
        var (client, server, asp) = Scopes(deny: false);
        var run = client.ServiceProvider.GetRequiredService<StaticAspAuthorizeCommands.RunWithResult>();

        var result = await run("hello");

        Assert.Equal("ServerOnly: hello", result);
        Assert.Equal(["result:hello"], Recorded(server));
        Assert.Equal([("CommandPolicy", (string?)null, true)], asp.Checks);
        Assert.Equal(1, client.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
    }

    [Fact]
    public async Task StaticExecute_AspAuthorize_Denied_MethodNeverRuns()
    {
        var (client, server, asp) = Scopes(deny: true);
        var run = client.ServiceProvider.GetRequiredService<StaticAspAuthorizeCommands.RunWithResult>();

        await Record.ExceptionAsync(() => run("hello"));

        Assert.Empty(Recorded(server));
        Assert.Equal([("CommandPolicy", (string?)null, true)], asp.Checks);
    }

    [Fact]
    public async Task StaticExecute_NoResult_AspAuthorize_Allowed_Runs()
    {
        var (client, server, asp) = Scopes(deny: false);
        var run = client.ServiceProvider.GetRequiredService<StaticAspAuthorizeCommands.RunNoResult>();

        await run("hello");

        Assert.Equal(["no-result:hello"], Recorded(server));
        Assert.Equal([("CommandPolicy", (string?)null, true)], asp.Checks);
    }

    [Fact]
    public async Task StaticExecute_NoResult_AspAuthorize_Denied_MethodNeverRuns()
    {
        var (client, server, asp) = Scopes(deny: true);
        var run = client.ServiceProvider.GetRequiredService<StaticAspAuthorizeCommands.RunNoResult>();

        await Record.ExceptionAsync(() => run("hello"));

        Assert.Empty(Recorded(server));
        Assert.Equal([("CommandPolicy", (string?)null, true)], asp.Checks);
    }

    [Fact]
    public async Task StaticExecute_BareWithAspAuthorize_CrossesTheWire_AndChecksEveryAttribute()
    {
        var (client, server, asp) = Scopes(deny: false);
        var run = client.ServiceProvider.GetRequiredService<StaticAspAuthorizeCommands.RunBare>();

        var result = await run("hello");

        Assert.Equal("bare: hello", result);
        Assert.Equal(["bare:hello"], Recorded(server));
        Assert.Equal(
            [("CommandPolicy", (string?)null, true), ((string?)null, "Admin", true)],
            asp.Checks);
        Assert.Equal(1, client.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
    }

    [Fact]
    public async Task StaticExecute_BareWithAspAuthorize_Denied_MethodNeverRuns()
    {
        var (client, server, asp) = Scopes(deny: true);
        var run = client.ServiceProvider.GetRequiredService<StaticAspAuthorizeCommands.RunBare>();

        await Record.ExceptionAsync(() => run("hello"));

        Assert.Empty(Recorded(server));
        Assert.Equal(
            [("CommandPolicy", (string?)null, true), ((string?)null, "Admin", true)],
            asp.Checks);
    }
}
