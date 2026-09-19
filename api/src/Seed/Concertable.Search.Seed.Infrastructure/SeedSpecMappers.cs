using Concertable.B2B.Seed.Contracts.Specs;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.ValueObjects;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Seed.Infrastructure;

public static class SeedSpecMappers
{
    public static ArtistReadModel ToReadModel(this ArtistSeedSpec spec, IGeometryProvider geometryProvider)
    {
        var artist = new ArtistReadModel
        {
            Id = spec.ArtistId,
            UserId = spec.UserId,
            Name = spec.Name,
            Avatar = spec.Avatar,
            Location = geometryProvider.CreatePoint(spec.Latitude, spec.Longitude),
            Address = new Address(spec.County, spec.Town)
        };

        foreach (var genre in spec.Genres)
            artist.ArtistGenres.Add(new ArtistReadModelGenre { ArtistId = spec.ArtistId, Genre = genre });

        return artist;
    }

    public static VenueReadModel ToReadModel(this VenueSeedSpec spec, IGeometryProvider geometryProvider) => new()
    {
        Id = spec.VenueId,
        UserId = spec.UserId,
        Name = spec.Name,
        Avatar = spec.Avatar,
        Location = geometryProvider.CreatePoint(spec.Latitude, spec.Longitude),
        Address = new Address(spec.County, spec.Town)
    };

    public static ConcertReadModel ToReadModel(this ConcertSeedSpec spec, IGeometryProvider geometryProvider)
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
            Location = geometryProvider.CreatePoint(spec.Latitude, spec.Longitude)
        };

        foreach (var genre in spec.Genres)
            concert.ConcertGenres.Add(new ConcertReadModelGenre { ConcertId = spec.ConcertId, Genre = genre });

        return concert;
    }
}
