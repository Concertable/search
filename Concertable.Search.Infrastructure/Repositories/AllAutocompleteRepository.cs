using Concertable.Search.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Repositories;

internal class AllAutocompleteRepository : IAllAutocompleteRepository
{
    private readonly ISearchDbContext context;
    private readonly IArtistSearchSpecification artistSpecification;
    private readonly IVenueSearchSpecification venueSpecification;
    private readonly IConcertSearchSpecification concertSpecification;

    public AllAutocompleteRepository(
        ISearchDbContext context,
        IArtistSearchSpecification artistSpecification,
        IVenueSearchSpecification venueSpecification,
        IConcertSearchSpecification concertSpecification)
    {
        this.context = context;
        this.artistSpecification = artistSpecification;
        this.venueSpecification = venueSpecification;
        this.concertSpecification = concertSpecification;
    }

    public async Task<IEnumerable<AutocompleteDto>> GetAsync(string? searchTerm)
    {
        var searchParams = new SearchParams { SearchTerm = searchTerm };

        return await artistSpecification
            .Apply(context.Artists, searchParams)
            .ToAutocompleteDtos()
            .Take(20)
            .Concat(
                venueSpecification
                    .Apply(context.Venues, searchParams)
                    .ToAutocompleteDtos()
                    .Take(20))
            .Concat(
                concertSpecification
                    .Apply(context.Concerts, searchParams)
                    .ToAutocompleteDtos()
                    .Take(20))
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();
    }
}
