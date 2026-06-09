namespace Orders;

/// <summary>
/// Live-toggleable chaos configuration shared with the Polly chaos strategies. The chaos
/// generators read these fields on every call, so flipping them via the /chaos endpoint changes
/// the behavior of the Payments pipeline at runtime — chaos as code, no redeploy (ADR-002).
/// </summary>
internal sealed class ChaosState
{
    public bool FaultEnabled { get; set; }

    public bool LatencyEnabled { get; set; }

    public double InjectionRate { get; set; } = 1.0;

    public TimeSpan Latency { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>Body of POST /chaos — only the provided fields are applied.</summary>
internal sealed record ChaosRequest(bool? Fault, bool? Latency, double? InjectionRate, int? LatencyMs);
