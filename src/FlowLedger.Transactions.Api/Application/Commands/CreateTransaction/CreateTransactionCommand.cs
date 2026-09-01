using FlowLedger.Transactions.Api.Domain.Enums;

namespace FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;

public sealed record CreateTransactionCommand(
    DateOnly ReferenceDate,
    TransactionType Type,
    decimal Amount,
    string Currency,
    string Description,
    string CreatedBy);