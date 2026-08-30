using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;

namespace Concertable.Search.Infrastructure.Queries;

internal sealed class ArtistSearchQuery : IArtistSearchQuery
{
    private readonly ISearchSpecification<ArtistReadModel> searchSpecification;
    private readonly ISortSpecification<ArtistReadModel> sortSpecification;

    public ArtistSearchQuery(
        ISearchSpecification<ArtistReadModel> searchSpecification,
        ISortSpecification<ArtistReadModel> sortSpecification)
    {
        this.searchSpecification = searchSpecification;
        this.sortSpecification = sortSpecification;
    }

    public IQueryable<ArtistReadModel> Apply(IQueryable<ArtistReadModel> query, SearchParams @params)
    {
        return query
            .Where(this.searchSpecification.ToExpression(@params))
            .ApplyOrders(this.sortSpecification.ToOrders(@params.Sort));
    }
}
