using Concertable.Search.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Data;

internal sealed class SearchConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ArtistReadModelConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistReadModelGenreConfiguration());
        modelBuilder.ApplyConfiguration(new VenueReadModelConfiguration());
        modelBuilder.ApplyConfiguration(new ConcertReadModelConfiguration());
        modelBuilder.ApplyConfiguration(new ConcertReadModelGenreConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistRatingProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new VenueRatingProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new ConcertRatingProjectionConfiguration());
    }
}
