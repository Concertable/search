namespace Concertable.Search.Domain.Projections;

public sealed class ArtistRatingProjection
{
    public int ArtistId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
