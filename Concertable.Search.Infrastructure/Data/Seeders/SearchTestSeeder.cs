using Concertable.Contracts;
using Concertable.Search.Infrastructure.Data;
using NetTopologySuite.Geometries;

namespace Concertable.Search.Infrastructure.Data.Seeders;

internal static class SearchTestSeeder
{
    public static async Task SeedAsync(SearchDbContext db)
    {
        var artist = new ArtistReadModel
        {
            UserId = new Guid("a0000000-0000-0000-0000-000000000001"),
            Name = "Test Artist",
            Address = new Address("Test County", "Test Town"),
            ArtistGenres = [new ArtistReadModelGenre { Genre = Genre.Rock }]
        };
        db.Set<ArtistReadModel>().Add(artist);
        await db.SaveChangesAsync();

        var venue = new VenueReadModel
        {
            UserId = new Guid("a0000000-0000-0000-0000-000000000002"),
            Name = "Test Venue",
            Location = new Point(-0.1276, 51.5074) { SRID = 4326 },
            Address = new Address("Test County", "Test Town")
        };
        db.Set<VenueReadModel>().Add(venue);
        await db.SaveChangesAsync();

        var concert = new ConcertReadModel
        {
            ArtistId = artist.Id,
            VenueId = venue.Id,
            Name = "Test Concert",
            Price = 20m,
            TotalTickets = 100,
            AvailableTickets = 100,
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(30).AddHours(2),
            DatePosted = DateTime.UtcNow,
            ConcertGenres = [new ConcertReadModelGenre { Genre = Genre.Rock }]
        };
        db.Set<ConcertReadModel>().Add(concert);
        await db.SaveChangesAsync();
    }
}
