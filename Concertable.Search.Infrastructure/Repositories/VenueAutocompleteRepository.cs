using Concertable.Search.Application.DTOs;
using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal sealed class VenueAutocompleteRepository : IVenueAutocompleteRepository
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

    public async Task<IEnumerable<Autocomplete>> GetAsync(string? searchTerm) =>
        await specification
            .Apply(context.Venues, new SearchParams { SearchTerm = searchTerm })
            .ToAutocompletes()
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
}
