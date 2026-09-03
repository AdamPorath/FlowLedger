using FlowLedger.Contracts.IntegrationEvents;
using FlowLedger.Contracts.IntegrationEvents.Transactions;
using FlowLedger.Transactions.Api.Domain.Events;
using DomainTransactionType = FlowLedger.Transactions.Api.Domain.Enums.TransactionType;
using ContractsTransactionType = FlowLedger.Contracts.IntegrationEvents.Transactions.TransactionType;

namespace FlowLedger.Transactions.Api.Infrastructure.DomainEvents;

public static class IntegrationEventMapper
{
    public static IIntegrationEvent? Map(IDomainEvent domainEvent) =>
        domainEvent switch
        {
            TransactionCreated e => new TransactionCreatedIntegrationEvent(
                e.TransactionId,
                e.MerchantId,
                e.ReferenceDate,
                MapTransactionType(e.Type),
                e.Amount,
                e.Currency),
            _ => null
        };

    private static ContractsTransactionType MapTransactionType(DomainTransactionType type) =>
        type switch
        {
            DomainTransactionType.Credit => ContractsTransactionType.Credit,
            DomainTransactionType.Debit => ContractsTransactionType.Debit,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown transaction type.")
        };
}
