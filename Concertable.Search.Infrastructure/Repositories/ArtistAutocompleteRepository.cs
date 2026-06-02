using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class ArtistAutocompleteRepository : IArtistAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IArtistSearchSpecification specification;

    public ArtistAutocompleteRepository(
        ISearchDbContext context,
        IArtistSearchSpecification specification)
    {
        this.context = context;
        this.specification = specification;
    }

    public async Task<IEnumerable<Autocomplete>> GetAsync(string? searchTerm) =>
        await specification
            .Apply(context.Artists, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompletes()
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
}
