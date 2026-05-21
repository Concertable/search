using Concertable.Concert.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ConcertRatingProjectionConfiguration : IEntityTypeConfiguration<ConcertRatingProjection>
{
    public void Configure(EntityTypeBuilder<ConcertRatingProjection> builder)
    {
        builder.ToTable("ConcertRatingProjections", "search");
        builder.HasKey(p => p.ConcertId);
        builder.Property(p => p.ConcertId).ValueGeneratedNever();
    }
}
