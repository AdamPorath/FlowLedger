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
    }
}
