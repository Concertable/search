using Concertable.Search.Application.Interfaces;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Specifications;

internal class VenueSearchSpecification : IVenueSearchSpecification
{
    private readonly ISearchSpecification<VenueReadModel> searchSpecification;

    public VenueSearchSpecification(ISearchSpecification<VenueReadModel> searchSpecification)
    {
        this.searchSpecification = searchSpecification;
    }

    public IQueryable<VenueReadModel> Apply(IQueryable<VenueReadModel> query, SearchParams searchParams) =>
        searchSpecification.Apply(query, searchParams.SearchTerm);
}
