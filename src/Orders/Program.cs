using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Orders;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Simmy;
using Polly.Simmy.Latency;
using Polly.Simmy.Outcomes;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Live-toggleable chaos config, shared with the chaos strategies below.
builder.Services.AddSingleton<ChaosState>();

// Inbound rate limiter (backpressure): caps concurrent /orders so a traffic spike can't saturate
// the service — excess requests get 429 instead of dragging everything down (ADR-004).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddConcurrencyLimiter("orders", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.QueueLimit = 50;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// Explicit Polly pipeline on the Payments call. Order outer→inner: bulkhead (concurrency
// isolation) → retry → circuit breaker → per-attempt timeout → chaos (innermost, simulating the
// real call). The standard ServiceDefaults handler is removed so this is the only pipeline.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental; intentional here.
builder.Services.AddHttpClient("payments", client => client.BaseAddress = new Uri("https+http://payments"))
    .RemoveAllResilienceHandlers()
#pragma warning restore EXTEXP0001
    .AddResilienceHandler("payments-pipeline", (pipeline, context) =>
    {
        var chaos = context.ServiceProvider.GetRequiredService<ChaosState>();

        // Bulkhead: isolate resources — a slow dependency can't consume unbounded concurrency.
        pipeline.AddConcurrencyLimiter(permitLimit: 50, queueLimit: 25);

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

app.UseRateLimiter();

app.MapDefaultEndpoints();

app.MapPost("/orders", async (OrderRequest request, IHttpClientFactory factory, CancellationToken ct) =>
{
    var orderId = Guid.NewGuid().ToString("N")[..8];
    var payments = factory.CreateClient("payments");

    try
    {
        using var response = await payments.PostAsJsonAsync(
            "/payments", new { orderId, amount = request.Amount }, ct);
        response.EnsureSuccessStatusCode();
        var payment = await response.Content.ReadFromJsonAsync<PaymentResult>(ct);

        return Results.Ok(new OrderResult(orderId, "confirmed", payment));
    }
    catch (Exception ex) when (ex is HttpRequestException or BrokenCircuitException or TimeoutRejectedException)
    {
        // Graceful degradation: when Payments is unavailable, accept the order with payment pending
        // (reconcile later) instead of returning 5xx — availability is preserved (ADR-003).
        return Results.Ok(new OrderResult(orderId, "pending_payment", null));
    }
}).RequireRateLimiting("orders");

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
