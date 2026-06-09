using System.Net.Http.Json;
using System.Text.Json;

namespace AppHost.Tests;

/// <summary>Baseline (happy path): an order confirms by calling Payments through the live app.</summary>
public class OrdersTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    [Fact]
    public async Task Order_confirms_via_payments()
    {
        using var client = fixture.App.CreateHttpClient("orders");

        using var response = await client.PostAsJsonAsync("/orders", new { amount = 42.0m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("confirmed", root.GetProperty("status").GetString());
        Assert.Equal("charged", root.GetProperty("payment").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Chaos_fault_injection_fails_the_order_then_recovers_when_disabled()
    {
        using var client = fixture.App.CreateHttpClient("orders");

        try
        {
            // Inject 100% faults into the Payments call at runtime.
            using var on = await client.PostAsJsonAsync("/chaos", new { fault = true, injectionRate = 1.0 });
            on.EnsureSuccessStatusCode();

            // Every attempt (incl. retries) hits an injected 500 → the order fails.
            using var failing = await client.PostAsJsonAsync("/orders", new { amount = 10.0m });
            Assert.Equal(HttpStatusCode.InternalServerError, failing.StatusCode);
        }
        finally
        {
            using var off = await client.PostAsJsonAsync("/chaos", new { fault = false });
            off.EnsureSuccessStatusCode();
        }

        // With chaos disabled, orders confirm again — the toggle is live.
        using var recovered = await client.PostAsJsonAsync("/orders", new { amount = 10.0m });
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
    }
}
