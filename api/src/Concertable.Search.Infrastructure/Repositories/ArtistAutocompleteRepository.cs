using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class ArtistAutocompleteRepository : IArtistAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IArtistSearchQuery searchQuery;

    public ArtistAutocompleteRepository(
        ISearchDbContext context,
        IArtistSearchQuery searchQuery)
    {
        this.context = context;
        this.searchQuery = searchQuery;
    }

    public async Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm) =>
        await searchQuery
            .Apply(context.Artists, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompletes()
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
}
