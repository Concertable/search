using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal class ArtistAutocompleteRepository : IArtistAutocompleteRepository
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

    public async Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm) =>
        await specification
            .Apply(context.Artists, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompleteDtos()
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
}
