using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Concertable.B2B.Hosting;
using Concertable.B2B.Seed.Contracts;
using Concertable.Search.Hosting;
using Concertable.Search.TestKit;

namespace Concertable.Search.E2ETests;

public sealed class SeedConvergenceTests
{
    [Fact]
    public async Task B2BSimulatorEventsRebuildEverySearchProjectionType()
    {
        using var startupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Concertable_Search_AppHost>(startupTimeout.Token);

        await using var app = await builder.BuildAsync(startupTimeout.Token);

        await app.StartAsync(startupTimeout.Token);

        await WaitForHealthyAsync(
            app,
            builder.Resources.Select(resource => resource.Name).ToArray(),
            SearchConstants.WebResource,
            startupTimeout.Token);
        await app.ResourceNotifications.WaitForResourceAsync(
            B2BSeedingSimulator.Name,
            KnownResourceStates.Exited,
            startupTimeout.Token);
        Assert.True(app.ResourceNotifications.TryGetCurrentState(
            B2BSeedingSimulator.Name,
            out var simulator));
        Assert.Equal(0, simulator.Snapshot.ExitCode);

        // Named, not defaulted: launchSettings declares https before http, and the unnamed overload
        // takes whichever endpoint comes first. CI trusts no development certificate, so resolving to
        // https fails the TLS handshake rather than the assertion.
        using var httpClient = app.CreateHttpClient(SearchConstants.WebResource, "http");
        var search = new SearchTestClient(httpClient);
        var seed = new SeedCatalog(TimeProvider.System);
        var observableConcert = seed.Concerts.First(concert =>
            concert.DatePosted is not null && concert.Period.End > seed.Now);

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
            observableConcert.ConcertId,
            observableConcert.Name,
            startupTimeout.Token);
    }

    // Aspire reports only the resource that was waited on, so a dependency that died takes the blame with
    // it and the failure names search-web whatever actually broke. Re-throwing with every resource's state
    // is the difference between reading the cause and guessing at it.
    private static async Task WaitForHealthyAsync(
        DistributedApplication app,
        IReadOnlyCollection<string> resourceNames,
        string resourceName,
        CancellationToken cancellationToken)
    {
        try
        {
            await app.ResourceNotifications.WaitForResourceHealthyAsync(resourceName, cancellationToken);
        }
        catch (Exception exception)
        {
            var states = resourceNames.Select(name =>
                app.ResourceNotifications.TryGetCurrentState(name, out var current)
                    ? $"{name}={current.Snapshot.State?.Text ?? "unknown"}"
                        + (current.Snapshot.ExitCode is { } exitCode ? $"(exit {exitCode})" : string.Empty)
                    : $"{name}=unreported");

            throw new InvalidOperationException(
                $"'{resourceName}' never became healthy. Resource states: {string.Join(", ", states)}.",
                exception);
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
}
