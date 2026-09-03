using FlowLedger.Consolidation.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Consolidation.Api.Application.Queries.GetDailyBalance;

public sealed class GetDailyBalanceHandler(
    ConsolidationDbContext dbContext)
{
    public async Task<GetDailyBalanceResponse?> HandleAsync(
        string merchantId,
        DateOnly date,
        string currency,
        CancellationToken cancellationToken)
    {
        return await dbContext.ConsolidatedBalances
            .AsNoTracking()
            .Where(x =>
                x.MerchantId == merchantId &&
                x.ReferenceDate == date &&
                x.Currency == currency)
            .Select(x => new GetDailyBalanceResponse(
                x.ReferenceDate,
                x.TotalCredits,
                x.TotalDebits,
                x.TotalCredits - x.TotalDebits))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
