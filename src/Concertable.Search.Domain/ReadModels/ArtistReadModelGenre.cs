using Concertable.Contracts.Enums;

namespace Concertable.Search.Domain.ReadModels;

public sealed class ArtistReadModelGenre
{
    public int ArtistId { get; set; }
    public Genre Genre { get; set; }
    public ArtistReadModel Artist { get; set; } = null!;
}
