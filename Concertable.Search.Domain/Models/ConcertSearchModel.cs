using Concertable.Shared;
using NetTopologySuite.Geometries;

namespace Concertable.Search.Domain.Models;

public sealed class ConcertSearchModel : IIdEntity, IHasName, IHasLocation, IEntity
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int ArtistId { get; set; }
    public int VenueId { get; set; }
    public string Name { get; set; } = null!;
    public string? Avatar { get; set; }
    public decimal Price { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? DatePosted { get; set; }
    public Point? Location { get; set; }
    public ArtistSearchModel Artist { get; set; } = null!;
    public VenueSearchModel Venue { get; set; } = null!;
    public HashSet<ConcertSearchModelGenre> ConcertGenres { get; set; } = [];
}
