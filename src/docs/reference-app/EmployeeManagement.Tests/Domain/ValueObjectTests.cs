using EmployeeManagement.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using EmployeeManagement.Tests.TestContainers;

namespace EmployeeManagement.Tests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void EmailAddress_ValidEmail_CreatesSuccessfully()
    {
        // Arrange & Act
        var email = new EmailAddress("test@example.com");

        // Assert
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void EmailAddress_InvalidEmail_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new EmailAddress("invalid"));
    }

    [Fact]
    public void Money_ValidAmount_CreatesSuccessfully()
    {
        // Arrange & Act
        var money = new Money(100.50m, "USD");

        // Assert
        Assert.Equal(100.50m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Money_NegativeAmount_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Money(-100, "USD"));
    }

    [Fact]
    public void Money_Addition_SameCurrency_Works()
    {
        // Arrange
        var a = new Money(100, "USD");
        var b = new Money(50, "USD");

        // Act
        var result = a + b;

        // Assert
        Assert.Equal(150, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Money_Addition_DifferentCurrency_ThrowsException()
    {
        // Arrange
        var a = new Money(100, "USD");
        var b = new Money(50, "EUR");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => a + b);
    }

    [Fact]
    public void PhoneNumber_ValidNumber_CreatesSuccessfully()
    {
        // Arrange & Act
        var phone = new PhoneNumber("+1", "555-1234");

        // Assert
        Assert.Equal("+1", phone.CountryCode);
        Assert.Equal("555-1234", phone.Number);
        Assert.Equal("+1 555-1234", phone.ToString());
    }

    [Fact]
    public void EmailAddress_Equality_Works()
    {
        // Arrange
        var email1 = new EmailAddress("test@example.com");
        var email2 = new EmailAddress("test@example.com");
        var email3 = new EmailAddress("other@example.com");

        // Assert
        Assert.Equal(email1, email2);
        Assert.NotEqual(email1, email3);
    }
}
