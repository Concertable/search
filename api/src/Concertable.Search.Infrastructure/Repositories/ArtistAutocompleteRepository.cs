using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class ArtistAutocompleteRepository : IArtistAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IArtistSearchQueryBuilder queryBuilder;

    public ArtistAutocompleteRepository(
        ISearchDbContext context,
        IArtistSearchQueryBuilder queryBuilder)
    {
        this.context = context;
        this.queryBuilder = queryBuilder;
    }

    public async Task<IReadOnlyList<Autocomplete>> GetAsync(string? searchTerm) =>
        await queryBuilder
            .Build(context.Artists, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompletes()
            .OrderBy(r => EF.Functions.Collate(r.Name.ToLower(), "C"))
            .ThenBy(r => EF.Functions.Collate(r.Name, "C"))
            .Take(10)
            .ToListAsync();
}
