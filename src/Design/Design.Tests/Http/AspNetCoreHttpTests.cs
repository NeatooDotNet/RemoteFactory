// =============================================================================
// DESIGN SOURCE OF TRUTH: What Crosses Over HTTP
// =============================================================================
//
// Each test sends a Remote-mode client's calls to Design.Server's real Program
// (DesignHttpHost) and checks what only HTTP shows: the status a server
// exception becomes, the headers UseNeatoo() reads and writes, the request's
// cancellation token, and the log categories the server writes to.
//
// =============================================================================

using System.Net;
using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Design.Domain.ReadModels;
using Design.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace Design.Tests.Http;

public class AspNetCoreHttpTests(DesignHttpHost host)
{
    private static async Task<IOrder> SaveNewOrder(IServiceScope client, string customer)
    {
        var orderFactory = client.ServiceProvider.GetRequiredService<IOrderFactory>();
        var lineFactory = client.ServiceProvider.GetRequiredService<IOrderLineFactory>();
        var order = orderFactory.Create(customer);
        order.AddLine(lineFactory.Create("Widget H", 4.00m, 3));
        return (await orderFactory.Save(order, TestContext.Current.CancellationToken))!;
    }

    [Fact]
    public async Task Order_SavesAndFetches_OverHttp()
    {
        using var client = host.CreateClientScope();
        var orderFactory = client.ServiceProvider.GetRequiredService<IOrderFactory>();

        var saved = await SaveNewOrder(client, "Http_Customer");
        var fetched = await orderFactory.Fetch(saved.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(fetched);
        Assert.False(fetched.IsNew);
        Assert.Equal("Http_Customer", fetched.CustomerName);
        Assert.Equal(3, Assert.Single(fetched.Lines).Quantity);
    }

    /// <summary>
    /// The server's handler rethrows, so ASP.NET Core answers 500 and the client's
    /// HTTP call throws HttpRequestException. The server's exception type does not cross.
    /// </summary>
    [Fact]
    public async Task ServerException_ReachesClient_AsHttpRequestException_With500()
    {
        using var client = host.CreateClientScope();
        var orderFactory = client.ServiceProvider.GetRequiredService<IOrderFactory>();
        var ct = TestContext.Current.CancellationToken;

        // Two screens hold the same order; one deletes it, the other then saves.
        var saved = await SaveNewOrder(client, "Http_Stale");
        var first = (await orderFactory.Fetch(saved.Id, ct))!;
        var second = (await orderFactory.Fetch(saved.Id, ct))!;
        first.MarkDeleted();
        await orderFactory.Save(first, ct);

        second.CustomerName = "Http_Stale_Renamed";
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => orderFactory.Save(second, ct));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
    }

    /// <summary>
    /// The client sends its correlation ID; UseNeatoo() puts it in the server's
    /// ICorrelationContext and echoes it on the response.
    /// </summary>
    [Fact]
    public async Task CorrelationId_FromClient_ReachesServerOperation_AndReturnsOnResponse()
    {
        var recorder = new ResponseRecorder();
        using var client = host.CreateClientScope(recorder);
        var reportProblem = client.ServiceProvider.GetRequiredService<SupportRequests.ReportProblem>();

        var reference = await reportProblem(42, "Total looks wrong", TestContext.Current.CancellationToken);

        var clientId = client.ServiceProvider.GetRequiredService<ICorrelationContext>().CorrelationId;
        Assert.False(string.IsNullOrEmpty(clientId));
        Assert.Equal(clientId, reference);
        Assert.Equal(clientId, Assert.Single(recorder.LastResponse!.Headers.GetValues("X-Correlation-Id")));
        Assert.Contains(host.Services.GetRequiredService<ISupportLog>().Entries,
            e => e.Reference == clientId && e.OrderId == 42);
    }

    /// <summary>
    /// The server operation's CancellationToken is the request's: cancelling the
    /// client call cancels the server's read.
    /// </summary>
    [Fact]
    public async Task CancelledCall_CancelsTheServerOperationsToken()
    {
        using var client = host.CreateClientScope();
        var summaries = client.ServiceProvider.GetRequiredService<IOrderSummaryFactory>();
        var saved = await SaveNewOrder(client, "Http_Cancel");
        var hold = host.Store.HoldNextReadOf(saved.Id);

        using var cts = new CancellationTokenSource();
        var fetch = summaries.Fetch(saved.Id, cts.Token);
        await hold.Entered.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fetch);
        var serverToken = await hold.Released.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.True(serverToken.IsCancellationRequested);
    }

    /// <summary>
    /// With no options passed, both tiers use Ordinal, and the server says so on
    /// every response. The client does not read the header; the tiers agree by
    /// registering the same format.
    /// </summary>
    [Fact]
    public async Task Server_AnnouncesItsFormat_OrdinalByDefault()
    {
        var recorder = new ResponseRecorder();
        using var client = host.CreateClientScope(recorder);
        var orderFactory = client.ServiceProvider.GetRequiredService<IOrderFactory>();
        var saved = await SaveNewOrder(client, "Http_Format");

        var fetched = await orderFactory.Fetch(saved.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(fetched);
        Assert.Equal(SerializationFormat.Ordinal, host.Services.GetRequiredService<NeatooSerializationOptions>().Format);
        Assert.Equal("ordinal", Assert.Single(recorder.LastResponse!.Headers.GetValues(NeatooSerializationOptions.FormatHeaderName)));
    }

    /// <summary>
    /// The server's request handling logs under Neatoo.RemoteFactory, the generated
    /// factory under its own type name; RemoteFactory.Trace is filtered to warnings.
    /// </summary>
    [Fact]
    public async Task ServerLogging_WritesTheConfiguredCategories()
    {
        using var client = host.CreateClientScope();
        var summaries = client.ServiceProvider.GetRequiredService<IOrderSummaryFactory>();
        var saved = await SaveNewOrder(client, "Http_Logging");

        await summaries.Fetch(saved.Id, TestContext.Current.CancellationToken);

        var correlationId = client.ServiceProvider.GetRequiredService<ICorrelationContext>().CorrelationId!;
        var entries = host.Logs.Entries.Where(e => e.Message.Contains(correlationId, StringComparison.Ordinal)).ToList();
        Assert.Contains(entries, e => e.Category == "Neatoo.RemoteFactory.Server" && e.Level == LogLevel.Information);
        Assert.Contains(entries, e => e.Category == "Design.Domain.ReadModels.OrderSummaryFactory");
        Assert.DoesNotContain(host.Logs.Entries, e => e.Category == "RemoteFactory.Trace" && e.Level < LogLevel.Warning);
    }

}
