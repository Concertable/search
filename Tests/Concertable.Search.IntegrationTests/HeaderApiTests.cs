using System.Net;
using Concertable.Search.Application.DTOs;
using Xunit.Abstractions;

namespace Concertable.Search.IntegrationTests;

[Collection("Integration")]

public sealed class HeaderApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    private sealed record PaginationResponse<T>(IEnumerable<T> Data, int TotalCount, int TotalPages, int PageNumber, int PageSize);

    public HeaderApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region GetByAmount

    [Fact]
    public async Task GetByAmount_ShouldReturn200_WithArtists()
    {
        var client = fixture.CreateClient();
        var expected = fixture.SeedState.Artists.OrderBy(a => a.Id).Take(5).Select(a => a.Name).ToArray();

        var response = await client.GetAsync("/api/Header/amount/5?headerType=Artist");
        await response.ShouldBe(HttpStatusCode.OK);
        var headers = await response.Content.ReadAsync<ArtistHeader[]>();
        Assert.NotNull(headers);
        Assert.Equal(expected, headers.Select(h => h.Name).ToArray());
    }

    [Fact]
    public async Task GetByAmount_ShouldReturn200_WithVenues()
    {
        var client = fixture.CreateClient();
        var expected = fixture.SeedState.Venues.OrderBy(v => v.Id).Take(5).Select(v => v.Name).ToArray();

        var response = await client.GetAsync("/api/Header/amount/5?headerType=Venue");

        await response.ShouldBe(HttpStatusCode.OK);
        var headers = await response.Content.ReadAsync<VenueHeader[]>();
        Assert.NotNull(headers);
        Assert.Equal(expected, headers.Select(h => h.Name).ToArray());
    }

    [Fact]
    public async Task GetByAmount_ShouldReturn200_WithConcerts()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Header/amount/10?headerType=Concert");
        await response.ShouldBe(HttpStatusCode.OK);
        var headers = await response.Content.ReadAsync<ConcertHeader[]>();
        Assert.NotNull(headers);
        Assert.NotEmpty(headers);
    }

    [Fact]
    public async Task GetByAmount_ShouldReturn400_WhenNoHeaderType()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Header/amount/5");

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Search

    [Fact]
    public async Task Search_ShouldReturn200_WithPaginatedArtists()
    {
        var client = fixture.CreateClient();
        var artist = fixture.SeedState.Artist;
        var expected = fixture.SeedState.Artists.Where(a => a.Name.Contains(artist.Name)).Select(a => a.Name).ToHashSet();

        var response = await client.GetAsync($"/api/Header?headerType=Artist&searchTerm={Uri.EscapeDataString(artist.Name)}");

        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<PaginationResponse<ArtistHeader>>();
        Assert.NotNull(result);
        Assert.Equal(expected.Count, result.TotalCount);
        Assert.All(result.Data, h => Assert.Contains(h.Name, expected));
    }

    [Fact]
    public async Task Search_ShouldReturn200_WithPaginatedVenues()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Header?headerType=Venue");

        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<PaginationResponse<VenueHeader>>();
        Assert.NotNull(result);
        Assert.Equal(fixture.SeedState.Venues.Count, result.TotalCount);
        var venueNames = fixture.SeedState.Venues.Select(v => v.Name).ToHashSet();
        Assert.All(result.Data, h => Assert.Contains(h.Name, venueNames));
    }

    [Fact]
    public async Task Search_ShouldReturn200_WithPaginatedConcerts()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Header?headerType=Concert");

        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<PaginationResponse<ConcertHeader>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Data);
    }

    [Fact]
    public async Task Search_ShouldReturn200_WithEmptyData_WhenNoMatch()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/Header?headerType=Artist&searchTerm=zzznomatch");

        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<PaginationResponse<ArtistHeader>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Search_ShouldReturn200_WithResults_WhenCommaDelimitedGenreIdsContainMatch()
    {
        // Arrange
        var client = fixture.CreateClient();
        var genres = fixture.SeedState.Artists
            .SelectMany(a => a.ArtistGenres).Select(g => g.Genre).Distinct().Take(2).ToArray();
        var expected = fixture.SeedState.Artists
            .Where(a => a.ArtistGenres.Any(g => genres.Contains(g.Genre)))
            .Select(a => a.Name)
            .ToHashSet();

        // Act
        var response = await client.GetAsync($"/api/Header?headerType=Artist&genres={string.Join(',', genres)}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<PaginationResponse<ArtistHeader>>();
        Assert.NotNull(result);
        Assert.Equal(expected.Count, result.TotalCount);
        Assert.All(result.Data, h => Assert.Contains(h.Name, expected));
    }

    [Fact]
    public async Task Search_ShouldReturn200_WithOnlyMatchingArtists_WhenGenreFiltered()
    {
        // Arrange
        var client = fixture.CreateClient();
        var genre = fixture.SeedState.Artists
            .SelectMany(a => a.ArtistGenres).Select(g => g.Genre).First();
        var expected = fixture.SeedState.Artists
            .Where(a => a.ArtistGenres.Any(g => g.Genre == genre))
            .Select(a => a.Name)
            .ToHashSet();

        // Act
        var response = await client.GetAsync($"/api/Header?headerType=Artist&genres={genre}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<PaginationResponse<ArtistHeader>>();
        Assert.NotNull(result);
        Assert.Equal(expected.Count, result.TotalCount);
        Assert.All(result.Data, h => Assert.Contains(h.Name, expected));
    }

    #endregion
}
