using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class AllAutocompleteRepository : IAllAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IArtistSearchQueryBuilder artistQueryBuilder;
    private readonly IVenueSearchQueryBuilder venueQueryBuilder;
    private readonly IConcertSearchQueryBuilder concertQueryBuilder;

    public AllAutocompleteRepository(
        ISearchDbContext context,
        IArtistSearchQueryBuilder artistQueryBuilder,
        IVenueSearchQueryBuilder venueQueryBuilder,
        IConcertSearchQueryBuilder concertQueryBuilder)
    {
        this.context = context;
        this.artistQueryBuilder = artistQueryBuilder;
        this.venueQueryBuilder = venueQueryBuilder;
        this.concertQueryBuilder = concertQueryBuilder;
    }

    public async Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm)
    {
        var searchParams = new SearchParams { SearchTerm = searchTerm };

        return await artistQueryBuilder
            .Build(context.Artists, searchParams)
            .ToAutocompletes()
            .Take(20)
            .Concat(
                venueQueryBuilder
                    .Build(context.Venues, searchParams)
                    .ToAutocompletes()
                    .Take(20))
            .Concat(
                concertQueryBuilder
                    .Build(context.Concerts, searchParams)
                    .ToAutocompletes()
                    .Take(20))
            .OrderBy(r => EF.Functions.Collate(r.Name.ToLower(), "C"))
            .ThenBy(r => EF.Functions.Collate(r.Name, "C"))
            .Take(10)
            .ToListAsync();
    }
}
