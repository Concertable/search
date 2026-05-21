using Concertable.Search.Application.Interfaces;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Specifications;

internal class VenueSearchSpecification : IVenueSearchSpecification
{
    private readonly ISearchSpecification<VenueSearchModel> searchSpecification;

    public VenueSearchSpecification(ISearchSpecification<VenueSearchModel> searchSpecification)
    {
        this.searchSpecification = searchSpecification;
    }

    public IQueryable<VenueSearchModel> Apply(IQueryable<VenueSearchModel> query, SearchParams searchParams) =>
        searchSpecification.Apply(query, searchParams.SearchTerm);
}
