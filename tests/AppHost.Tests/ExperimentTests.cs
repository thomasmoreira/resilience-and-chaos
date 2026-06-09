using System.Net.Http.Json;
using System.Text.Json;

namespace AppHost.Tests;

/// <summary>
/// The chaos experiment, as a test: with Payments in a 100% outage, the order availability SLI must
/// stay high — the fallback holds it. This is resilience MEASURED, not asserted (ADR-005): the
/// experiment has a success criterion (the error budget survives), the same idea as the burn-rate
/// gating the observability lab.
/// </summary>
public class ExperimentTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    private const double AvailabilitySlo = 0.99;

    [Fact]
    public async Task Availability_stays_above_slo_under_total_payments_outage()
    {
        using var client = fixture.App.CreateHttpClient("orders");

        try
        {
            using var on = await client.PostAsJsonAsync("/chaos", new { fault = true, injectionRate = 1.0 });
            on.EnsureSuccessStatusCode();

            const int total = 50;
            var available = 0;
            var degraded = 0;
            for (var i = 0; i < total; i++)
            {
                using var response = await client.PostAsJsonAsync("/orders", new { amount = 5.0m });
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                available++;
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (document.RootElement.GetProperty("status").GetString() == "pending_payment")
                {
                    degraded++;
                }
            }

            var availability = (double)available / total;

            // The SLI holds despite Payments being fully down — the fallback keeps it available.
            Assert.True(availability >= AvailabilitySlo, $"availability under chaos was {availability:P0}, SLO is {AvailabilitySlo:P0}");
            // ...and it really was degraded (proving chaos was active, not silently healthy).
            Assert.True(degraded >= total - 1, $"expected requests degraded under chaos, got {degraded}/{total}");
        }
        finally
        {
            using var off = await client.PostAsJsonAsync("/chaos", new { fault = false });
            off.EnsureSuccessStatusCode();
        }
    }
}
