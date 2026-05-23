namespace Concertable.Search.Application.Params;

public interface IGeoParams
{
    double? Latitude { get; }
    double? Longitude { get; }
    int? RadiusKm { get; }
}
