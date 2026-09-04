using FlowLedger.Consolidation.Worker.Consumers;
using FlowLedger.Consolidation.Worker.Infrastructure.Persistence;
using FlowLedger.Contracts.IntegrationEvents.Transactions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FlowLedger.Consolidation.UnitTests.Consumers;

public sealed class TransactionCreatedConsumerTests
{
    private static ConsolidationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ConsolidationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ConsolidationDbContext(options);
    }

    private static ConsumeContext<TransactionCreatedIntegrationEvent> CreateContext(
        TransactionCreatedIntegrationEvent message)
    {
        var context = Substitute.For<ConsumeContext<TransactionCreatedIntegrationEvent>>();

        context.Message.Returns(message);
        context.CancellationToken.Returns(CancellationToken.None);

        return context;
    }

    [Theory]
    [InlineData(TransactionType.Credit)]
    [InlineData(TransactionType.Debit)]
    public async Task Consume_NewMerchant_CreatesBalanceOnTheCorrectSide(
        TransactionType transactionType)
    {
        await using var dbContext = CreateDbContext();
        var consumer = new TransactionCreatedConsumer(dbContext, NullLogger<TransactionCreatedConsumer>.Instance);

        var message = new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", new DateOnly(2026, 9, 3), transactionType, 150m, "BRL");

        await consumer.Consume(CreateContext(message));

        var balance = await dbContext.ConsolidatedBalances.SingleAsync();

        Assert.Equal("merchant-1", balance.MerchantId);
        Assert.Equal(transactionType == TransactionType.Credit ? 150m : 0m, balance.TotalCredits);
        Assert.Equal(transactionType == TransactionType.Debit ? 150m : 0m, balance.TotalDebits);
    }

    [Fact]
    public async Task Consume_MultipleTransactionsForSameMerchant_AccumulatesOnSameBalance()
    {
        await using var dbContext = CreateDbContext();
        var consumer = new TransactionCreatedConsumer(dbContext, NullLogger<TransactionCreatedConsumer>.Instance);
        var referenceDate = new DateOnly(2026, 9, 3);

        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", referenceDate, TransactionType.Credit, 100m, "BRL")));
        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", referenceDate, TransactionType.Debit, 40m, "BRL")));
        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", referenceDate, TransactionType.Credit, 25m, "BRL")));

        var balance = await dbContext.ConsolidatedBalances.SingleAsync();

        Assert.Equal(125m, balance.TotalCredits);
        Assert.Equal(40m, balance.TotalDebits);
        Assert.Equal(85m, balance.Balance);
    }

    [Fact]
    public async Task Consume_DifferentMerchantCurrencyOrDate_CreatesSeparateBalances()
    {
        await using var dbContext = CreateDbContext();
        var consumer = new TransactionCreatedConsumer(dbContext, NullLogger<TransactionCreatedConsumer>.Instance);

        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", new DateOnly(2026, 9, 3), TransactionType.Credit, 100m, "BRL")));
        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", new DateOnly(2026, 9, 3), TransactionType.Credit, 50m, "USD")));
        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", new DateOnly(2026, 9, 4), TransactionType.Credit, 75m, "BRL")));
        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-2", new DateOnly(2026, 9, 3), TransactionType.Credit, 10m, "BRL")));

        var balances = await dbContext.ConsolidatedBalances.ToListAsync();

        Assert.Equal(4, balances.Count);
    }

    [Fact]
    public async Task Consume_ExistingBalance_UpdatesUpdatedAtTimestamp()
    {
        await using var dbContext = CreateDbContext();
        var consumer = new TransactionCreatedConsumer(dbContext, NullLogger<TransactionCreatedConsumer>.Instance);
        var referenceDate = new DateOnly(2026, 9, 3);

        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", referenceDate, TransactionType.Credit, 10m, "BRL")));

        var firstUpdatedAt = (await dbContext.ConsolidatedBalances.SingleAsync()).UpdatedAt;

        await consumer.Consume(CreateContext(new TransactionCreatedIntegrationEvent(
            Guid.NewGuid(), "merchant-1", referenceDate, TransactionType.Credit, 5m, "BRL")));

        var secondUpdatedAt = (await dbContext.ConsolidatedBalances.SingleAsync()).UpdatedAt;

        Assert.True(secondUpdatedAt >= firstUpdatedAt);
    }
}
