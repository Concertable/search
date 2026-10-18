using Concertable.Kernel;
using Concertable.Search.Infrastructure.Specifications;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Concertable.Search.UnitTests.Specifications;

public sealed class GeometrySpecificationTests
{
    private readonly GeometrySpecification<TestEntity> specification;
    private readonly Point londonPoint;
    private readonly Point manchesterPoint;

    public GeometrySpecificationTests()
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857);
        londonPoint = geometryFactory.CreatePoint(new Coordinate(-14226, 6711542));
        manchesterPoint = geometryFactory.CreatePoint(new Coordinate(-249645, 7072432));
        specification = new GeometrySpecification<TestEntity>(londonPoint, 10000);
    }

    [Fact]
    public void ToExpression_UnavailableCenter_MatchesAllEntities()
    {
        var unrestricted = new GeometrySpecification<TestEntity>(null, 10000);

        var result = new[] { London, Manchester }.AsQueryable().Where(unrestricted.ToExpression());

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void ToExpression_EntityWithinRadius_IncludesEntity()
    {
        var result = new[] { London }.AsQueryable().Where(specification.ToExpression());

        Assert.Single(result);
    }

    [Fact]
    public void ToExpression_EntityOutsideRadius_ExcludesEntity()
    {
        var result = new[] { Manchester }.AsQueryable().Where(specification.ToExpression());

        Assert.Empty(result);
    }

    private TestEntity London => new() { Location = londonPoint };

    private TestEntity Manchester => new() { Location = manchesterPoint };

    private sealed class TestEntity : IHasLocation
    {
        public Point Location { get; set; } = null!;
    }
}
