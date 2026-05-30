using System.Net;
using Concertable.Contracts;
using Concertable.Search.Application.DTOs;

namespace Concertable.Search.IntegrationTests;

[Collection("Integration")]

public class ConcertHeaderApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ConcertHeaderApiTests(ApiFixture fixture) => this.fixture = fixture;
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region GetPopular

    [Fact]
    public async Task GetPopular_ShouldReturn200_WithPostedConcerts()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/concert/headers/popular");

        await response.ShouldBe(HttpStatusCode.OK);
        var concerts = await response.Content.ReadAsync<ConcertHeaderDto[]>();
        Assert.NotNull(concerts);
        Assert.NotEmpty(concerts);
    }

    #endregion

    #region GetFree

    [Fact]
    public async Task GetFree_ShouldReturn200_WithEmptyList_WhenNoPaidConcerts()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/concert/headers/free");

        await response.ShouldBe(HttpStatusCode.OK);
        var concerts = await response.Content.ReadAsync<ConcertHeaderDto[]>();
        Assert.NotNull(concerts);
        Assert.Empty(concerts);
    }

    #endregion

    #region GetRecommended

    [Fact]
    public async Task GetRecommended_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/concert/headers/recommended");

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRecommended_ShouldReturn200_WithConcerts_WhenAuthenticated()
    {
        var client = fixture.CreateClient(fixture.SeedState.Customer);

        var response = await client.GetAsync("/api/concert/headers/recommended");

        await response.ShouldBe(HttpStatusCode.OK);
        var concerts = await response.Content.ReadAsync<ConcertHeaderDto[]>();
        Assert.NotNull(concerts);
        Assert.NotEmpty(concerts);
    }

    [Fact]
    public async Task GetRecommended_ShouldReturn200_FilteredByGenre_WhenGenreProvided()
    {
        var client = fixture.CreateClient(fixture.SeedState.Customer);

        var response = await client.GetAsync($"/api/concert/headers/recommended?genres={Genre.Rock}");

        await response.ShouldBe(HttpStatusCode.OK);
        var concerts = await response.Content.ReadAsync<ConcertHeaderDto[]>();
        Assert.NotNull(concerts);
        Assert.NotEmpty(concerts);
    }

    [Fact]
    public async Task GetRecommended_ShouldReturn200_WithEmptyList_WhenGenreHasNoMatches()
    {
        var client = fixture.CreateClient(fixture.SeedState.Customer);

        var response = await client.GetAsync($"/api/concert/headers/recommended?genres={Genre.Jazz}");

        await response.ShouldBe(HttpStatusCode.OK);
        var concerts = await response.Content.ReadAsync<ConcertHeaderDto[]>();
        Assert.NotNull(concerts);
        Assert.Empty(concerts);
    }

    [Fact]
    public async Task GetRecommended_ShouldReturn200_WithConcerts_WhenCommaDelimitedGenreIdsContainMatch()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.Customer);

        // Act
        var response = await client.GetAsync($"/api/concert/headers/recommended?genres={Genre.Rock},{Genre.Jazz}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var concerts = await response.Content.ReadAsync<ConcertHeaderDto[]>();
        Assert.NotNull(concerts);
        Assert.NotEmpty(concerts);
    }

    #endregion
}
