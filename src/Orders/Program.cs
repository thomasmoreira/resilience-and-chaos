using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Http.Resilience;
using Orders;
using Polly;
using Polly.Simmy;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Live-toggleable chaos config, shared with the chaos strategies below.
builder.Services.AddSingleton<ChaosState>();

// Typed client to Payments by name (service discovery). We drop the ServiceDefaults standard
// resilience handler for this client and build an EXPLICIT pipeline — resilience is the point of
// this lab, so each strategy is visible and tuned. Order is outer→inner: retry wraps the circuit
// breaker (open circuit fails fast), a per-attempt timeout, and the chaos strategies are INNERMOST
// (they simulate the real call being slow/failing, so the resilience strategies react to them).
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental; intentional here.
builder.Services.AddHttpClient("payments", client => client.BaseAddress = new Uri("https+http://payments"))
    .RemoveAllResilienceHandlers()
#pragma warning restore EXTEXP0001
    .AddResilienceHandler("payments-pipeline", (pipeline, context) =>
    {
        var chaos = context.ServiceProvider.GetRequiredService<ChaosState>();

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

        // Chaos: injected just before the real call so the strategies above react to it.
        pipeline.AddChaosLatency(new ChaosLatencyStrategyOptions
        {
            EnabledGenerator = _ => ValueTask.FromResult(chaos.LatencyEnabled),
            InjectionRateGenerator = _ => ValueTask.FromResult(chaos.InjectionRate),
            LatencyGenerator = _ => ValueTask.FromResult(chaos.Latency),
        });

        pipeline.AddChaosOutcome(new ChaosOutcomeStrategyOptions<HttpResponseMessage>
        {
            EnabledGenerator = _ => ValueTask.FromResult(chaos.FaultEnabled),
            InjectionRateGenerator = _ => ValueTask.FromResult(chaos.InjectionRate),
            OutcomeGenerator = _ => ValueTask.FromResult<Outcome<HttpResponseMessage>?>(
                Outcome.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError))),
        });
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

// Toggle chaos at runtime — only the provided fields are applied (chaos as code, ADR-002).
app.MapPost("/chaos", (ChaosRequest request, ChaosState chaos) =>
{
    if (request.Fault is { } fault)
    {
        chaos.FaultEnabled = fault;
    }

    if (request.Latency is { } latency)
    {
        chaos.LatencyEnabled = latency;
    }

    if (request.InjectionRate is { } rate)
    {
        chaos.InjectionRate = rate;
    }

    if (request.LatencyMs is { } ms)
    {
        chaos.Latency = TimeSpan.FromMilliseconds(ms);
    }

    return Results.Ok(new
    {
        chaos.FaultEnabled,
        chaos.LatencyEnabled,
        chaos.InjectionRate,
        latencyMs = chaos.Latency.TotalMilliseconds,
    });
});

app.Run();

internal sealed record OrderRequest(decimal Amount);

internal sealed record PaymentResult(string Status, string TransactionId, decimal Amount);

internal sealed record OrderResult(string OrderId, string Status, PaymentResult? Payment);
