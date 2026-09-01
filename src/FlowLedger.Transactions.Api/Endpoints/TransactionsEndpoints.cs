using FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;
using FlowLedger.Transactions.Api.Application.Queries.GetTransaction;

namespace FlowLedger.Transactions.Api.Endpoints;

public static class TransactionsEndpoints
{
    public static IEndpointRouteBuilder MapTransactionsEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/transactions")
            .WithTags("Transactions");

        group.MapPost("/", async (
            CreateTransactionCommand command,
            CreateTransactionHandler handler,
            CancellationToken cancellationToken) =>
        {
            // Its just for test purposes
            const string merchantId = "merchant-test";

            var result = await handler.HandleAsync(
                merchantId,
                command,
                cancellationToken);

            return Results.Created(
                $"/api/v1/transactions/{result.Id}",
                result);
        })
        .WithName("CreateTransaction")
        .WithSummary("Create a financial transaction by a merchant");

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetTransactionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                id,
                cancellationToken);

            return result is not null
                ? Results.Ok(result)
                : Results.NotFound();
        })
        .WithName("GetTransaction")
        .WithSummary("Get a financial transaction");

        return app;
    }
}