using FlowLedger.Consolidation.Worker.Domain;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Consolidation.Worker.Infrastructure.Persistence;

public sealed class ConsolidationDbContext(
    DbContextOptions<ConsolidationDbContext> options)
    : DbContext(options)
{
    public DbSet<ConsolidatedBalance> ConsolidatedBalances =>
        Set<ConsolidatedBalance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConsolidatedBalance>(builder =>
        {
            builder.ToTable("consolidated_balances");

            builder.HasKey(x => new { x.MerchantId, x.ReferenceDate, x.Currency });

            builder.Property(x => x.MerchantId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            builder.Ignore(x => x.Balance);
        });
    }
}
