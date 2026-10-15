using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Execute;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Execute;

/// <summary>
/// [AuthorizeFactory&lt;T&gt;] on a static-factory [Execute] is enforced where the command runs.
/// </summary>
/// <remarks>
/// A delegate has no Authorized&lt;T&gt; to carry a refusal back, so a refusal throws
/// NotAuthorizedException, as an interface factory's does. The in-process containers carry the
/// server's exception type to the client; over HTTP the server answers 500.
/// </remarks>
public class StaticAuthorizeFactoryTests
{
    private static (IServiceScope client, IServiceScope server, IServiceScope local) Scopes(bool allow)
    {
        void Configure(IServiceCollection services) => services.AddSingleton(new StaticCommandAuthSwitch { Allow = allow });
        return ClientServerContainers.Scopes(configureClient: Configure, configureServer: Configure, configureLocal: Configure);
    }

    private static IReadOnlyCollection<string> Recorded(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IVoidExecuteRecorder>().Recorded;

    [Fact]
    public async Task StaticExecute_AuthorizeFactory_Allowed_Runs()
    {
        var (client, server, _) = Scopes(allow: true);
        var run = client.ServiceProvider.GetRequiredService<StaticAuthorizeFactoryCommands.RunWithResult>();

        Assert.Equal("ran: hello", await run("hello"));
        Assert.Equal(["result:hello"], Recorded(server));
    }

    [Fact]
    public async Task StaticExecute_AuthorizeFactory_Denied_Throws_AndTheMethodNeverRuns()
    {
        var (client, server, _) = Scopes(allow: false);
        var run = client.ServiceProvider.GetRequiredService<StaticAuthorizeFactoryCommands.RunWithResult>();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => run("hello"));

        Assert.Contains("StaticCommandAuth", ex.Message, StringComparison.Ordinal);
        Assert.Empty(Recorded(server));
    }

    [Fact]
    public async Task StaticExecute_AuthorizeFactory_TypeMatchedCheck_RefusesWithItsReason()
    {
        var (client, server, _) = Scopes(allow: true);
        var run = client.ServiceProvider.GetRequiredService<StaticAuthorizeFactoryCommands.RunWithResult>();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => run("blocked"));

        Assert.Equal("Input is blocked", ex.Message);
        Assert.Empty(Recorded(server));
    }

    [Fact]
    public async Task StaticExecute_NoResult_AuthorizeFactory_Denied_MethodNeverRuns()
    {
        var (client, server, _) = Scopes(allow: false);
        var run = client.ServiceProvider.GetRequiredService<StaticAuthorizeFactoryCommands.RunNoResult>();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => run("hello"));

        Assert.Empty(Recorded(server));
    }

    [Fact]
    public async Task StaticExecute_Bare_AuthorizeFactory_Denied_OnTheTierThatRunsIt()
    {
        var (_, _, local) = Scopes(allow: false);
        var run = local.ServiceProvider.GetRequiredService<StaticAuthorizeFactoryCommands.RunLocal>();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => run("hello"));

        Assert.Empty(Recorded(local));
    }
}
