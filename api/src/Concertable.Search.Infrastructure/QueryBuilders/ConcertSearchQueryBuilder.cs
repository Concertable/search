using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.QueryBuilders;

internal sealed class ConcertSearchQueryBuilder : IConcertSearchQueryBuilder
{
    private readonly IGeometryProvider geometryProvider;
    private readonly TimeProvider timeProvider;

    public ConcertSearchQueryBuilder(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        TimeProvider timeProvider)
    {
        this.geometryProvider = geometryProvider;
        this.timeProvider = timeProvider;
    }

    public IQueryable<ConcertReadModel> Build(IQueryable<ConcertReadModel> query, SearchParams @params)
    {
        var center = @params.HasValidCoordinates()
            ? geometryProvider.CreatePoint(@params.Latitude, @params.Longitude)
            : null;
        var search = new SearchTermSpecification<ConcertReadModel>(@params.SearchTerm)
            .And(new GeometrySpecification<ConcertReadModel>(center, (@params.RadiusKm ?? 10) * 1000))
            .And(new GenreSpecification<ConcertReadModel>(
                ConcertSearchGenreSelectors.FromConcert, @params.Genres));
        var specification = new ConcertSearchSpecification(
            search, timeProvider.GetUtcNow().UtcDateTime, @params.From, @params.ShowHistory, @params.ShowSold);

        return query
            .Where(specification.ToExpression())
            .ApplyOrders(new ConcertSortSpecification(@params.Sort));
    }
}
