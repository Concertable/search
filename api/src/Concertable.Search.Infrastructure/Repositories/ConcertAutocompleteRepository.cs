using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class ConcertAutocompleteRepository : IConcertAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IConcertSearchQueryBuilder queryBuilder;

    public ConcertAutocompleteRepository(
        ISearchDbContext context,
        IConcertSearchQueryBuilder queryBuilder)
    {
        this.context = context;
        this.queryBuilder = queryBuilder;
    }

    public async Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm) =>
        await queryBuilder
            .Build(context.Concerts, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompletes()
            .OrderBy(r => EF.Functions.Collate(r.Name.ToLower(), "C"))
            .ThenBy(r => EF.Functions.Collate(r.Name, "C"))
            .Take(10)
            .ToListAsync();
}
