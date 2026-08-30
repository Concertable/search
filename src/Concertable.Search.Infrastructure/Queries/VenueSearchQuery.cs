using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;

namespace Concertable.Search.Infrastructure.Queries;

internal sealed class VenueSearchQuery : IVenueSearchQuery
{
    private readonly ISearchSpecification<VenueReadModel> searchSpecification;
    private readonly ISortSpecification<VenueReadModel> sortSpecification;

    public VenueSearchQuery(
        ISearchSpecification<VenueReadModel> searchSpecification,
        ISortSpecification<VenueReadModel> sortSpecification)
    {
        this.searchSpecification = searchSpecification;
        this.sortSpecification = sortSpecification;
    }

    public IQueryable<VenueReadModel> Apply(IQueryable<VenueReadModel> query, SearchParams @params) =>
        query
            .Where(this.searchSpecification.ToExpression(@params))
            .ApplyOrders(this.sortSpecification.ToOrders(@params.Sort));
}
