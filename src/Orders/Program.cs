using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Typed client to Payments by name (service discovery). The resilience pipeline + chaos are
// layered onto this client in later phases.
builder.Services.AddHttpClient("payments", client => client.BaseAddress = new Uri("https+http://payments"));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/orders", async (OrderRequest request, IHttpClientFactory factory, CancellationToken ct) =>
{
    var orderId = Guid.NewGuid().ToString("N")[..8];
    var payments = factory.CreateClient("payments");

    using var response = await payments.PostAsJsonAsync(
        "/payments", new { orderId, amount = request.Amount }, ct);
    response.EnsureSuccessStatusCode();
    var payment = await response.Content.ReadFromJsonAsync<PaymentResult>(ct);

    return Results.Ok(new OrderResult(orderId, "confirmed", payment));
});

app.Run();

internal sealed record OrderRequest(decimal Amount);

internal sealed record PaymentResult(string Status, string TransactionId, decimal Amount);

internal sealed record OrderResult(string OrderId, string Status, PaymentResult? Payment);
