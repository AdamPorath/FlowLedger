using FlowLedger.Identity.Api.Domain.Entities;
using FlowLedger.Identity.Api.Infrastructure.Security;
using FlowLedger.Identity.Api.Infrastructure.Users;
using Microsoft.AspNetCore.Identity;

namespace FlowLedger.Identity.Api.Application.Commands.Login;

public sealed class LoginHandler(
    InMemoryUserStore userStore,
    IPasswordHasher<User> passwordHasher,
    JwtTokenGenerator tokenGenerator)
{
    public LoginResult? Handle(LoginCommand command)
    {
        var user = userStore.FindByUsername(command.Username);

        if (user is null)
        {
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            command.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var accessToken = tokenGenerator.GenerateToken(user);

        return new LoginResult(accessToken);
    }
}

public sealed record LoginResult(string AccessToken);
