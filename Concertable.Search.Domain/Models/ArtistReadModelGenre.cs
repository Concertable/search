using Concertable.Shared;

namespace Concertable.Search.Domain.Models;

public sealed class ArtistReadModelGenre
{
    public int ArtistId { get; set; }
    public Genre Genre { get; set; }
    public ArtistReadModel Artist { get; set; } = null!;
}
