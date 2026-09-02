using FlowLedger.Consolidation.Worker.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Consolidation.Worker.Infrastructure.Persistence;

public sealed class ConsolidationDbContext(
    DbContextOptions<ConsolidationDbContext> options)
    : DbContext(options)
{
    public DbSet<ConsolidatedTransaction> ConsolidatedTransactions =>
        Set<ConsolidatedTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsolidatedTransaction>(builder =>
        {
            builder.ToTable("consolidated_transactions");

            builder.HasKey(x => x.TransactionId);

            builder.Property(x => x.MerchantId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
