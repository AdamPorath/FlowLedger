using FlowLedger.Transactions.Api.Domain.Enums;

namespace FlowLedger.Transactions.Api.Application.Queries.GetTransaction;

public sealed record GetTransactionResponse(
    Guid Id,
    string MerchantId,
    DateOnly ReferenceDate,
    TransactionType Type,
    decimal Amount,
    string Currency,
    string Description,
    string CreatedBy,
    DateTimeOffset CreatedAt);