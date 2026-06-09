// The AppHost orchestrates Orders + Payments. Orders calls Payments by name (service discovery);
// the resilience pipeline and chaos injection are added on that call in later phases.

var builder = DistributedApplication.CreateBuilder(args);

var payments = builder.AddProject<Projects.Payments>("payments")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Orders>("orders")
    .WithReference(payments)
    .WaitFor(payments)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
