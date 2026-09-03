using FlowLedger.Consolidation.Api.Application.Queries.GetDailyBalance;

namespace FlowLedger.Consolidation.Api.Endpoints;

public static class ConsolidationEndpoints
{
    public static IEndpointRouteBuilder MapConsolidationEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/consolidation")
            .WithTags("Consolidation");

        group.MapGet("/{date}", async (
            DateOnly date,
            string merchantId,
            string currency,
            GetDailyBalanceHandler handler,
            CancellationToken cancellationToken) =>
        {
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
        .WithSummary("Get the consolidated daily balance for a merchant");

        return app;
    }
}
