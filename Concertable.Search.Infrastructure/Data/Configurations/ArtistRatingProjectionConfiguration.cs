using Concertable.Artist.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ArtistRatingProjectionConfiguration : IEntityTypeConfiguration<ArtistRatingProjection>
{
    public void Configure(EntityTypeBuilder<ArtistRatingProjection> builder)
    {
        builder.ToTable("ArtistRatingProjections", "search");
        builder.HasKey(p => p.ArtistId);
        builder.Property(p => p.ArtistId).ValueGeneratedNever();
    }
}
