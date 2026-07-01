using Concertable.Contracts;
using Concertable.Kernel;
using System.Text.Json.Serialization;

namespace Concertable.Search.Application.DTOs;

[JsonDerivedType(typeof(ArtistHeader), HeaderTypeNames.Artist)]
[JsonDerivedType(typeof(VenueHeader), HeaderTypeNames.Venue)]
[JsonDerivedType(typeof(ConcertHeader), HeaderTypeNames.Concert)]
public interface IHeader : IHasRating, IAddress
{
    string Name { get; }
    string ImageUrl { get; }
}

public sealed record ArtistHeader : IHeader, IAddress
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string ImageUrl { get; init; }
    public double? Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}

public sealed record VenueHeader : IHeader, IAddress
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string ImageUrl { get; init; }
    public double? Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}

public sealed record ConcertHeader : IHeader, IAddress
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string ImageUrl { get; init; }
    public double? Rating { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? DatePosted { get; init; }
    public IEnumerable<Genre> Genres { get; init; } = [];
}

public sealed class Autocomplete : IHasName
{
    [JsonPropertyName("$type")]
    public required HeaderType Type { get; init; }
    public required int Id { get; init; }
    public required string Name { get; init; }
}
