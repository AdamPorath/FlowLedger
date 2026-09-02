namespace FlowLedger.Transactions.Api.Infrastructure.Outbox;
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public string? Error { get; private set; }

    private OutboxMessage()
    {
    }

    public OutboxMessage(string type, string content, DateTimeOffset occurredOn)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
    }

    public void MarkAsProcessed(DateTimeOffset processedOn)
    {
        ProcessedOn = processedOn;
        Error = null;
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
    }
}
