using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Concertable.B2B.Hosting;
using Concertable.B2B.Seed.Contracts;
using Concertable.Search.Hosting;
using Concertable.Search.TestKit;

namespace Concertable.Search.StandaloneTests;

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
