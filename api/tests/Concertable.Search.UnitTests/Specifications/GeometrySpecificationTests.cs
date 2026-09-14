using Concertable.Kernel;
using Concertable.Kernel.Geometry;
using Concertable.Search.Application.Params;
using Concertable.Search.Infrastructure.Specifications;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Concertable.Search.UnitTests.Specifications;

public sealed class GeometrySpecificationTests
{
    private static readonly TestGeoParams londonParams = new(51.5074, -0.1278);
    private static readonly TestGeoParams manchesterParams = new(53.4808, -2.2426);

    private readonly GeometrySpecification<TestEntity> specification;
    private readonly Mock<IGeometryProvider> geometryProvider;
    private readonly Point londonPoint;
    private readonly Point manchesterPoint;

    public GeometrySpecificationTests()
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857);

        this.londonPoint = geometryFactory.CreatePoint(new Coordinate(-14226, 6711542));
        this.manchesterPoint = geometryFactory.CreatePoint(new Coordinate(-249645, 7072432));
        this.geometryProvider = new Mock<IGeometryProvider>();
        this.geometryProvider
            .Setup(provider => provider.CreatePoint(londonParams.Latitude, londonParams.Longitude))
            .Returns(this.londonPoint);
        this.geometryProvider
            .Setup(provider => provider.CreatePoint(manchesterParams.Latitude, manchesterParams.Longitude))
            .Returns(this.manchesterPoint);
        this.specification = new GeometrySpecification<TestEntity>(this.geometryProvider.Object);
    }

    [Fact]
    public void ToExpression_InvalidCoordinates_MatchesAllEntities()
    {
        var result = new[] { this.London }
            .AsQueryable()
            .Where(this.specification.ToExpression(new TestGeoParams(null, null, null)));

        Assert.Single(result);
        this.geometryProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void ToExpression_MissingLatitude_MatchesAllEntities()
    {
        var result = new[] { this.London }
            .AsQueryable()
            .Where(this.specification.ToExpression(londonParams with { Latitude = null }));

        Assert.Single(result);
        this.geometryProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void ToExpression_MissingLongitude_MatchesAllEntities()
    {
        var result = new[] { this.London }
            .AsQueryable()
            .Where(this.specification.ToExpression(londonParams with { Longitude = null }));

        Assert.Single(result);
        this.geometryProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void ToExpression_UnavailablePoint_MatchesAllEntities()
    {
        this.geometryProvider
            .Setup(provider => provider.CreatePoint(It.IsAny<double?>(), It.IsAny<double?>()))
            .Returns((Point?)null);

        var result = new[] { this.London }
            .AsQueryable()
            .Where(this.specification.ToExpression(londonParams with { RadiusKm = 10 }));

        Assert.Single(result);
    }

    [Fact]
    public void ToExpression_EntityWithinRadius_IncludesEntity()
    {
        var result = new[] { this.London }
            .AsQueryable()
            .Where(this.specification.ToExpression(londonParams with { RadiusKm = 10 }));

        Assert.Single(result);
    }

    [Fact]
    public void ToExpression_EntityOutsideRadius_ExcludesEntity()
    {
        var result = new[] { this.Manchester }
            .AsQueryable()
            .Where(this.specification.ToExpression(londonParams with { RadiusKm = 10 }));

        Assert.Empty(result);
    }

    [Fact]
    public void ToExpression_MissingRadius_UsesTenKilometres()
    {
        var result = new[] { this.London, this.Manchester }
            .AsQueryable()
            .Where(this.specification.ToExpression(londonParams));

        Assert.Single(result);
    }

    private TestEntity London => new() { Location = this.londonPoint };

    private TestEntity Manchester => new() { Location = this.manchesterPoint };

    private sealed class TestEntity : IIdEntity, IHasLocation
    {
        public int Id { get; set; }
        public Point Location { get; set; } = null!;
    }

    private sealed record TestGeoParams(double? Latitude, double? Longitude, int? RadiusKm = null) : IGeoParams;
}
