namespace FlowLedger.Transactions.Api.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount <= 0)
            throw new ArgumentException(
                "Amount must be greater than zero.",
                nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException(
                "Currency is a required field.",
                nameof(currency));

        return new Money(amount, currency.ToUpperInvariant());
    }
}