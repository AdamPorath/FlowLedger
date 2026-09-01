using FlowLedger.Transactions.Api.Endpoints;
using FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;
using FlowLedger.Transactions.Api.Application.Queries.GetTransaction;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<TransactionsDbContext>("flowledger");

builder.Services.AddOpenApi();

builder.Services.AddScoped<CreateTransactionHandler>();
builder.Services.AddScoped<GetTransactionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<TransactionsDbContext>();

    await dbContext.Database.MigrateAsync();
}

app.MapDefaultEndpoints();
// app.UseProductionHttpsSecurity();
// app.UseHttpsRedirection();

app.MapTransactionsEndpoints();

app.Run();