// =============================================================================
// DESIGN SOURCE OF TRUTH: Value Object Tests
// =============================================================================
//
// Money and Percentage are plain records whose constructors throw on an invalid
// value. TaxRate carries [Factory] because its rate comes from a server-only
// table (VisionAlignment R-F).
//
// =============================================================================

using Design.Domain.ValueObjects;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class ValueObjectTests
{
    // -------------------------------------------------------------------------
    // Plain records: the constructor throws
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("")]
    [InlineData("U$D")]
    public void Money_RejectsACurrencyThatIsNotAThreeLetterCode(string currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(10m, currency));
    }

    [Fact]
    public void Money_KeepsItsCurrency_ThroughArithmetic()
    {
        var price = new Money(10m, "EUR");

        var total = price.Multiply(3).Add(new Money(5m, "EUR"));

        Assert.Equal(new Money(35m, "EUR"), total);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Percentage_RejectsAValueOutsideZeroToOneHundred(decimal value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Percentage_AcceptsTheBounds(decimal value)
    {
        Assert.Equal(value, new Percentage(value).Value);
    }

    // -------------------------------------------------------------------------
    // TaxRate: [Factory] because construction needs a server-only service
    // -------------------------------------------------------------------------

    /// <summary>
    /// The client loads a region's rate in one call and applies it to an amount.
    /// </summary>
    [Fact]
    public async Task TaxRate_LoadsFromTheServer_AndAppliesToMoney()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ITaxRateFactory>();

        var taxRate = await factory.Fetch("WA");

        Assert.Equal(1, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);
        Assert.NotNull(taxRate);
        Assert.Equal("WA", taxRate.Region);
        Assert.Equal(new Percentage(6.5m), taxRate.Rate);
        Assert.Equal(new Money(13.00m), taxRate.Rate.Of(new Money(200m)));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A region the table does not hold is null on the client, not an exception (R-I).
    /// </summary>
    [Fact]
    public async Task TaxRate_IsNull_ForAnUnknownRegion()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ITaxRateFactory>();

        var taxRate = await factory.Fetch("ZZ");

        Assert.Null(taxRate);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// The tax table is the reason TaxRate has a factory: it exists only on the server.
    /// </summary>
    [Fact]
    public void TaxTable_IsRegisteredOnTheServer_AndNotOnTheClient()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();

        Assert.NotNull(server.ServiceProvider.GetService<ITaxTable>());
        Assert.Null(client.ServiceProvider.GetService<ITaxTable>());

        server.Dispose();
        client.Dispose();
    }
}
