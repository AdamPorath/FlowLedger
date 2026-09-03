using FlowLedger.Consolidation.Worker.Consumers;
using FlowLedger.Consolidation.Worker.Infrastructure.Persistence;
using FlowLedger.Contracts.IntegrationEvents.Transactions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FlowLedger.Consolidation.IntegrationTests;

public sealed class TransactionCreatedFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlBuilder _postgresBuilder = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("consolidation")
        .WithUsername("postgres")
        .WithPassword("postgres");

    private PostgreSqlContainer _postgres = null!;
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _postgres = _postgresBuilder.Build();
        await _postgres.StartAsync();

        await using (var migrationContext = CreateDbContext())
        {
            await migrationContext.Database.MigrateAsync();
        }

        var services = new ServiceCollection();

        services.AddDbContext<ConsolidationDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));

        services.AddMassTransitTestHarness(x =>
        {
            x.SetTestTimeouts(
                testTimeout: TimeSpan.FromSeconds(30),
                testInactivityTimeout: TimeSpan.FromSeconds(3));

            x.AddEntityFrameworkOutbox<ConsolidationDbContext>(o => o.UsePostgres());

            x.AddConsumer<TransactionCreatedConsumer>();

            x.UsingInMemory((context, cfg) =>
            {
                cfg.ReceiveEndpoint("consolidation-transaction-created", e =>
                {
                    e.UseMessageRetry(r => r.Immediate(3));
                    e.UseEntityFrameworkOutbox<ConsolidationDbContext>(context);
                    e.ConfigureConsumer<TransactionCreatedConsumer>(context);
                });
            });
        });

        _provider = services.BuildServiceProvider(true);
        _harness = await _provider.StartTestHarness();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private ConsolidationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ConsolidationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new ConsolidationDbContext(options);
    }

    private async Task PublishAsync(TransactionCreatedIntegrationEvent integrationEvent) =>
        await _harness.Bus.Publish(
            integrationEvent,
            integrationEvent.GetType(),
            context => context.MessageId = integrationEvent.EventId);

    [Fact]
    public async Task Publish_TransactionCreated_PersistsConsolidatedBalance()
    {
        var integrationEvent = new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", new DateOnly(2026, 9, 3), TransactionType.Credit, 200m, "BRL");

        await PublishAsync(integrationEvent);

        Assert.True(await _harness.Consumed.Any<TransactionCreatedIntegrationEvent>(
            x => x.Context.Message.TransactionId == integrationEvent.TransactionId));

        await using var dbContext = CreateDbContext();
        var balance = await dbContext.ConsolidatedBalances.SingleAsync(
            b => b.MerchantId == "merchant-1" && b.Currency == "BRL");

        Assert.Equal(200m, balance.TotalCredits);
        Assert.Equal(0m, balance.TotalDebits);
    }

    [Fact]
    public async Task Publish_SameEventTwice_InboxDeduplicatesAndBalanceIsNotDoubled()
    {
        var integrationEvent = new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-2", new DateOnly(2026, 9, 3), TransactionType.Credit, 300m, "BRL");

        await PublishAsync(integrationEvent);

        Assert.True(await _harness.Consumed.Any<TransactionCreatedIntegrationEvent>(
            x => x.Context.Message.TransactionId == integrationEvent.TransactionId));

        await PublishAsync(integrationEvent);
        await Task.Delay(TimeSpan.FromSeconds(1));

        await using var dbContext = CreateDbContext();
        var balance = await dbContext.ConsolidatedBalances.SingleAsync(
            b => b.MerchantId == "merchant-2" && b.Currency == "BRL");

        Assert.Equal(300m, balance.TotalCredits);
    }
}
