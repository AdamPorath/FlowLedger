using System.Diagnostics.Metrics;
using FlowLedger.Consolidation.Worker.Domain;
using FlowLedger.Consolidation.Worker.Infrastructure.Persistence;
using FlowLedger.Contracts.IntegrationEvents.Transactions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowLedger.Consolidation.Worker.Consumers;

public sealed class TransactionCreatedConsumer(
    ConsolidationDbContext dbContext,
    ILogger<TransactionCreatedConsumer> logger)
    : IConsumer<TransactionCreatedIntegrationEvent>
{
    private static readonly Meter Meter = new("FlowLedger.Consolidation.Worker");
    private static readonly Counter<long> MessagesProcessedCounter =
        Meter.CreateCounter<long>("flowledger.consolidation.messages_processed");

    public async Task Consume(
        ConsumeContext<TransactionCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        var balance = await dbContext.ConsolidatedBalances.FindAsync(
            [message.MerchantId, message.ReferenceDate, message.Currency],
            context.CancellationToken);

        if (balance is null)
        {
            balance = new ConsolidatedBalance
            {
                MerchantId = message.MerchantId,
                ReferenceDate = message.ReferenceDate,
                Currency = message.Currency,
            };

            dbContext.ConsolidatedBalances.Add(balance);
        }

        if (message.TransactionType == TransactionType.Credit)
        {
            balance.TotalCredits += message.Amount;
        }
        else
        {
            balance.TotalDebits += message.Amount;
        }

        balance.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(context.CancellationToken);

        MessagesProcessedCounter.Add(
            1,
            new KeyValuePair<string, object?>("currency", message.Currency),
            new KeyValuePair<string, object?>("type", message.TransactionType.ToString()));

        logger.LogInformation(
            "Consolidated balance updated for merchant {MerchantId} on {ReferenceDate} ({Type} {Amount} {Currency})",
            message.MerchantId,
            message.ReferenceDate,
            message.TransactionType,
            message.Amount,
            message.Currency);
    }
}
