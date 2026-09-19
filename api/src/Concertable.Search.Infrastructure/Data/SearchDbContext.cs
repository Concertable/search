using Microsoft.EntityFrameworkCore;

using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.Extensions.Options;

namespace Concertable.Search.Infrastructure.Data;

internal sealed class SearchDbContext(
    DbContextOptions<SearchDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    SearchConfigurationProvider provider)
    : DbContextBase(options, outboxOptions), ISearchDbContext
{
    IQueryable<ArtistReadModel> ISearchDbContext.Artists => Set<ArtistReadModel>().AsNoTracking();
    IQueryable<VenueReadModel> ISearchDbContext.Venues => Set<VenueReadModel>().AsNoTracking();
    IQueryable<ConcertReadModel> ISearchDbContext.Concerts => Set<ConcertReadModel>().AsNoTracking();
    IQueryable<ArtistRatingProjection> ISearchDbContext.ArtistRatingProjections => Set<ArtistRatingProjection>().AsNoTracking();
    IQueryable<VenueRatingProjection> ISearchDbContext.VenueRatingProjections => Set<VenueRatingProjection>().AsNoTracking();
    IQueryable<ConcertRatingProjection> ISearchDbContext.ConcertRatingProjections => Set<ConcertRatingProjection>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("postgis");
        provider.Configure(modelBuilder);
    }
}
