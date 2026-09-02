using FlowLedger.Transactions.Api.Endpoints;
using FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;
using FlowLedger.Transactions.Api.Application.Queries.GetTransaction;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;
using FlowLedger.Transactions.Api.Infrastructure.DomainEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddScoped<DomainEventInterceptor>();

builder.Services.AddScoped<Func<IPublishEndpoint>>(
    provider => () => provider.GetRequiredService<IPublishEndpoint>());

builder.Services.AddDbContext<TransactionsDbContext>(options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("flowledger"));
    }
);

var rabbitMqConnectionString =
    builder.Configuration.GetConnectionString("messaging")
    ?? throw new InvalidOperationException(
        "Connection string 'messaging' was not configured.");

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<TransactionsDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitMqConnectionString));

        cfg.ConfigureEndpoints(context);
    });
});

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

// app.UseHttpsRedirection();

app.MapTransactionsEndpoints();

app.Run();