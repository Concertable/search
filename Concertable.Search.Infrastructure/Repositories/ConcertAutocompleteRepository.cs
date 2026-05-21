using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal class ConcertAutocompleteRepository : IConcertAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IConcertSearchSpecification specification;

    public ConcertAutocompleteRepository(
        ISearchDbContext context,
        IConcertSearchSpecification specification)
    {
        this.context = context;
        this.specification = specification;
    }

    public async Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm) =>
        await specification
            .Apply(context.Concerts, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompleteDtos()
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
}
