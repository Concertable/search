using Concertable.Shared;

namespace Concertable.Search.Domain.Models;

public sealed class ConcertSearchModelGenre
{
    public int ConcertId { get; set; }
    public Genre Genre { get; set; }
    public ConcertSearchModel Concert { get; set; } = null!;
}
