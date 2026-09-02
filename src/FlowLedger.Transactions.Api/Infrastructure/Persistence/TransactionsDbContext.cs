using FlowLedger.Transactions.Api.Domain.Entities;
using FlowLedger.Transactions.Api.Infrastructure.DomainEvents;
using FlowLedger.Transactions.Api.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Transactions.Api.Infrastructure.Persistence;

public sealed class TransactionsDbContext(
    DbContextOptions<TransactionsDbContext> options,
    DomainEventInterceptor? domainEventInterceptor = null)
    : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (domainEventInterceptor is not null)
        {
            optionsBuilder.AddInterceptors(domainEventInterceptor);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TransactionsDbContext).Assembly);
    }
}