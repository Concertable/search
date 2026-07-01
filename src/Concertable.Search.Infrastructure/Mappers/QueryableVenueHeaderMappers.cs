using Concertable.Search.Application.DTOs;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class QueryableVenueHeaderMappers
{
    public static IQueryable<VenueHeader> ToHeaderDtos(
        this IQueryable<VenueReadModel> query,
        IQueryable<VenueRatingProjection> ratings) =>
        from v in query
        join r in ratings on v.Id equals r.VenueId into rg
        from rating in rg.DefaultIfEmpty()
        select new VenueHeader
        {
            Id = v.Id,
            Name = v.Name,
            ImageUrl = v.Avatar,
            Rating = rating != null ? rating.AverageRating : null,
            County = v.Address.County,
            Town = v.Address.Town
        };
}
