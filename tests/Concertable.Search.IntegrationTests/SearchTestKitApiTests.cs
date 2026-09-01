using Concertable.Search.TestKit;
using Xunit.Abstractions;

namespace Concertable.Search.IntegrationTests;

[Collection("Integration")]
public sealed class SearchTestKitApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public SearchTestKitApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync()
    {
        fixture.DetachOutput();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task WaitForProjectionAsync_FindsProjectionThroughPublicApi()
    {
        using var httpClient = fixture.CreateClient();
        var client = new SearchTestClient(httpClient);
        var artist = fixture.SeedState.Artist;

        var result = await client.WaitForProjectionAsync(
            SearchProjectionType.Artist,
            artist.Id,
            artist.Name,
            timeout: TimeSpan.FromSeconds(5),
            pollInterval: TimeSpan.FromMilliseconds(100));

        Assert.Equal(artist.Name, result.Name);
    }
}
