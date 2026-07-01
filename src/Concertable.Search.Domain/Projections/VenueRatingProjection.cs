namespace Concertable.Search.Domain.Projections;

public sealed class VenueRatingProjection
{
    public int VenueId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
