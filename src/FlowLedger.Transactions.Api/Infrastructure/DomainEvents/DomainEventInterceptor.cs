using System.Text.Json;
using FlowLedger.Transactions.Api.Domain.Entities;
using FlowLedger.Transactions.Api.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;

namespace FlowLedger.Transactions.Api.Infrastructure.DomainEvents;

public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context as TransactionsDbContext;

        if (dbContext is null)
        {
            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        var transactions = dbContext.ChangeTracker
            .Entries<Transaction>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        foreach (var transaction in transactions)
        {
            foreach (var domainEvent in transaction.DomainEvents)
            {
                var integrationEvent = IntegrationEventMapper.Map(domainEvent);

                if (integrationEvent is null)
                {
                    continue;
                }

                var outboxMessage = new OutboxMessage(
                    integrationEvent.GetType().FullName!,
                    JsonSerializer.Serialize(
                        integrationEvent,
                        integrationEvent.GetType()),
                    integrationEvent.OccurredOn);

                dbContext.OutboxMessages.Add(outboxMessage);
            }

            transaction.ClearDomainEvents();
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}