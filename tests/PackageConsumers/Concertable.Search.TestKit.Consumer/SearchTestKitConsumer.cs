using Concertable.Search.TestKit;

namespace Concertable.Search.TestKit.Consumer;

public static class SearchTestKitConsumer
{
    public static SearchTestClient CreateClient(HttpClient httpClient) => new(httpClient);

    public static Task<SearchAutocompleteResult> WaitForProjectionAsync(
        SearchTestClient client,
        SearchProjectionType type,
        int id,
        string name,
        CancellationToken cancellationToken = default)
        => client.WaitForProjectionAsync(
            type,
            id,
            name,
            timeout: TimeSpan.FromSeconds(30),
            pollInterval: TimeSpan.FromMilliseconds(250),
            cancellationToken);
}
