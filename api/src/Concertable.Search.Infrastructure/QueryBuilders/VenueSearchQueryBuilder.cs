using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;
using Concertable.Search.Domain.ReadModels;
using Concertable.Search.Infrastructure.Specifications;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.QueryBuilders;

internal sealed class VenueSearchQueryBuilder : IVenueSearchQueryBuilder
{
    private readonly IGeometryProvider geometryProvider;

    public VenueSearchQueryBuilder(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.geometryProvider = geometryProvider;
    }

    public IQueryable<VenueReadModel> Build(IQueryable<VenueReadModel> query, SearchParams @params)
    {
        var center = @params.HasValidCoordinates()
            ? geometryProvider.CreatePoint(@params.Latitude, @params.Longitude)
            : null;
        var search = new SearchTermSpecification<VenueReadModel>(@params.SearchTerm)
            .And(new GeometrySpecification<VenueReadModel>(center, (@params.RadiusKm ?? 10) * 1000));

        return query
            .Where(search.ToExpression())
            .ApplyOrders(new SortSpecification<VenueReadModel>(@params.Sort));
    }
}
