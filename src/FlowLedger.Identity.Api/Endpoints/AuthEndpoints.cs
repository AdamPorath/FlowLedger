using FlowLedger.Identity.Api.Application.Commands.Login;

namespace FlowLedger.Identity.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/login", (
            LoginCommand command,
            LoginHandler handler) =>
        {
            var result = handler.Handle(command);

            return result is not null
                ? Results.Ok(result)
                : Results.Unauthorized();
        })
        .WithName("Login")
        .WithSummary("Authenticate a user and issue a JWT");

        return app;
    }
}
