using FlowLedger.Transactions.Api.Domain.Entities;
using FlowLedger.Transactions.Api.Domain.ValueObjects;

namespace FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;

public sealed class CreateTransactionHandler
{
    public Task<CreateTransactionResult> HandleAsync(
        string merchantId,
        CreateTransactionCommand command,
        CancellationToken cancellationToken)
    {
        var money = Money.Create(
            command.Amount,
            command.Currency);

        var transaction = Transaction.Create(
            merchantId,
            command.ReferenceDate,
            command.Type,
            money,
            command.Description,
            command.CreatedBy);

        var result = new CreateTransactionResult(
            transaction.Id,
            transaction.CreatedAt);

        return Task.FromResult(result);
    }
}

public sealed record CreateTransactionResult(
    Guid Id,
    DateTimeOffset CreatedAt);