using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FlowLedger.Consolidation.Worker.Infrastructure.Persistence;

public sealed class ConsolidationDbContextFactory
    : IDesignTimeDbContextFactory<ConsolidationDbContext>
{
    public ConsolidationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ConsolidationDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("consolidation");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'consolidation' was not configured.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<ConsolidationDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new ConsolidationDbContext(optionsBuilder.Options);
    }
}
