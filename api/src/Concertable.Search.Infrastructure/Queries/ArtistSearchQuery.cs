using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;

namespace Concertable.Search.Infrastructure.Queries;

internal sealed class ArtistSearchQuery : IArtistSearchQuery
{
    private readonly ISearchSpecification<ArtistReadModel> searchSpec;
    private readonly ISortSpecification<ArtistReadModel> sortSpec;

    public ArtistSearchQuery(
        ISearchSpecification<ArtistReadModel> searchSpec,
        ISortSpecification<ArtistReadModel> sortSpec)
    {
        this.searchSpec = searchSpec;
        this.sortSpec = sortSpec;
    }

    public IQueryable<ArtistReadModel> Apply(IQueryable<ArtistReadModel> query, SearchParams @params)
    {
        return query
            .Where(this.searchSpec.ToExpression(@params))
            .ApplyOrders(this.sortSpec.ToOrders(@params.Sort));
    }
}
