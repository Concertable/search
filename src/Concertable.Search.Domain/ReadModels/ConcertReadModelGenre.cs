using Concertable.Contracts.Enums;

namespace Concertable.Search.Domain.ReadModels;

public sealed class ConcertReadModelGenre
{
    public int ConcertId { get; set; }
    public Genre Genre { get; set; }
    public ConcertReadModel Concert { get; set; } = null!;
}
