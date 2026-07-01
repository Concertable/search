using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Kernel;
using Concertable.Kernel.Geometry;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Handlers;

internal static class ProjectionMappers
{
    public static VenueReadModel ToReadModel(this VenueChangedEvent e, IGeometryProvider geometryProvider) => new()
    {
        Id = e.VenueId,
        UserId = e.UserId,
        Name = e.Name,
        Avatar = e.Avatar,
        Location = geometryProvider.CreatePoint(e.Latitude, e.Longitude),
        Address = new Address(e.County, e.Town)
    };

    public static ArtistReadModel ToReadModel(this ArtistChangedEvent e, IGeometryProvider geometryProvider)
    {
        var artist = new ArtistReadModel
        {
            Id = e.ArtistId,
            UserId = e.UserId,
            Name = e.Name,
            Avatar = e.Avatar,
            Location = geometryProvider.CreatePoint(e.Latitude, e.Longitude),
            Address = new Address(e.County, e.Town)
        };

        foreach (var genre in e.Genres)
            artist.ArtistGenres.Add(new ArtistReadModelGenre { ArtistId = e.ArtistId, Genre = genre });

        return artist;
    }

    public static ConcertReadModel ToReadModel(this ConcertChangedEvent e, IGeometryProvider geometryProvider)
    {
        var concert = new ConcertReadModel
        {
            Id = e.ConcertId,
            ArtistId = e.ArtistId,
            VenueId = e.VenueId,
            Name = e.Name,
            Avatar = e.Avatar,
            Price = e.Price,
            TotalTickets = e.TotalTickets,
            AvailableTickets = e.AvailableTickets,
            StartDate = e.Period.Start,
            EndDate = e.Period.End,
            DatePosted = e.DatePosted,
            Location = geometryProvider.CreatePoint(e.Latitude, e.Longitude)
        };

        foreach (var genre in e.Genres)
            concert.ConcertGenres.Add(new ConcertReadModelGenre { ConcertId = e.ConcertId, Genre = genre });

        return concert;
    }
}
