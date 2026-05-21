namespace Concertable.Search.UnitTests.Extensions;
public class GeoParamsExtensionsTests
{
    [Fact]
    public void HasValidCoordinates_ShouldReturnTrue_WhenBothProvided()
    {
        var geo = new TestGeoParams(51.5, -0.1);

        Assert.True(geo.HasValidCoordinates());
    }

    [Fact]
    public void HasValidCoordinates_ShouldReturnFalse_WhenLatitudeIsMissing()
    {
        var geo = new TestGeoParams(null, -0.1);

        Assert.False(geo.HasValidCoordinates());
    }

    [Fact]
    public void HasValidCoordinates_ShouldReturnFalse_WhenLongitudeIsMissing()
    {
        var geo = new TestGeoParams(51.5, null);

        Assert.False(geo.HasValidCoordinates());
    }

    [Fact]
    public void HasValidCoordinates_ShouldReturnFalse_WhenBothMissing()
    {
        var geo = new TestGeoParams(null, null);

        Assert.False(geo.HasValidCoordinates());
    }

    private record TestGeoParams(double? Latitude, double? Longitude, int? RadiusKm = null) : IGeoParams;
}
