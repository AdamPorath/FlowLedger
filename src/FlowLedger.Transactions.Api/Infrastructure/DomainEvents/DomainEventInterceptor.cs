using FlowLedger.Transactions.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FlowLedger.Transactions.Api.Infrastructure.DomainEvents;

public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;

        if (dbContext is null)
            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);

        var transactions = dbContext.ChangeTracker
            .Entries<Transaction>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity);

        foreach (var transaction in transactions)
        {
            foreach (var domainEvent in transaction.DomainEvents)
            {
                Console.WriteLine(
                    $"Domain event: {domainEvent.GetType().Name}");
            }
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }
}