using Concertable.Artist.Domain;
using Concertable.Search.Domain.Models;
using LinqKit;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class QueryableArtistHeaderMappers
{
    public static IQueryable<ArtistHeaderDto> ToHeaderDtos(
        this IQueryable<ArtistSearchModel> query,
        IQueryable<ArtistRatingProjection> ratings) =>
        from a in query.AsExpandable()
        where a.Address != null
        join r in ratings on a.Id equals r.ArtistId into rg
        from rating in rg.DefaultIfEmpty()
        select new ArtistHeaderDto
        {
            Id = a.Id,
            Name = a.Name,
            ImageUrl = a.Avatar,
            Rating = rating != null ? rating.AverageRating : null,
            County = a.Address!.County,
            Town = a.Address!.Town,
            Genres = ArtistSearchGenreSelectors.FromArtist.Invoke(a)
        };
}
