using System.Text;
using FlowLedger.Transactions.Api.Endpoints;
using FlowLedger.Transactions.Api.Application.Commands.CreateTransaction;
using FlowLedger.Transactions.Api.Application.Queries.GetTransaction;
using FlowLedger.Transactions.Api.Infrastructure.Persistence;
using FlowLedger.Transactions.Api.Infrastructure.DomainEvents;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var jwtSection = builder.Configuration.GetSection("Jwt");

var jwtIssuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer was not configured.");

var jwtAudience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience was not configured.");

var jwtSigningKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey was not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<DomainEventInterceptor>();

builder.Services.AddScoped<Func<IPublishEndpoint>>(
    provider => () => provider.GetRequiredService<IPublishEndpoint>());

builder.Services.AddDbContext<TransactionsDbContext>(options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("flowledger"));
    }
);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TransactionsDbContext>("database", tags: ["ready"]);

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

app.UseAuthentication();
app.UseAuthorization();

app.MapTransactionsEndpoints();

app.Run();