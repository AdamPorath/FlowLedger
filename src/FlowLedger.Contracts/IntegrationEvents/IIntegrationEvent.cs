namespace FlowLedger.Contracts.IntegrationEvents;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredOn { get; }
}
