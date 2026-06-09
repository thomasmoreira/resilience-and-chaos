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
    public async Task Chaos_degrades_gracefully_then_recovers_when_disabled()
    {
        using var client = fixture.App.CreateHttpClient("orders");

        try
        {
            // Inject 100% faults into the Payments call at runtime.
            using var on = await client.PostAsJsonAsync("/chaos", new { fault = true, injectionRate = 1.0 });
            on.EnsureSuccessStatusCode();

            // The order still succeeds (HTTP 200) but DEGRADED: payment pending, not 5xx. The
            // fallback preserves availability — this is the killer detail (ADR-003).
            using var degraded = await client.PostAsJsonAsync("/orders", new { amount = 10.0m });
            Assert.Equal(HttpStatusCode.OK, degraded.StatusCode);
            using var degradedDoc = JsonDocument.Parse(await degraded.Content.ReadAsStringAsync());
            Assert.Equal("pending_payment", degradedDoc.RootElement.GetProperty("status").GetString());
        }
        finally
        {
            using var off = await client.PostAsJsonAsync("/chaos", new { fault = false });
            off.EnsureSuccessStatusCode();
        }

        // With chaos disabled, orders confirm again — the toggle is live.
        using var recovered = await client.PostAsJsonAsync("/orders", new { amount = 10.0m });
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        using var recoveredDoc = JsonDocument.Parse(await recovered.Content.ReadAsStringAsync());
        Assert.Equal("confirmed", recoveredDoc.RootElement.GetProperty("status").GetString());
    }
}
