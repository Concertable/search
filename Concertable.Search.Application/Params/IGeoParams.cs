namespace Concertable.Search.Application;

public interface IGeoParams
{
    double? Latitude { get; }
    double? Longitude { get; }
    int? RadiusKm { get; }
}
