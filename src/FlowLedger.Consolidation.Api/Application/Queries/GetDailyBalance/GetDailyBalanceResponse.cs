namespace FlowLedger.Consolidation.Api.Application.Queries.GetDailyBalance;

public sealed record GetDailyBalanceResponse(
    DateOnly Date,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal Balance);
