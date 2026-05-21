using Concertable.Shared;
using NetTopologySuite.Geometries;

namespace Concertable.Search.Domain.Models;

public sealed class VenueSearchModel : IIdEntity, IHasName, IHasLocation, IEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Avatar { get; set; }
    public Point? Location { get; set; }
    public Address? Address { get; set; }
}
