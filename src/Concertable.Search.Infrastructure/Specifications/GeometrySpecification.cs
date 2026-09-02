using System.Linq.Expressions;
using Concertable.Kernel;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Kernel.Specifications;
using Concertable.Search.Application.Params;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class GeometrySpecification<TEntity>
    : PredicateSpecification<TEntity, IGeoParams>, IGeometrySpecification<TEntity>
    where TEntity : class, IIdEntity, IHasLocation
{
    private readonly IGeometryProvider geometryProvider;

    public GeometrySpecification(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.geometryProvider = geometryProvider;
    }

    protected override Expression<Func<TEntity, bool>> Predicate(IGeoParams @params)
    {
        if (!@params.HasValidCoordinates())
            return _ => true;

        var center = this.geometryProvider.CreatePoint(@params.Latitude, @params.Longitude);
        if (center is null)
            return _ => true;

        var radiusMeters = (@params.RadiusKm ?? 10) * 1000;

        return entity => entity.Location.Distance(center) <= radiusMeters;
    }
}
