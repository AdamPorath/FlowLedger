using FlowLedger.Transactions.Api.Domain.Enums;
using FlowLedger.Transactions.Api.Domain.ValueObjects;
using FlowLedger.Transactions.Api.Domain.Events;

namespace FlowLedger.Transactions.Api.Domain.Entities;

public sealed class Transaction
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public Guid Id { get; private set; }
    public string MerchantId { get; private set; } = null!;
    public DateOnly ReferenceDate { get; private set; }
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Transaction()
    {
    }

    private Transaction(
        string merchantId,
        DateOnly referenceDate,
        TransactionType type,
        Money amount,
        string description,
        string createdBy)
    {
        Id = Guid.NewGuid();
        MerchantId = merchantId;
        ReferenceDate = referenceDate;
        Type = type;
        Amount = amount;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(
            new TransactionCreated(
                Id,
                MerchantId,
                ReferenceDate,
                Amount.Amount,
                Amount.Currency
            )
        );
    }

    public static Transaction Create(
        string merchantId,
        DateOnly referenceDate,
        TransactionType type,
        Money amount,
        string description,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            throw new ArgumentException(
            "MerchantId is required.",
            nameof(merchantId));
        }
        

        if (!Enum.IsDefined(typeof(TransactionType), type))
        {
            throw new ArgumentException(
                "Invalid transaction type.",
                nameof(type));
        }

        if (amount.Amount <= 0)
        {
            throw new ArgumentException(
                "Amount must be greater than zero.",
                nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(amount.Currency))
        {
            throw new ArgumentException(
                "Currency is required.",
                nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Description is required.",
                nameof(description));
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException(
                "CreatedBy is required.",
                nameof(createdBy));
        }

        return new Transaction(
            merchantId,
            referenceDate,
            type,
            amount,
            description,
            createdBy
        );
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    private void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}