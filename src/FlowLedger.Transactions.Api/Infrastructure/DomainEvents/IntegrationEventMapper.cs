using FlowLedger.Contracts.IntegrationEvents;
using FlowLedger.Contracts.IntegrationEvents.Transactions;
using FlowLedger.Transactions.Api.Domain.Events;

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
                e.Amount,
                e.Currency),
            _ => null
        };
}
