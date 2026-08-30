using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;

namespace Concertable.Search.Infrastructure.Queries;

internal sealed class ConcertSearchQuery : IConcertSearchQuery
{
    private readonly IConcertSearchSpecification concertSearchSpecification;
    private readonly ISortSpecification<ConcertReadModel> sortSpecification;

    public ConcertSearchQuery(
        IConcertSearchSpecification concertSearchSpecification,
        ISortSpecification<ConcertReadModel> sortSpecification)
    {
        this.concertSearchSpecification = concertSearchSpecification;
        this.sortSpecification = sortSpecification;
    }

    public IQueryable<ConcertReadModel> Apply(IQueryable<ConcertReadModel> query, SearchParams @params)
        => query
            .Where(this.concertSearchSpecification.ToExpression(@params))
            .ApplyOrders(this.sortSpecification.ToOrders(@params.Sort));
}
