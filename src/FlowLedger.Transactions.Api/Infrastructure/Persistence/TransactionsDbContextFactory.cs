using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FlowLedger.Transactions.Api.Infrastructure.Persistence;

public sealed class TransactionsDbContextFactory
    : IDesignTimeDbContextFactory<TransactionsDbContext>
{
    public TransactionsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TransactionsDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("flowledger");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'flowledger' was not configured.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<TransactionsDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new TransactionsDbContext(optionsBuilder.Options);
    }
}