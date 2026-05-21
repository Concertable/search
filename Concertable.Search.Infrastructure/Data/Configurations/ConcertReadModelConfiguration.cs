using Concertable.Search.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ConcertReadModelConfiguration : IEntityTypeConfiguration<ConcertReadModel>
{
    public void Configure(EntityTypeBuilder<ConcertReadModel> builder)
    {
        builder.ToTable("Concerts", "search");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geography");

        builder.HasOne(x => x.Artist)
            .WithMany()
            .HasForeignKey(x => x.ArtistId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Venue)
            .WithMany()
            .HasForeignKey(x => x.VenueId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(x => x.ConcertGenres)
            .WithOne(x => x.Concert)
            .HasForeignKey(x => x.ConcertId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
