using Concertable.Application.Interfaces.Geometry;
using Concertable.Search.Infrastructure.Specifications;
using Concertable.Shared;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Concertable.Search.UnitTests.Specifications;
public class GeometrySpecificationTests
{
    private static readonly TestGeoParams LondonParams = new(51.5074, -0.1278);
    private static readonly TestGeoParams ManchesterParams = new(53.4808, -2.2426);

    private readonly GeometrySpecification<TestEntity> sut;
    private readonly Mock<IGeometryProvider> geometryProvider;
    private readonly Point londonPoint;
    private readonly Point manchesterPoint;

    public GeometrySpecificationTests()
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857);

        londonPoint = geometryFactory.CreatePoint(new Coordinate(-14226, 6711542));
        manchesterPoint = geometryFactory.CreatePoint(new Coordinate(-249645, 7072432));

        geometryProvider = new Mock<IGeometryProvider>();
        geometryProvider.Setup(p => p.CreatePoint(LondonParams.Latitude, LondonParams.Longitude)).Returns(londonPoint);
        geometryProvider.Setup(p => p.CreatePoint(ManchesterParams.Latitude, ManchesterParams.Longitude)).Returns(manchesterPoint);

        sut = new GeometrySpecification<TestEntity>(geometryProvider.Object);
    }

    private TestEntity London => new() { Location = londonPoint };
    private TestEntity Manchester => new() { Location = manchesterPoint };
    private TestEntity NoLocation => new() { Location = null };

    [Fact]
    public void Apply_ShouldReturnUnmodifiedQuery_WhenCoordinatesAreInvalid()
    {
        var query = new[] { London }.AsQueryable();

        var result = sut.Apply(query, new TestGeoParams(null, null, null));

        Assert.Equal(query, result);
        geometryProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void Apply_ShouldReturnUnmodifiedQuery_WhenLatitudeIsMissing()
    {
        var query = new[] { London }.AsQueryable();

        var result = sut.Apply(query, LondonParams with { Latitude = null });

        Assert.Equal(query, result);
        geometryProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void Apply_ShouldReturnUnmodifiedQuery_WhenLongitudeIsMissing()
    {
        var query = new[] { London }.AsQueryable();

        var result = sut.Apply(query, LondonParams with { Longitude = null });

        Assert.Equal(query, result);
        geometryProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void Apply_ShouldReturnUnmodifiedQuery_WhenProviderReturnsNull()
    {
        geometryProvider.Setup(p => p.CreatePoint(It.IsAny<double?>(), It.IsAny<double?>())).Returns((Point?)null);
        var query = new[] { NoLocation }.AsQueryable();

        var result = sut.Apply(query, LondonParams with { RadiusKm = 10 });

        Assert.Equal(query, result);
    }

    [Fact]
    public void Apply_ShouldFilterOutEntities_WhenLocationIsNull()
    {
        var query = new[] { NoLocation }.AsQueryable();

        var result = sut.Apply(query, LondonParams with { RadiusKm = 10 });

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_ShouldIncludeEntity_WhenWithinRadius()
    {
        var query = new[] { London }.AsQueryable();

        var result = sut.Apply(query, LondonParams with { RadiusKm = 10 });

        Assert.Single(result);
    }

    [Fact]
    public void Apply_ShouldExcludeEntity_WhenOutsideRadius()
    {
        var query = new[] { Manchester }.AsQueryable();

        var result = sut.Apply(query, LondonParams with { RadiusKm = 10 });

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_ShouldDefaultToTenKmRadius_WhenRadiusKmIsNull()
    {
        var query = new[] { London, Manchester }.AsQueryable();

        var result = sut.Apply(query, LondonParams);

        Assert.Single(result);
    }

    private class TestEntity : IIdEntity, IHasLocation
    {
        public int Id { get; set; }
        public Point? Location { get; set; }
    }

    private record TestGeoParams(double? Latitude, double? Longitude, int? RadiusKm = null) : IGeoParams;
}
