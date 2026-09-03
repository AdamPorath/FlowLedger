using FlowLedger.Transactions.Api.Domain.Entities;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FlowLedger.Transactions.Api.Infrastructure.DomainEvents;

public sealed class DomainEventInterceptor(
    Func<IPublishEndpoint> publishEndpointAccessor) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context as TransactionsDbContext;

        if (dbContext is null)
        {
            return await base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }

        var transactions = dbContext.ChangeTracker
            .Entries<Transaction>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        if (transactions.Count > 0)
        {
            var publishEndpoint = publishEndpointAccessor();

            foreach (var transaction in transactions)
            {
                foreach (var domainEvent in transaction.DomainEvents)
                {
                    var integrationEvent = IntegrationEventMapper.Map(domainEvent);

                    if (integrationEvent is null)
                    {
                        continue;
                    }

                    await publishEndpoint.Publish(
                        integrationEvent,
                        integrationEvent.GetType(),
                        context => context.MessageId = integrationEvent.EventId,
                        cancellationToken);
                }

                transaction.ClearDomainEvents();
            }
        }

        return await base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}