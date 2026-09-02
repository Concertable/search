using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.B2B.Seed.Contracts;
using Concertable.Search.Hosting;
using Concertable.Search.TestKit;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using Xunit.Abstractions;

namespace Concertable.Search.StandaloneTests;

public sealed class SeedConvergenceTests
{
    private static readonly IReadOnlyList<string> resourceNames =
    [
        "concertable-search-sql-data",
        SearchConstants.Database,
        "asb",
        AuthConstants.Resource,
        SearchConstants.MigrationsResource,
        SearchConstants.WebResource,
        SearchConstants.WorkersResource,
        B2BConstants.SeedingSimulatorResource
    ];

    private readonly ITestOutputHelper output;

    public SeedConvergenceTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task B2BSimulatorEventsRebuildEverySearchProjectionType()
    {
        using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Concertable_Search_AppHost>(startupTimeout.Token);

        await using var app = await builder.BuildAsync(startupTimeout.Token);
        await app.StartAsync(startupTimeout.Token);

        try
        {
            await app.ResourceNotifications.WaitForResourceHealthyAsync(
                SearchConstants.WebResource,
                startupTimeout.Token);
            await app.ResourceNotifications.WaitForResourceAsync(
                B2BConstants.SeedingSimulatorResource,
                KnownResourceStates.Exited,
                startupTimeout.Token);
            Assert.True(app.ResourceNotifications.TryGetCurrentState(
                B2BConstants.SeedingSimulatorResource,
                out var simulator));
            Assert.Equal(0, simulator.Snapshot.ExitCode);

            using var httpClient = app.CreateHttpClient(SearchConstants.WebResource);
            var search = new SearchTestClient(httpClient);
            var seed = new SeedCatalog(TimeProvider.System);

            await AssertProjectionAsync(
                search,
                SearchProjectionType.Artist,
                seed.Artists[0].ArtistId,
                seed.Artists[0].Name,
                startupTimeout.Token);
            await AssertProjectionAsync(
                search,
                SearchProjectionType.Venue,
                seed.Venues[0].VenueId,
                seed.Venues[0].Name,
                startupTimeout.Token);
            await AssertProjectionAsync(
                search,
                SearchProjectionType.Concert,
                seed.Concerts[0].ConcertId,
                seed.Concerts[0].Name,
                startupTimeout.Token);
        }
        catch
        {
            await WriteDiagnosticsAsync(app);
            throw;
        }
    }

    private static async Task AssertProjectionAsync(
        SearchTestClient search,
        SearchProjectionType type,
        int id,
        string name,
        CancellationToken cancellationToken)
    {
        var projection = await search.WaitForProjectionAsync(
            type,
            id,
            name,
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(2),
            cancellationToken).ConfigureAwait(false);

        Assert.Equal(name, projection.Name);
    }

    private async Task WriteDiagnosticsAsync(DistributedApplication app)
    {
        var loggers = app.Services.GetRequiredService<ResourceLoggerService>();

        foreach (var resourceName in resourceNames)
        {
            if (app.ResourceNotifications.TryGetCurrentState(resourceName, out var resource))
            {
                output.WriteLine(
                    "Resource {0}: state={1}, health={2}, exitCode={3}",
                    resourceName,
                    resource.Snapshot.State?.Text ?? "unknown",
                    resource.Snapshot.HealthStatus?.ToString() ?? "unknown",
                    resource.Snapshot.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "unknown");
            }

            await foreach (var batch in loggers.GetAllAsync(resourceName).ConfigureAwait(false))
            {
                foreach (var line in batch)
                    output.WriteLine("Resources.{0}: {1}", resourceName, line.Content);
            }
        }
    }
}
