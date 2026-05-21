using System.Net;
using Concertable.Shared;

namespace Concertable.Search.IntegrationTests;

[Collection("Integration")]

public class AutocompleteApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public AutocompleteApiTests(ApiFixture fixture) => this.fixture = fixture;
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetHeaders_ShouldReturn200_WithResults_WhenSearchTermMatches()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Autocomplete?searchTerm=Test");

        await response.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadAsync<AutocompleteDto[]>();
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetHeaders_ShouldReturn200_WithEmptyList_WhenNoMatch()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Autocomplete?searchTerm=zzznomatch");

        await response.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadAsync<AutocompleteDto[]>();
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetHeaders_ShouldReturn200_WithAllResults_WhenNoSearchTerm()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Autocomplete");

        await response.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadAsync<AutocompleteDto[]>();
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task GetHeaders_ShouldReturn200_WithArtistResult_WhenArtistNameMatches()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Autocomplete?searchTerm=Test Artist");

        await response.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadAsync<AutocompleteDto[]>();
        Assert.NotNull(results);
        Assert.Contains(results, r => r.Name == "Test Artist" && r.Type == "artist");
    }
}
