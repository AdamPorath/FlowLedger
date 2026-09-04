using System.Text;
using FlowLedger.Consolidation.Api.Application.Queries.GetDailyBalance;
using FlowLedger.Consolidation.Api.Endpoints;
using FlowLedger.Consolidation.Worker.Infrastructure.Persistence;
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

builder.Services.AddDbContext<ConsolidationDbContext>(options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("consolidation"));
    }
);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ConsolidationDbContext>("database", tags: ["ready"]);

builder.Services.AddOpenApi();

builder.Services.AddScoped<GetDailyBalanceHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapConsolidationEndpoints();

app.Run();

