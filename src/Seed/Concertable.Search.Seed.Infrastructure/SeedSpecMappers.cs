using Concertable.B2B.Seed.Contracts.Specs;
using Concertable.Kernel;
using Concertable.Search.Domain.Models;
using NetTopologySuite.Geometries;

namespace Concertable.Search.Seed.Infrastructure;

public static class SeedSpecMappers
{
    public static ArtistReadModel ToReadModel(this ArtistSeedSpec spec)
    {
        var artist = new ArtistReadModel
        {
            Id = spec.ArtistId,
            UserId = spec.UserId,
            Name = spec.Name,
            Avatar = spec.Avatar,
            Location = new Point(spec.Longitude, spec.Latitude) { SRID = 4326 },
            Address = new Address(spec.County, spec.Town)
        };

        foreach (var genre in spec.Genres)
            artist.ArtistGenres.Add(new ArtistReadModelGenre { ArtistId = spec.ArtistId, Genre = genre });

        return artist;
    }

    public static VenueReadModel ToReadModel(this VenueSeedSpec spec) => new()
    {
        Id = spec.VenueId,
        UserId = spec.UserId,
        Name = spec.Name,
        Avatar = spec.Avatar,
        Location = new Point(spec.Longitude, spec.Latitude) { SRID = 4326 },
        Address = new Address(spec.County, spec.Town)
    };

    public static ConcertReadModel ToReadModel(this ConcertSeedSpec spec)
    {
        var concert = new ConcertReadModel
        {
            Id = spec.ConcertId,
            ArtistId = spec.ArtistId,
            VenueId = spec.VenueId,
            Name = spec.Name,
            Avatar = spec.Avatar,
            Price = spec.Price,
            TotalTickets = spec.TotalTickets,
            AvailableTickets = spec.AvailableTickets,
            StartDate = spec.Period.Start,
            EndDate = spec.Period.End,
            DatePosted = spec.DatePosted,
            Location = new Point(spec.Longitude, spec.Latitude) { SRID = 4326 }
        };

        foreach (var genre in spec.Genres)
            concert.ConcertGenres.Add(new ConcertReadModelGenre { ConcertId = spec.ConcertId, Genre = genre });

        return concert;
    }
}
