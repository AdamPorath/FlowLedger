using FlowLedger.Transactions.Api.Domain.ValueObjects;

namespace FlowLedger.Transactions.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Create_WithValidAmountAndCurrency_NormalizesCurrencyToUpperInvariant()
    {
        var money = Money.Create(150.75m, "brl");

        Assert.Equal(150.75m, money.Amount);
        Assert.Equal("BRL", money.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_WhenAmountIsNotPositive_Throws(decimal amount)
    {
        var exception = Assert.Throws<ArgumentException>(() => Money.Create(amount, "BRL"));

        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WhenCurrencyIsMissing_Throws(string? currency)
    {
        var exception = Assert.Throws<ArgumentException>(() => Money.Create(100m, currency!));

        Assert.Equal("currency", exception.ParamName);
    }
}
