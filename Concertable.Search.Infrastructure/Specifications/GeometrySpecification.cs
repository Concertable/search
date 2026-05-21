using Concertable.Application.Interfaces.Geometry;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.Search.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Specifications;

internal class GeometrySpecification<TEntity> : IGeometrySpecification<TEntity>
    where TEntity : class, IIdEntity, IHasLocation
{
    private readonly IGeometryProvider geometryProvider;

    public GeometrySpecification(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.geometryProvider = geometryProvider;
    }

    public IQueryable<TEntity> Apply(IQueryable<TEntity> query, IGeoParams geoParams)
    {
        if (!geoParams.HasValidCoordinates())
            return query;

        var center = geometryProvider.CreatePoint(geoParams.Latitude, geoParams.Longitude);
        if (center is null)
            return query;

        var radiusMeters = (geoParams.RadiusKm ?? 10) * 1000;

        return query.Where(e => e.Location != null && e.Location.Distance(center) <= radiusMeters);
    }
}
