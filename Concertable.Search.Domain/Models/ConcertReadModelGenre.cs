using Concertable.Contracts;

namespace Concertable.Search.Domain.Models;

public sealed class ConcertReadModelGenre
{
    public int ConcertId { get; set; }
    public Genre Genre { get; set; }
    public ConcertReadModel Concert { get; set; } = null!;
}
