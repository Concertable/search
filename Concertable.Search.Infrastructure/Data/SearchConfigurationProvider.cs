using Concertable.Search.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Search.Infrastructure.Data;

internal sealed class SearchConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ArtistSearchModelConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistSearchModelGenreConfiguration());
        modelBuilder.ApplyConfiguration(new VenueSearchModelConfiguration());
        modelBuilder.ApplyConfiguration(new ConcertSearchModelConfiguration());
        modelBuilder.ApplyConfiguration(new ConcertSearchModelGenreConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistRatingProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new VenueRatingProjectionConfiguration());
        modelBuilder.ApplyConfiguration(new ConcertRatingProjectionConfiguration());
    }
}
