using FlowLedger.Identity.Api.Domain.Entities;
using FlowLedger.Identity.Api.Infrastructure.Security;
using FlowLedger.Identity.Api.Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FlowLedger.Identity.Api.Application.Commands.Login;

public sealed class LoginHandler(
    InMemoryUserStore userStore,
    IPasswordHasher<User> passwordHasher,
    JwtTokenGenerator tokenGenerator,
    ILogger<LoginHandler> logger)
{
    public LoginResult? Handle(LoginCommand command)
    {
        var user = userStore.FindByUsername(command.Username);

        if (user is null)
        {
            logger.LogWarning(
                "Login failed for username {Username}: user not found",
                command.Username);

            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            command.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            logger.LogWarning(
                "Login failed for username {Username}: invalid password",
                command.Username);

            return null;
        }

        var accessToken = tokenGenerator.GenerateToken(user);

        logger.LogInformation(
            "User {Username} logged in successfully",
            command.Username);

        return new LoginResult(accessToken);
    }
}

public sealed record LoginResult(string AccessToken);
