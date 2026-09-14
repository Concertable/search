using Concertable.Search.Application;
using Concertable.Search.Application.DTOs;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class QueryableAutocompleteMappers
{
    public static IQueryable<Autocomplete> ToAutocompletes(this IQueryable<ArtistReadModel> query) =>
        query.Select(a => new Autocomplete { Id = a.Id, Name = a.Name, Type = HeaderType.Artist });

    public static IQueryable<Autocomplete> ToAutocompletes(this IQueryable<VenueReadModel> query) =>
        query.Select(v => new Autocomplete { Id = v.Id, Name = v.Name, Type = HeaderType.Venue });

    public static IQueryable<Autocomplete> ToAutocompletes(this IQueryable<ConcertReadModel> query) =>
        query.Select(c => new Autocomplete { Id = c.Id, Name = c.Name, Type = HeaderType.Concert });
}
