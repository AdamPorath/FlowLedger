using FlowLedger.Identity.Api.Application.Commands.Login;
using FlowLedger.Identity.Api.Domain.Entities;
using FlowLedger.Identity.Api.Endpoints;
using FlowLedger.Identity.Api.Infrastructure.Security;
using FlowLedger.Identity.Api.Infrastructure.Users;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddSingleton<InMemoryUserStore>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<LoginHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    var userStore = app.Services.GetRequiredService<InMemoryUserStore>();
    var passwordHasher = app.Services.GetRequiredService<IPasswordHasher<User>>();

    var seedUser = new User
    {
        Id = Guid.NewGuid(),
        Username = "merchant-test",
        MerchantId = "merchant-test",
    };
    seedUser.PasswordHash = passwordHasher.HashPassword(seedUser, "Passw0rd!");

    userStore.Add(seedUser);
}

app.MapDefaultEndpoints();

app.MapAuthEndpoints();

app.Run();