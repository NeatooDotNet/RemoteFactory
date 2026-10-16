// =============================================================================
// DESIGN SOURCE OF TRUTH: Class Hierarchy Tests
// =============================================================================
//
// Payment is an abstract base with a [Create]; CardPayment and InvoicePayment
// carry their own [Factory]. The base's operation joins each concrete factory,
// and each concrete payment crosses the wire as its own type, also when the
// declared type is the base interface.
//
// =============================================================================

using Design.Domain.Payments;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class PaymentHierarchyTests
{
    /// <summary>
    /// The base's [Create] is on each concrete factory, and it runs on the client:
    /// no call crosses.
    /// </summary>
    [Fact]
    public void ConcreteFactories_CarryTheBasesCreate_AndRunItOnTheClient()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var calls = client.GetRequiredService<ServerServiceProvider>();
        var callsBefore = calls.RemoteCallCount;

        ICardPayment card = client.GetRequiredService<ICardPaymentFactory>().Create(7, 20.00m);
        IInvoicePayment invoice = client.GetRequiredService<IInvoicePaymentFactory>().Create(8, 50.00m);

        Assert.Equal(7, card.OrderId);
        Assert.Equal(20.00m, card.Amount);
        Assert.Equal(8, invoice.OrderId);
        Assert.Equal(50.00m, invoice.Amount);
        Assert.Equal(callsBefore, calls.RemoteCallCount);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Each concrete payment loads across the wire as its own type, with the
    /// base's data and its own; the other kind's factory finds nothing.
    /// </summary>
    [Fact]
    public async Task ConcretePayments_LoadAcrossTheWire_WithTheirOwnAndTheBasesData()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var cards = client.GetRequiredService<ICardPaymentFactory>();
        var invoices = client.GetRequiredService<IInvoicePaymentFactory>();
        var calls = client.GetRequiredService<ServerServiceProvider>();
        var callsBefore = calls.RemoteCallCount;

        var card = await cards.Fetch(PaymentLedger.CardOrderId);
        var invoice = await invoices.Fetch(PaymentLedger.InvoiceOrderId);

        Assert.NotNull(card);
        Assert.Equal(PaymentLedger.CardOrderId, card.OrderId);
        Assert.Equal(37.50m, card.Amount);
        Assert.Equal("4242", card.CardLast4);
        Assert.NotNull(invoice);
        Assert.Equal(120.00m, invoice.Amount);
        Assert.Equal(30, invoice.NetDays);
        Assert.Null(await cards.Fetch(PaymentLedger.InvoiceOrderId));
        Assert.Equal(callsBefore + 3, calls.RemoteCallCount);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// The domain service returns IPayment; the client receives the concrete type
    /// the server chose, with its own data intact. One call each.
    /// </summary>
    [Fact]
    public async Task PaymentLookup_ReturnsEachConcreteType_ThroughTheBaseInterface()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var lookup = client.GetRequiredService<IPaymentLookup>();
        var calls = client.GetRequiredService<ServerServiceProvider>();
        var callsBefore = calls.RemoteCallCount;

        var cardPayment = await lookup.ForOrderAsync(PaymentLedger.CardOrderId);
        var invoicePayment = await lookup.ForOrderAsync(PaymentLedger.InvoiceOrderId);

        var card = Assert.IsAssignableFrom<ICardPayment>(cardPayment);
        Assert.Equal("4242", card.CardLast4);
        Assert.Equal(37.50m, card.Amount);
        var invoice = Assert.IsAssignableFrom<IInvoicePayment>(invoicePayment);
        Assert.Equal(30, invoice.NetDays);
        Assert.Equal(PaymentLedger.InvoiceOrderId, invoice.OrderId);
        Assert.Null(await lookup.ForOrderAsync(999));
        Assert.Equal(callsBefore + 3, calls.RemoteCallCount);

        server.Dispose();
        client.Dispose();
    }
}
