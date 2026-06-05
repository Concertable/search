using Concertable.Kernel;
using NetTopologySuite.Geometries;

namespace Concertable.Search.Domain.Models;

public sealed class ArtistReadModel : IIdEntity, IHasName, IHasLocation, IEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Avatar { get; set; } = null!;
    public Point Location { get; set; } = null!;
    public Address Address { get; set; } = null!;
    public HashSet<ArtistReadModelGenre> ArtistGenres { get; set; } = [];
}
