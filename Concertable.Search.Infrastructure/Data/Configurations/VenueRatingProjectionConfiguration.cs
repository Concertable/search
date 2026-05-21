using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class VenueRatingProjectionConfiguration : IEntityTypeConfiguration<VenueRatingProjection>
{
    public void Configure(EntityTypeBuilder<VenueRatingProjection> builder)
    {
        builder.ToTable("VenueRatingProjections", "search");
        builder.HasKey(p => p.VenueId);
        builder.Property(p => p.VenueId).ValueGeneratedNever();
    }
}
