using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;

namespace Concertable.Search.Infrastructure.Queries;

internal sealed class VenueSearchQuery : IVenueSearchQuery
{
    private readonly ISearchSpecification<VenueReadModel> searchSpec;
    private readonly ISortSpecification<VenueReadModel> sortSpec;

    public VenueSearchQuery(
        ISearchSpecification<VenueReadModel> searchSpec,
        ISortSpecification<VenueReadModel> sortSpec)
    {
        this.searchSpec = searchSpec;
        this.sortSpec = sortSpec;
    }

    public IQueryable<VenueReadModel> Apply(IQueryable<VenueReadModel> query, SearchParams @params) =>
        query
            .Where(this.searchSpec.ToExpression(@params))
            .ApplyOrders(this.sortSpec.ToOrders(@params.Sort));
}
