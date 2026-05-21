using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class QueryableAutocompleteMappers
{
    public static IQueryable<AutocompleteDto> ToAutocompleteDtos(this IQueryable<ArtistReadModel> query) =>
        query.Select(a => new AutocompleteDto { Id = a.Id, Name = a.Name, Type = "artist" });

    public static IQueryable<AutocompleteDto> ToAutocompleteDtos(this IQueryable<VenueReadModel> query) =>
        query.Select(v => new AutocompleteDto { Id = v.Id, Name = v.Name, Type = "venue" });

    public static IQueryable<AutocompleteDto> ToAutocompleteDtos(this IQueryable<ConcertReadModel> query) =>
        query.Select(c => new AutocompleteDto { Id = c.Id, Name = c.Name, Type = "concert" });
}
