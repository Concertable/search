namespace Concertable.Search.Domain.Projections;

public sealed class ConcertRatingProjection
{
    public int ConcertId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
