using Concertable.B2B.Seed.Contracts;
using Concertable.Contracts;
using Concertable.Search.Domain.Models;
using Concertable.Seed.Identity;

namespace Concertable.Search.Seed.Infrastructure;

public sealed class SeedState
{
    public Guid Customer => SeedCustomers.CustomerId(1);

    public ArtistReadModel Artist { get; }
    public VenueReadModel Venue { get; }

    public IReadOnlyList<ArtistReadModel> Artists { get; }
    public IReadOnlyList<VenueReadModel> Venues { get; }
    public IReadOnlyList<ConcertReadModel> Concerts { get; }
    public IReadOnlyList<ConcertReadModel> ActiveConcerts { get; }

    public Genre GenreWithActiveConcerts { get; }
    public Genre GenreWithoutActiveConcerts { get; }

    public SeedState(SeedCatalog catalog)
    {
        Artists = catalog.Artists.Select(s => s.ToReadModel()).ToList();
        Venues = catalog.Venues.Select(s => s.ToReadModel()).ToList();
        Concerts = catalog.Concerts.Select(s => s.ToReadModel()).ToList();

        Artist = Artists[0];
        Venue = Venues[0];

        ActiveConcerts = Concerts
            .Where(c => c.DatePosted != null && c.EndDate > catalog.Now)
            .ToList();

        GenreWithActiveConcerts = ActiveConcerts
            .SelectMany(c => c.ConcertGenres)
            .Select(g => g.Genre)
            .First();
        GenreWithoutActiveConcerts = Enum.GetValues<Genre>()
            .First(g => !ActiveConcerts.Any(c => c.ConcertGenres.Any(cg => cg.Genre == g)));
    }
}
