using FlowLedger.Transactions.Api.Endpoints;
using FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<TransactionsDbContext>("flowledger");

builder.Services.AddOpenApi();

builder.Services.AddScoped<CreateTransactionHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

// app.UseHttpsRedirection();

app.MapTransactionsEndpoints();

app.Run();