namespace FlowLedger.Consolidation.Worker.Domain;

public sealed class ConsolidatedTransaction
{
    public Guid TransactionId { get; init; }

    public string MerchantId { get; init; } = string.Empty;

    public DateOnly ReferenceDate { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public DateTimeOffset ConsolidatedAt { get; init; }
}
