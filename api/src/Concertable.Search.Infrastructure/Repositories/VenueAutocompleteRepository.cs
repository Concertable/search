using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class VenueAutocompleteRepository : IVenueAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IVenueSearchQuery searchQuery;

    public VenueAutocompleteRepository(
        ISearchDbContext context,
        IVenueSearchQuery searchQuery)
    {
        this.context = context;
        this.searchQuery = searchQuery;
    }

    public async Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm) =>
        await searchQuery
            .Apply(context.Venues, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompletes()
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
}
