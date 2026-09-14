using Concertable.Search.Application.DTOs;
using Concertable.Search.Domain.ReadModels;
using LinqKit;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class QueryableConcertHeaderMappers
{
    public static IQueryable<ConcertHeader> ToHeaderDtos(
        this IQueryable<ConcertReadModel> query,
        IQueryable<ArtistReadModel> artists,
        IQueryable<VenueReadModel> venues,
        IQueryable<ConcertRatingProjection> ratings) =>
        from c in query.AsExpandable()
        join a in artists on c.ArtistId equals a.Id
        join v in venues on c.VenueId equals v.Id
        join r in ratings on c.Id equals r.ConcertId into rg
        from rating in rg.DefaultIfEmpty()
        select new ConcertHeader
        {
            Id = c.Id,
            Name = c.Name,
            ImageUrl = a.Avatar,
            Rating = rating != null ? rating.AverageRating : null,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            DatePosted = c.DatePosted,
            County = v.Address.County,
            Town = v.Address.Town,
            Genres = ConcertSearchGenreSelectors.FromConcert.Invoke(c)
        };
}
