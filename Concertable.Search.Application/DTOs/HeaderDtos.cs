using Concertable.Contracts;
using Concertable.Kernel;
using System.Text.Json.Serialization;

namespace Concertable.Search.Application.DTOs;

[JsonDerivedType(typeof(ArtistHeaderDto), "artist")]
[JsonDerivedType(typeof(VenueHeaderDto), "venue")]
[JsonDerivedType(typeof(ConcertHeaderDto), "concert")]
public interface IHeader : IHasRating, IAddress
{
    string Name { get; set; }
    string ImageUrl { get; set; }
}

public sealed record ArtistHeaderDto : IHeader, IAddress
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
    public double? Rating { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public IEnumerable<Genre> Genres { get; set; } = [];
}

public sealed record VenueHeaderDto : IHeader, IAddress
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
    public double? Rating { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public IEnumerable<Genre> Genres { get; set; } = [];
}

public sealed record ConcertHeaderDto : IHeader, IAddress
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
    public double? Rating { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? DatePosted { get; set; }
    public IEnumerable<Genre> Genres { get; set; } = [];
}

public sealed class AutocompleteDto : IHasName
{
    [JsonPropertyName("$type")]
    public required string Type { get; init; }
    public required int Id { get; init; }
    public required string Name { get; init; }
}
