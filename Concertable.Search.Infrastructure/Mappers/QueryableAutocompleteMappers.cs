using Concertable.Search.Application.DTOs;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class QueryableAutocompleteMappers
{
    public static IQueryable<Autocomplete> ToAutocompletes(this IQueryable<ArtistReadModel> query) =>
        query.Select(a => new Autocomplete { Id = a.Id, Name = a.Name, Type = "artist" });

    public static IQueryable<Autocomplete> ToAutocompletes(this IQueryable<VenueReadModel> query) =>
        query.Select(v => new Autocomplete { Id = v.Id, Name = v.Name, Type = "venue" });

    public static IQueryable<Autocomplete> ToAutocompletes(this IQueryable<ConcertReadModel> query) =>
        query.Select(c => new Autocomplete { Id = c.Id, Name = c.Name, Type = "concert" });
}
