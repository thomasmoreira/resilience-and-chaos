using System.Net.Http.Json;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Typed client to Payments by name (service discovery). We drop the ServiceDefaults standard
// resilience handler for this client and build an EXPLICIT pipeline — resilience is the point of
// this lab, so each strategy is visible and tuned. Order is outer→inner: retry wraps the circuit
// breaker (so an open circuit fails fast without retrying), and a per-attempt timeout is innermost.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental; intentional here.
builder.Services.AddHttpClient("payments", client => client.BaseAddress = new Uri("https+http://payments"))
    .RemoveAllResilienceHandlers()
#pragma warning restore EXTEXP0001
    .AddResilienceHandler("payments-pipeline", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
        });

        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(5),
        });

        pipeline.AddTimeout(TimeSpan.FromSeconds(2));
    });

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
