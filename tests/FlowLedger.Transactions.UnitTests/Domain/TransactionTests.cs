using FlowLedger.Transactions.Api.Domain.Entities;
using FlowLedger.Transactions.Api.Domain.Enums;
using FlowLedger.Transactions.Api.Domain.Events;
using FlowLedger.Transactions.Api.Domain.ValueObjects;

namespace FlowLedger.Transactions.UnitTests.Domain;

public sealed class TransactionTests
{
    private static readonly DateOnly ReferenceDate = new(2026, 9, 3);

    [Theory]
    [InlineData(TransactionType.Credit)]
    [InlineData(TransactionType.Debit)]
    public void Create_WithValidData_SetsPropertiesAndRaisesTransactionCreatedEvent(
        TransactionType type)
    {
        var amount = Money.Create(150m, "brl");

        var transaction = Transaction.Create(
            "merchant-1",
            ReferenceDate,
            type,
            amount,
            "Payment",
            "user-1");

        Assert.NotEqual(Guid.Empty, transaction.Id);
        Assert.Equal("merchant-1", transaction.MerchantId);
        Assert.Equal(ReferenceDate, transaction.ReferenceDate);
        Assert.Equal(type, transaction.Type);
        Assert.Equal(amount, transaction.Amount);
        Assert.Equal("Payment", transaction.Description);
        Assert.Equal("user-1", transaction.CreatedBy);

        var domainEvent = Assert.Single(transaction.DomainEvents);
        var transactionCreated = Assert.IsType<TransactionCreated>(domainEvent);

        Assert.Equal(transaction.Id, transactionCreated.TransactionId);
        Assert.Equal("merchant-1", transactionCreated.MerchantId);
        Assert.Equal(ReferenceDate, transactionCreated.ReferenceDate);
        Assert.Equal(type, transactionCreated.Type);
        Assert.Equal(150m, transactionCreated.Amount);
        Assert.Equal("BRL", transactionCreated.Currency);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WhenMerchantIdIsMissing_Throws(string? merchantId)
    {
        var amount = Money.Create(100m, "BRL");

        var exception = Assert.Throws<ArgumentException>(() => Transaction.Create(
            merchantId!,
            ReferenceDate,
            TransactionType.Credit,
            amount,
            "Payment",
            "user-1"));

        Assert.Equal("merchantId", exception.ParamName);
    }

    [Fact]
    public void Create_WhenTypeIsUndefined_Throws()
    {
        var amount = Money.Create(100m, "BRL");

        var exception = Assert.Throws<ArgumentException>(() => Transaction.Create(
            "merchant-1",
            ReferenceDate,
            (TransactionType)99,
            amount,
            "Payment",
            "user-1"));

        Assert.Equal("type", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WhenDescriptionIsMissing_Throws(string? description)
    {
        var amount = Money.Create(100m, "BRL");

        var exception = Assert.Throws<ArgumentException>(() => Transaction.Create(
            "merchant-1",
            ReferenceDate,
            TransactionType.Credit,
            amount,
            description!,
            "user-1"));

        Assert.Equal("description", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WhenCreatedByIsMissing_Throws(string? createdBy)
    {
        var amount = Money.Create(100m, "BRL");

        var exception = Assert.Throws<ArgumentException>(() => Transaction.Create(
            "merchant-1",
            ReferenceDate,
            TransactionType.Credit,
            amount,
            "Payment",
            createdBy!));

        Assert.Equal("createdBy", exception.ParamName);
    }

    [Fact]
    public void ClearDomainEvents_RemovesRaisedEvents()
    {
        var amount = Money.Create(100m, "BRL");
        var transaction = Transaction.Create(
            "merchant-1",
            ReferenceDate,
            TransactionType.Credit,
            amount,
            "Payment",
            "user-1");

        transaction.ClearDomainEvents();

        Assert.Empty(transaction.DomainEvents);
    }
}
