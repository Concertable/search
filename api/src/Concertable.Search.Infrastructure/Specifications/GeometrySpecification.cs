using System.Linq.Expressions;
using Concertable.Kernel;
using Concertable.Kernel.Specifications;
using NetTopologySuite.Geometries;

namespace Concertable.Search.Infrastructure.Specifications;

internal sealed class GeometrySpecification<TEntity> : PredicateSpecification<TEntity>
    where TEntity : class, IHasLocation
{
    public GeometrySpecification(Point? center, double radiusMeters)
    {
        var origin = (Point?)center?.Copy();
        Predicate = origin is null
            ? _ => true
            : entity => entity.Location.IsWithinDistance(origin, radiusMeters);
    }

    protected override Expression<Func<TEntity, bool>> Predicate { get; }
}
