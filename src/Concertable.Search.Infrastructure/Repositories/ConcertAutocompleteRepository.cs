using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class ConcertAutocompleteRepository : IConcertAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IConcertSearchQuery searchQuery;

    public ConcertAutocompleteRepository(
        ISearchDbContext context,
        IConcertSearchQuery searchQuery)
    {
        this.context = context;
        this.searchQuery = searchQuery;
    }

    public async Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm) =>
        await searchQuery
            .Apply(context.Concerts, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompletes()
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
}
