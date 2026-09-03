namespace FlowLedger.Contracts.IntegrationEvents.Transactions;

public sealed record TransactionCreatedIntegrationEvent(
    Guid TransactionId,
    string MerchantId,
    DateOnly ReferenceDate,
    TransactionType TransactionType,
    decimal Amount,
    string Currency) : IntegrationEvent;
