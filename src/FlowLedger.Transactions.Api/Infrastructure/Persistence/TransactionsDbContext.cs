using FlowLedger.Transactions.Api.Domain.Entities;
using FlowLedger.Transactions.Api.Infrastructure.DomainEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FlowLedger.Transactions.Api.Infrastructure.Persistence;

public sealed class TransactionsDbContext(
    DbContextOptions<TransactionsDbContext> options,
    DomainEventInterceptor? domainEventInterceptor = null)
    : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (domainEventInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(domainEventInterceptor);
        }

       // MassTransit's Outbox model is enriched at runtime via AddEntityFrameworkOutbox, which design-time tooling does not reproduce identically, causing a false positive in EF Core model validation; ignoring it is the recommended approach for this scenario.
        optionsBuilder.ConfigureWarnings(
            w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TransactionsDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}