using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal class VenueAutocompleteRepository : IVenueAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IVenueSearchSpecification specification;

    public VenueAutocompleteRepository(
        ISearchDbContext context,
        IVenueSearchSpecification specification)
    {
        this.context = context;
        this.specification = specification;
    }

    public async Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm) =>
        await specification
            .Apply(context.Venues, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompleteDtos()
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
}
