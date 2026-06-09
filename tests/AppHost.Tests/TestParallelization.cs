// Each test class starts its own distributed app, and the chaos experiment opens the circuit
// breaker — so run classes sequentially to isolate that state and avoid racing the launch ports.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
