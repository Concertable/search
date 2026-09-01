using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Concertable.Search.TestKit;

/// <summary>
/// Reads and polls the public Search autocomplete API without depending on Search implementation types.
/// </summary>
public sealed class SearchTestClient
{
    private static readonly JsonSerializerOptions serializerOptions = CreateSerializerOptions();
    private readonly HttpClient httpClient;

    /// <summary>
    /// Creates a client for the Search service root.
    /// </summary>
    /// <param name="httpClient">An HTTP client whose base address is the Search service root.</param>
    public SearchTestClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        if (httpClient.BaseAddress is null)
        {
            throw new ArgumentException("The Search HTTP client must have a base address.", nameof(httpClient));
        }

        this.httpClient = httpClient;
    }

    /// <summary>
    /// Gets projections visible through the public autocomplete endpoint.
    /// </summary>
    public async Task<IReadOnlyList<SearchAutocompleteResult>> GetAutocompleteAsync(
        string? searchTerm = null,
        SearchProjectionType? type = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(CreateAutocompleteUri(searchTerm, type), cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var results = await response.Content
            .ReadFromJsonAsync<List<SearchAutocompleteResult>>(serializerOptions, cancellationToken)
            .ConfigureAwait(false);

        return results ?? throw new InvalidDataException("The Search autocomplete response did not contain a JSON array.");
    }

    /// <summary>
    /// Waits until a known projection is observable through the public autocomplete endpoint.
    /// </summary>
    public async Task<SearchAutocompleteResult> WaitForProjectionAsync(
        SearchProjectionType type,
        int id,
        string name,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Projection IDs must be positive.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The projection name is required.", nameof(name));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be positive.");
        }

        if (pollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval), pollInterval, "The poll interval must be positive.");
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            while (true)
            {
                var results = await GetAutocompleteAsync(name, type, linkedSource.Token)
                    .ConfigureAwait(false);
                var projection = results.FirstOrDefault(result => result.Type == type && result.Id == id);

                if (projection is not null)
                {
                    return projection;
                }

                await Task.Delay(pollInterval, linkedSource.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Search projection {type} {id} ('{name}') was not observable within {timeout}.",
                exception);
        }
    }

    private static string CreateAutocompleteUri(string? searchTerm, SearchProjectionType? type)
    {
        var query = new List<string>(capacity: 2);

        if (type is not null)
        {
            query.Add($"headerType={Uri.EscapeDataString(type.Value.ToString())}");
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        }

        return query.Count == 0
            ? "api/Autocomplete"
            : $"api/Autocomplete?{string.Join('&', query)}";
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
