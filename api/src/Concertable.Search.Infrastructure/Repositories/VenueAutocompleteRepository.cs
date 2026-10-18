using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class VenueAutocompleteRepository : IVenueAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IVenueSearchQueryBuilder queryBuilder;

    public VenueAutocompleteRepository(
        ISearchDbContext context,
        IVenueSearchQueryBuilder queryBuilder)
    {
        this.context = context;
        this.queryBuilder = queryBuilder;
    }

    public async Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm) =>
        await queryBuilder
            .Build(context.Venues, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompletes()
            .OrderBy(r => EF.Functions.Collate(r.Name.ToLower(), "C"))
            .ThenBy(r => EF.Functions.Collate(r.Name, "C"))
            .Take(10)
            .ToListAsync();
}
