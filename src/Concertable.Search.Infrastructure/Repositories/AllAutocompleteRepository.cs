using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class AllAutocompleteRepository : IAllAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IArtistSearchQuery artistQuery;
    private readonly IVenueSearchQuery venueQuery;
    private readonly IConcertSearchQuery concertQuery;

    public AllAutocompleteRepository(
        ISearchDbContext context,
        IArtistSearchQuery artistQuery,
        IVenueSearchQuery venueQuery,
        IConcertSearchQuery concertQuery)
    {
        this.context = context;
        this.artistQuery = artistQuery;
        this.venueQuery = venueQuery;
        this.concertQuery = concertQuery;
    }

    public async Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm)
    {
        var searchParams = new SearchParams { SearchTerm = searchTerm };

        return await artistQuery
            .Apply(context.Artists, searchParams)
            .ToAutocompletes()
            .Take(20)
            .Concat(
                venueQuery
                    .Apply(context.Venues, searchParams)
                    .ToAutocompletes()
                    .Take(20))
            .Concat(
                concertQuery
                    .Apply(context.Concerts, searchParams)
                    .ToAutocompletes()
                    .Take(20))
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
    }
}
