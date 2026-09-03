using FlowLedger.Consolidation.Api.Application.Queries.GetDailyBalance;
using FlowLedger.Consolidation.Api.Endpoints;
using FlowLedger.Consolidation.Worker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<ConsolidationDbContext>(options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("consolidation"));
    }
);

builder.Services.AddOpenApi();

builder.Services.AddScoped<GetDailyBalanceHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.MapConsolidationEndpoints();

app.Run();

