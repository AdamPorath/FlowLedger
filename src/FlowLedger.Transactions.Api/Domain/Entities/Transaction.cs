using FlowLedger.Transactions.Api.Domain.Enums;
using FlowLedger.Transactions.Api.Domain.ValueObjects;

namespace FlowLedger.Transactions.Api.Domain.Entities;

public sealed class Transaction
{
    private Transaction(
        Guid id,
        string merchantId,
        DateOnly referenceDate,
        TransactionType type,
        Money money,
        string description,
        string createdBy,
        DateTimeOffset createdAt)
    {
        Id = id;
        MerchantId = merchantId;
        ReferenceDate = referenceDate;
        Type = type;
        Money = money;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string MerchantId { get; }
    public DateOnly ReferenceDate { get; }
    public TransactionType Type { get; }
    public Money Money { get; }
    public string Description { get; }
    public string CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }

    public static Transaction Create(
        string merchantId,
        DateOnly referenceDate,
        TransactionType type,
        Money money,
        string description,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException(
                "MerchantId is a required field.",
                nameof(merchantId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "Description is a required field.",
                nameof(description));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException(
                "CreatedBy is a required field.",
                nameof(createdBy));

        return new Transaction(
            Guid.NewGuid(),
            merchantId,
            referenceDate,
            type,
            money,
            description,
            createdBy,
            DateTimeOffset.UtcNow);
    }
}