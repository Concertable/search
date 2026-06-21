using Concertable.B2B.Seed.Contracts;
using Concertable.Contracts;
using Concertable.Kernel.Services.Geometry;
using Concertable.Search.Domain.Models;
using Concertable.Search.Infrastructure.Handlers;
using Concertable.Search.Seed.Infrastructure;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Concertable.Search.UnitTests.Seed;

// Search builds its read-model rows two ways that a project-reference boundary forbids merging into one
// mapper: the seed/test direct-insert path (SeedSpecMappers.ToReadModel, in Seed.Infrastructure) and the
// live projection-handler path (ProjectionMappers.ToReadModel off the XChangedEvent, in Infrastructure).
// SeedCatalog claims both produce identical rows; this proves it for every seed spec instead of trusting it.
public sealed class SeedProjectionParityTests
{
    private static readonly GeographicGeometryProvider Geo =
        new(NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));

    private static readonly SeedCatalog Catalog = new(TimeProvider.System);

    [Fact]
    public void Venue_seed_insert_matches_projection_handler()
    {
        var diffs = new List<string>();
        foreach (var spec in Catalog.Venues)
        {
            var seeded = spec.ToReadModel();
            var projected = spec.ToChangedEvent().ToReadModel(Geo);
            var at = $"venue {spec.VenueId}";

            Compare(diffs, at, "Id", seeded.Id, projected.Id);
            Compare(diffs, at, "UserId", seeded.UserId, projected.UserId);
            Compare(diffs, at, "Name", seeded.Name, projected.Name);
            Compare(diffs, at, "Avatar", seeded.Avatar, projected.Avatar);
            Compare(diffs, at, "Address", seeded.Address, projected.Address);
            CompareLocation(diffs, at, seeded.Location, projected.Location);
        }

        Assert.True(diffs.Count == 0, string.Join(Environment.NewLine, diffs));
    }

    [Fact]
    public void Artist_seed_insert_matches_projection_handler()
    {
        var diffs = new List<string>();
        foreach (var spec in Catalog.Artists)
        {
            var seeded = spec.ToReadModel();
            var projected = spec.ToChangedEvent().ToReadModel(Geo);
            var at = $"artist {spec.ArtistId}";

            Compare(diffs, at, "Id", seeded.Id, projected.Id);
            Compare(diffs, at, "UserId", seeded.UserId, projected.UserId);
            Compare(diffs, at, "Name", seeded.Name, projected.Name);
            Compare(diffs, at, "Avatar", seeded.Avatar, projected.Avatar);
            Compare(diffs, at, "Address", seeded.Address, projected.Address);
            CompareLocation(diffs, at, seeded.Location, projected.Location);
            CompareGenres(diffs, at, seeded.ArtistGenres.Select(g => g.Genre), projected.ArtistGenres.Select(g => g.Genre));
        }

        Assert.True(diffs.Count == 0, string.Join(Environment.NewLine, diffs));
    }

    [Fact]
    public void Concert_seed_insert_matches_projection_handler()
    {
        var diffs = new List<string>();
        foreach (var spec in Catalog.Concerts)
        {
            var seeded = spec.ToReadModel();
            var projected = spec.ToChangedEvent().ToReadModel(Geo);
            var at = $"concert {spec.ConcertId}";

            Compare(diffs, at, "Id", seeded.Id, projected.Id);
            Compare(diffs, at, "ArtistId", seeded.ArtistId, projected.ArtistId);
            Compare(diffs, at, "VenueId", seeded.VenueId, projected.VenueId);
            Compare(diffs, at, "Name", seeded.Name, projected.Name);
            Compare(diffs, at, "Avatar", seeded.Avatar, projected.Avatar);
            Compare(diffs, at, "Price", seeded.Price, projected.Price);
            Compare(diffs, at, "TotalTickets", seeded.TotalTickets, projected.TotalTickets);
            Compare(diffs, at, "AvailableTickets", seeded.AvailableTickets, projected.AvailableTickets);
            Compare(diffs, at, "StartDate", seeded.StartDate, projected.StartDate);
            Compare(diffs, at, "EndDate", seeded.EndDate, projected.EndDate);
            Compare(diffs, at, "DatePosted", seeded.DatePosted, projected.DatePosted);
            CompareLocation(diffs, at, seeded.Location, projected.Location);
            CompareGenres(diffs, at, seeded.ConcertGenres.Select(g => g.Genre), projected.ConcertGenres.Select(g => g.Genre));
        }

        Assert.True(diffs.Count == 0, string.Join(Environment.NewLine, diffs));
    }

    private static void Compare<T>(List<string> diffs, string entity, string field, T seeded, T projected)
    {
        if (!Equals(seeded, projected))
            diffs.Add($"{entity}.{field}: seeder={seeded} handler={projected}");
    }

    private static void CompareLocation(List<string> diffs, string entity, Point seeded, Point projected)
    {
        if (seeded.X != projected.X || seeded.Y != projected.Y || seeded.SRID != projected.SRID)
            diffs.Add($"{entity}.Location: seeder=({seeded.X},{seeded.Y};SRID {seeded.SRID}) handler=({projected.X},{projected.Y};SRID {projected.SRID})");
    }

    private static void CompareGenres(List<string> diffs, string entity, IEnumerable<Genre> seeded, IEnumerable<Genre> projected)
    {
        var s = seeded.OrderBy(g => g).ToList();
        var p = projected.OrderBy(g => g).ToList();
        if (!s.SequenceEqual(p))
            diffs.Add($"{entity}.Genres: seeder=[{string.Join(",", s)}] handler=[{string.Join(",", p)}]");
    }
}
