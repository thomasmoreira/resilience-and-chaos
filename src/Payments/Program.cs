var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

// The downstream dependency. Healthy by default — Orders injects chaos into its call to it.
app.MapPost("/payments", (PaymentRequest request) =>
    Results.Ok(new PaymentResult("charged", $"txn-{Guid.NewGuid():N}", request.Amount)));

app.Run();

internal sealed record PaymentRequest(string OrderId, decimal Amount);

internal sealed record PaymentResult(string Status, string TransactionId, decimal Amount);
