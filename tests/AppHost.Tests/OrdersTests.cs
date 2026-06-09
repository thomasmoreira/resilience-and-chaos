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
}
