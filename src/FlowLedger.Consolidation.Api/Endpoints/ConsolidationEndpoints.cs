using System.Security.Claims;
using FlowLedger.Consolidation.Api.Application.Queries.GetDailyBalance;

namespace FlowLedger.Consolidation.Api.Endpoints;

public static class ConsolidationEndpoints
{
    public static IEndpointRouteBuilder MapConsolidationEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/consolidation")
            .WithTags("Consolidation")
            .RequireAuthorization();

        group.MapGet("/{date}", async (
            DateOnly date,
            string currency,
            ClaimsPrincipal user,
            GetDailyBalanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var merchantId = user.FindFirstValue("merchantId");

            if (merchantId is null)
            {
                return Results.Unauthorized();
            }

            var result = await handler.HandleAsync(
                merchantId,
                date,
                currency,
                cancellationToken);

            return result is not null
                ? Results.Ok(result)
                : Results.NotFound();
        })
        .WithName("GetDailyBalance")
        .WithSummary("Get the consolidated daily balance for merchant");

        return app;
    }
}
