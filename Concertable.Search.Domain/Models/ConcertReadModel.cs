using Concertable.Kernel;
using NetTopologySuite.Geometries;

namespace Concertable.Search.Domain.Models;

public sealed class ConcertReadModel : IIdEntity, IHasName, IHasLocation, IEntity
{
    public int Id { get; set; }
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
    public Point Location { get; set; } = null!;
    public HashSet<ConcertReadModelGenre> ConcertGenres { get; set; } = [];
}
