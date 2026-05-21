using Concertable.Concert.Domain;
using Concertable.Search.Domain.Models;
using LinqKit;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class QueryableConcertHeaderMappers
{
    public static IQueryable<ConcertHeaderDto> ToHeaderDtos(
        this IQueryable<ConcertSearchModel> query,
        IQueryable<ConcertRatingProjection> ratings) =>
        from c in query.Where(c => c.Venue.Location != null).AsExpandable()
        join r in ratings on c.Id equals r.ConcertId into rg
        from rating in rg.DefaultIfEmpty()
        select new ConcertHeaderDto
        {
            Id = c.Id,
            Name = c.Name,
            ImageUrl = c.Artist.Avatar,
            Rating = rating != null ? rating.AverageRating : null,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            DatePosted = c.DatePosted,
            County = c.Venue.Address != null ? c.Venue.Address.County ?? string.Empty : string.Empty,
            Town = c.Venue.Address != null ? c.Venue.Address.Town ?? string.Empty : string.Empty,
            Genres = ConcertSearchGenreSelectors.FromConcert.Invoke(c)
        };
}
