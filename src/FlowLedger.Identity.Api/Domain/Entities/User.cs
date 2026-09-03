namespace FlowLedger.Identity.Api.Domain.Entities;

public sealed class User
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public required string MerchantId { get; init; }

    public string PasswordHash { get; set; } = string.Empty;
}
