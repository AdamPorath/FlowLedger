using FlowLedger.Transactions.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Transactions.Api.Infrastructure.Persistence;

public sealed class TransactionsDbContext : DbContext
{
    public TransactionsDbContext(
        DbContextOptions<TransactionsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TransactionsDbContext).Assembly);
    }
}