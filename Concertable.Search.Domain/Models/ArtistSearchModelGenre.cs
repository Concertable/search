using Concertable.Shared;

namespace Concertable.Search.Domain.Models;

public sealed class ArtistSearchModelGenre
{
    public int ArtistId { get; set; }
    public Genre Genre { get; set; }
    public ArtistSearchModel Artist { get; set; } = null!;
}
