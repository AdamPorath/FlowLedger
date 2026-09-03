using FlowLedger.Transactions.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowLedger.Transactions.Api.Application.Queries.GetTransaction;

public sealed class GetTransactionHandler(
    TransactionsDbContext dbContext)
{
    public async Task<GetTransactionResponse?> HandleAsync(
        Guid id,
        string merchantId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.Id == id && x.MerchantId == merchantId)
            .Select(x => new GetTransactionResponse(
                x.Id,
                x.MerchantId,
                x.ReferenceDate,
                x.Type,
                x.Amount.Amount,
                x.Amount.Currency,
                x.Description,
                x.CreatedBy,
                x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}