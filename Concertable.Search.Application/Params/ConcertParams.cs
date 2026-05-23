using Concertable.Contracts;

namespace Concertable.Search.Application.Params;

public class ConcertParams : IGeoParams
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? RadiusKm { get; set; } = 25;
    public Genre[] Genres { get; set; } = [];
    public bool OrderByRecent { get; set; } = false;
    public int Take { get; set; }
}
