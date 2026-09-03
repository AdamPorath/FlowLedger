using FlowLedger.Identity.Api.Domain.Entities;

namespace FlowLedger.Identity.Api.Infrastructure.Users;

public sealed class InMemoryUserStore
{
    private readonly Dictionary<string, User> _usersByUsername =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(User user) => _usersByUsername[user.Username] = user;

    public User? FindByUsername(string username) =>
        _usersByUsername.GetValueOrDefault(username);
}
