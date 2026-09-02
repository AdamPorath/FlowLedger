namespace FlowLedger.Contracts.IntegrationEvents.Transactions;

public sealed record TransactionCreatedIntegrationEvent(
    Guid TransactionId,
    string MerchantId,
    DateOnly ReferenceDate,
    decimal Amount,
    string Currency) : IntegrationEvent;
