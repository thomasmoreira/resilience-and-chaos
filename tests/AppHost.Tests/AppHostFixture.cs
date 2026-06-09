using Aspire.Hosting;

namespace AppHost.Tests;

/// <summary>Starts Orders + Payments once for the test collection (real services, fast).</summary>
public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    public DistributedApplication App { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(CancellationToken.None);
        App = await builder.BuildAsync();
        await App.StartAsync();
        await App.ResourceNotifications.WaitForResourceHealthyAsync("orders").WaitAsync(StartupTimeout);
    }

    public async Task DisposeAsync() => await App.DisposeAsync();
}
