using System.Net;
using Concertable.Search.Application.DTOs;
using Xunit.Abstractions;

namespace Concertable.Search.IntegrationTests;

[Collection("Integration")]

public sealed class AutocompleteApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public AutocompleteApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task GetHeaders_ShouldReturn200_WithResults_WhenSearchTermMatches()
    {
        var client = fixture.CreateClient();
        var venue = fixture.SeedState.Venue;

        var response = await client.GetAsync($"/api/Autocomplete?searchTerm={Uri.EscapeDataString(venue.Name)}");

        await response.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadAsync<Autocomplete[]>();
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetHeaders_ShouldReturn200_WithEmptyList_WhenNoMatch()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Autocomplete?searchTerm=zzznomatch");

        await response.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadAsync<Autocomplete[]>();
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetHeaders_ShouldReturn200_WithAllResults_WhenNoSearchTerm()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Autocomplete");

        await response.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadAsync<Autocomplete[]>();
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetHeaders_ShouldReturn200_WithArtistResult_WhenArtistNameMatches()
    {
        var client = fixture.CreateClient();
        var artist = fixture.SeedState.Artist;

        var response = await client.GetAsync($"/api/Autocomplete?searchTerm={Uri.EscapeDataString(artist.Name)}");

        await response.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadAsync<Autocomplete[]>();
        Assert.NotNull(results);
        Assert.Contains(results, r => r.Name == artist.Name && r.Type == "artist");
    }
}
