using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Data;

internal class SearchDbContext(
    DbContextOptions<SearchDbContext> options,
    SearchConfigurationProvider provider)
    : DbContextBase(options), ISearchDbContext
{
    IQueryable<ArtistReadModel> ISearchDbContext.Artists => Set<ArtistReadModel>().AsNoTracking();
    IQueryable<VenueReadModel> ISearchDbContext.Venues => Set<VenueReadModel>().AsNoTracking();
    IQueryable<ConcertReadModel> ISearchDbContext.Concerts => Set<ConcertReadModel>().AsNoTracking();
    IQueryable<ArtistRatingProjection> ISearchDbContext.ArtistRatingProjections => Set<ArtistRatingProjection>().AsNoTracking();
    IQueryable<VenueRatingProjection> ISearchDbContext.VenueRatingProjections => Set<VenueRatingProjection>().AsNoTracking();
    IQueryable<ConcertRatingProjection> ISearchDbContext.ConcertRatingProjections => Set<ConcertRatingProjection>().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        provider.Configure(modelBuilder);

        modelBuilder.Entity<InboxMessageEntity>(b =>
        {
            b.ToTable("Inbox", "messaging", t => t.ExcludeFromMigrations());
            b.HasKey(m => new { m.MessageId, m.ConsumerName });
            b.Property(m => m.MessageId).ValueGeneratedNever();
            b.Property(m => m.ConsumerName).IsRequired().HasMaxLength(256);
            b.Property(m => m.MessageType).IsRequired().HasColumnType("nvarchar(450)");
            b.Property(m => m.ReceivedAt).IsRequired();
        });
    }
}
