using FlowLedger.Contracts.IntegrationEvents.Transactions;
using FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;
using FlowLedger.Transactions.Api.Infrastructure.DomainEvents;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using TransactionType = FlowLedger.Transactions.Api.Domain.Enums.TransactionType;

namespace FlowLedger.Transactions.IntegrationTests;

public sealed class TransactionCreationFlowTests : IAsyncLifetime
{
    private readonly PostgreSqlBuilder _postgresBuilder = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("transactions")
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

        services.AddScoped<DomainEventInterceptor>();

        services.AddScoped<Func<IPublishEndpoint>>(
            provider => () => provider.GetRequiredService<IPublishEndpoint>());

        services.AddDbContext<TransactionsDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));

        services.AddMassTransitTestHarness(x =>
        {
            x.SetTestTimeouts(
                testTimeout: TimeSpan.FromSeconds(30),
                testInactivityTimeout: TimeSpan.FromSeconds(3));

            x.UsingInMemory((context, cfg) => { });
        });

        _provider = services.BuildServiceProvider(true);
        _harness = await _provider.StartTestHarness();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private TransactionsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new TransactionsDbContext(options);
    }

    [Theory]
    [InlineData(TransactionType.Credit)]
    [InlineData(TransactionType.Debit)]
    public async Task HandleAsync_ValidCommand_PersistsTransactionAndPublishesIntegrationEvent(
        TransactionType transactionType)
    {
        await using var scope = _provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var handler = new CreateTransactionHandler(dbContext, NullLogger<CreateTransactionHandler>.Instance);

        var command = new CreateTransactionCommand(
            new DateOnly(2026, 9, 3), transactionType, 250m, "brl", "Payment", "user-1");

        var result = await handler.HandleAsync("merchant-1", command, CancellationToken.None);

        await using var verifyContext = CreateDbContext();
        var persisted = await verifyContext.Transactions.SingleAsync(t => t.Id == result.Id);

        Assert.Equal("merchant-1", persisted.MerchantId);
        Assert.Equal(transactionType, persisted.Type);
        Assert.Equal(250m, persisted.Amount.Amount);
        Assert.Equal("BRL", persisted.Amount.Currency);
        Assert.Equal("Payment", persisted.Description);
        Assert.Equal("user-1", persisted.CreatedBy);

        Assert.True(await _harness.Published.Any<TransactionCreatedIntegrationEvent>(
            x => x.Context.Message.TransactionId == result.Id
                && (int)x.Context.Message.TransactionType == (int)transactionType
                && x.Context.Message.Amount == 250m
                && x.Context.Message.Currency == "BRL"));
    }
}
