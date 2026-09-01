using FlowLedger.Transactions.Api.Domain.Entities;
using FlowLedger.Transactions.Api.Domain.ValueObjects;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;

namespace FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;

public sealed class CreateTransactionHandler(
    TransactionsDbContext dbContext)
{
    public async Task<CreateTransactionResult> HandleAsync(
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

        dbContext.Transactions.Add(transaction);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateTransactionResult(
            transaction.Id,
            transaction.CreatedAt);
    }
}

public sealed record CreateTransactionResult(
    Guid Id,
    DateTimeOffset CreatedAt);