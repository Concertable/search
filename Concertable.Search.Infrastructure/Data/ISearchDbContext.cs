using Concertable.Artist.Domain;
using Concertable.Concert.Domain;
using Concertable.Search.Domain.Models;
using Concertable.Venue.Domain;

namespace Concertable.Search.Infrastructure.Data;

internal interface ISearchDbContext
{
    IQueryable<ArtistSearchModel> Artists { get; }
    IQueryable<VenueSearchModel> Venues { get; }
    IQueryable<ConcertSearchModel> Concerts { get; }
    IQueryable<ArtistRatingProjection> ArtistRatingProjections { get; }
    IQueryable<VenueRatingProjection> VenueRatingProjections { get; }
    IQueryable<ConcertRatingProjection> ConcertRatingProjections { get; }
}
