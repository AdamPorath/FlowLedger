namespace FlowLedger.Transactions.Api.Domain.Events;

public sealed record TransactionCreated(
    Guid TransactionId,
    string MerchantId,
    DateOnly ReferenceDate,
    decimal Amount,
    string Currency) : IDomainEvent;