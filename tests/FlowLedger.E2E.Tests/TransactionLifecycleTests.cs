using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowLedger.E2E.Tests;

public sealed class TransactionLifecycleTests : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private ResourceNotificationService _notifications = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.FlowLedger_AppHost>();

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            });
        });

        _app = await appHost.BuildAsync();
        _notifications = _app.Services.GetRequiredService<ResourceNotificationService>();

        await _app.StartAsync();

        await _notifications.WaitForResourceAsync(
            "gateway",
            KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(3));

        await _notifications.WaitForResourceAsync(
            "transactions-api",
            KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(3));

        await _notifications.WaitForResourceAsync(
            "identity-api",
            KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(3));
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task CreateTransaction_IsConsolidatedIntoDailyBalance()
    {
        using var client = _app.CreateHttpClient("gateway");

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { Username = "merchant-test", Password = "Passw0rd!" });

        if (!loginResponse.IsSuccessStatusCode)
        {
            var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Login failed. StatusCode: {loginResponse.StatusCode}, Body: {loginResponseBody}");
        }

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login!.AccessToken);

        var referenceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        const string currency = "BRL";
        const decimal amount = 150.75m;

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/transactions",
            new
            {
                ReferenceDate = referenceDate,
                Type = 1,
                Amount = amount,
                Currency = currency,
                Description = "e2e-test-transaction",
                CreatedBy = "e2e-test",
            });
        if (!createResponse.IsSuccessStatusCode)
        {
            var createResponseBody = await createResponse.Content.ReadAsStringAsync();
            Assert.Fail($"StatusCode: {createResponse.StatusCode}, Body: {createResponseBody}");
        }

        var deadline = DateTime.UtcNow.AddSeconds(30);
        DailyBalanceResponse? balance = null;

        while (DateTime.UtcNow < deadline)
        {
            var balanceResponse = await client.GetAsync(
                $"/api/v1/consolidation/{referenceDate:yyyy-MM-dd}?currency={currency}");

            if (balanceResponse.IsSuccessStatusCode)
            {
                balance = await balanceResponse.Content.ReadFromJsonAsync<DailyBalanceResponse>();

                if (balance is not null && balance.TotalCredits >= amount)
                {
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.NotNull(balance);
        Assert.True(balance!.TotalCredits >= amount);
    }

    private sealed record LoginResponse(string AccessToken);

    private sealed record DailyBalanceResponse(
        DateOnly ReferenceDate,
        decimal TotalCredits,
        decimal TotalDebits,
        decimal NetBalance);
}
