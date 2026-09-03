using FlowLedger.Consolidation.Worker.Consumers;
using FlowLedger.Consolidation.Worker.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<ConsolidationDbContext>(options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("consolidation"));
    }
);

var rabbitMqConnectionString =
    builder.Configuration.GetConnectionString("messaging")
    ?? throw new InvalidOperationException(
        "Connection string 'messaging' was not configured.");

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<ConsolidationDbContext>(o =>
    {
        o.UsePostgres();
    });

    x.AddConsumer<TransactionCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri(rabbitMqConnectionString));

        cfg.ReceiveEndpoint("consolidation-transaction-created", e =>
        {
            e.UseMessageRetry(r => r.Exponential(
                retryLimit: 5,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(30),
                intervalDelta: TimeSpan.FromSeconds(5)));

            e.UseEntityFrameworkOutbox<ConsolidationDbContext>(context);

            e.ConfigureConsumer<TransactionCreatedConsumer>(context);
        });
    });
});

var host = builder.Build();

if (builder.Environment.IsDevelopment())
{
    using var scope = host.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<ConsolidationDbContext>();

    await dbContext.Database.MigrateAsync();
}

host.Run();
