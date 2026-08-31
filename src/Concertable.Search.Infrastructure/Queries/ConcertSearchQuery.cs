using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;

namespace Concertable.Search.Infrastructure.Queries;

internal sealed class ConcertSearchQuery : IConcertSearchQuery
{
    private readonly IConcertSearchSpecification concertSearchSpec;
    private readonly ISortSpecification<ConcertReadModel> sortSpec;

    public ConcertSearchQuery(
        IConcertSearchSpecification concertSearchSpec,
        ISortSpecification<ConcertReadModel> sortSpec)
    {
        this.concertSearchSpec = concertSearchSpec;
        this.sortSpec = sortSpec;
    }

    public IQueryable<ConcertReadModel> Apply(IQueryable<ConcertReadModel> query, SearchParams @params)
        => query
            .Where(this.concertSearchSpec.ToExpression(@params))
            .ApplyOrders(this.sortSpec.ToOrders(@params.Sort));
}
