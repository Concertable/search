using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Data;

internal interface ISearchDbContext
{
    IQueryable<ArtistReadModel> Artists { get; }
    IQueryable<VenueReadModel> Venues { get; }
    IQueryable<ConcertReadModel> Concerts { get; }
    IQueryable<ArtistRatingProjection> ArtistRatingProjections { get; }
    IQueryable<VenueRatingProjection> VenueRatingProjections { get; }
    IQueryable<ConcertRatingProjection> ConcertRatingProjections { get; }
}
