namespace FlowLedger.Consolidation.Worker.Domain;

public sealed class ConsolidatedBalance
{
    public string MerchantId { get; init; } = string.Empty;

    public DateOnly ReferenceDate { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal TotalCredits { get; set; }

    public decimal TotalDebits { get; set; }

    public decimal Balance => TotalCredits - TotalDebits;

    public DateTimeOffset UpdatedAt { get; set; }
}
