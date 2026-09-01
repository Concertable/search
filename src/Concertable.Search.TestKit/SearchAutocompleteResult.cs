using System.Text.Json.Serialization;

namespace Concertable.Search.TestKit;

/// <summary>
/// Describes the observable identity returned by the Search autocomplete API.
/// </summary>
public sealed record SearchAutocompleteResult
{
    /// <summary>Gets the projection kind.</summary>
    [JsonPropertyName("$type")]
    public required SearchProjectionType Type { get; init; }

    /// <summary>Gets the producer-owned entity identifier.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the projected display name.</summary>
    public required string Name { get; init; }
}
