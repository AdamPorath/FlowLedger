using FlowLedger.Consolidation.Worker.Domain;
using FlowLedger.Consolidation.Worker.Infrastructure.Persistence;
using FlowLedger.Contracts.IntegrationEvents.Transactions;
using MassTransit;

namespace FlowLedger.Consolidation.Worker.Consumers;

public sealed class TransactionCreatedConsumer(
    ConsolidationDbContext dbContext)
    : IConsumer<TransactionCreatedIntegrationEvent>
{
    public async Task Consume(
        ConsumeContext<TransactionCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        dbContext.ConsolidatedTransactions.Add(new ConsolidatedTransaction
        {
            TransactionId = message.TransactionId,
            MerchantId = message.MerchantId,
            ReferenceDate = message.ReferenceDate,
            Amount = message.Amount,
            Currency = message.Currency,
            ConsolidatedAt = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
