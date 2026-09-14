using System.Net;
using System.Text;
using Concertable.Search.TestKit;

namespace Concertable.Search.UnitTests.TestKit;

public sealed class SearchTestClientTests
{
    [Fact]
    public async Task GetAutocompleteAsync_BuildsQueryAndDeserializesResult()
    {
        string? requestUri = null;
        using var httpClient = CreateHttpClient((request, _) =>
        {
            requestUri = request.RequestUri?.PathAndQuery;
            return JsonResponse("""[{"$type":"artist","id":42,"name":"The Artist"}]""");
        });
        var client = new SearchTestClient(httpClient);

        var results = await client.GetAutocompleteAsync("The Artist", SearchProjectionType.Artist);

        Assert.Equal("/api/Autocomplete?headerType=Artist&searchTerm=The%20Artist", requestUri);
        var result = Assert.Single(results);
        Assert.Equal(SearchProjectionType.Artist, result.Type);
        Assert.Equal(42, result.Id);
        Assert.Equal("The Artist", result.Name);
    }

    [Fact]
    public async Task WaitForProjectionAsync_RetriesUntilProjectionIsObservable()
    {
        var requestCount = 0;
        using var httpClient = CreateHttpClient((_, _) =>
        {
            requestCount++;
            return requestCount == 1
                ? JsonResponse("[]")
                : JsonResponse("""[{"$type":"venue","id":7,"name":"The Venue"}]""");
        });
        var client = new SearchTestClient(httpClient);

        var result = await client.WaitForProjectionAsync(
            SearchProjectionType.Venue,
            id: 7,
            name: "The Venue",
            timeout: TimeSpan.FromSeconds(1),
            pollInterval: TimeSpan.FromMilliseconds(1));

        Assert.Equal(2, requestCount);
        Assert.Equal("The Venue", result.Name);
    }

    [Fact]
    public async Task GetAutocompleteAsync_ThrowsWhenSearchReturnsFailure()
    {
        using var httpClient = CreateHttpClient((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = new SearchTestClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAutocompleteAsync());
    }

    private static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory)
        => new(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("https://search.test/")
        };

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request, cancellationToken));
    }
}
