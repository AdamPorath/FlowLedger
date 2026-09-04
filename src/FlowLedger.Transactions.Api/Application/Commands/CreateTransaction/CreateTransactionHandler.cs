using System.Diagnostics.Metrics;
using FlowLedger.Transactions.Api.Domain.Entities;
using FlowLedger.Transactions.Api.Domain.ValueObjects;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;

public sealed class CreateTransactionHandler(
    TransactionsDbContext dbContext,
    ILogger<CreateTransactionHandler> logger)
{
    private static readonly Meter Meter = new("FlowLedger.Transactions.Api");
    private static readonly Counter<long> TransactionsCreatedCounter =
        Meter.CreateCounter<long>("flowledger.transactions.created");

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

        TransactionsCreatedCounter.Add(
            1,
            new KeyValuePair<string, object?>("currency", money.Currency),
            new KeyValuePair<string, object?>("type", command.Type.ToString()));

        logger.LogInformation(
            "Transaction {TransactionId} created for merchant {MerchantId} ({Type} {Amount} {Currency})",
            transaction.Id,
            merchantId,
            command.Type,
            money.Amount,
            money.Currency);

        return new CreateTransactionResult(
            transaction.Id,
            transaction.CreatedAt);
    }
}

public sealed record CreateTransactionResult(
    Guid Id,
    DateTimeOffset CreatedAt);