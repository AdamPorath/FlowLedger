using FlowLedger.Transactions.Api.Domain.Enums;

namespace FlowLedger.Transactions.Api.Domain.Events;

public sealed record TransactionCreated(
    Guid TransactionId,
    string MerchantId,
    DateOnly ReferenceDate,
    TransactionType Type,
    decimal Amount,
    string Currency) : IDomainEvent;