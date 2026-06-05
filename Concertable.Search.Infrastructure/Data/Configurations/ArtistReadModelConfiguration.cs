using Concertable.Search.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ArtistReadModelConfiguration : IEntityTypeConfiguration<ArtistReadModel>
{
    public void Configure(EntityTypeBuilder<ArtistReadModel> builder)
    {
        builder.ToTable(Schema.Tables.Artists, Schema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geography").IsRequired();
        builder.OwnsOne(x => x.Address, a =>
        {
            a.Property(x => x.County).HasColumnName("County");
            a.Property(x => x.Town).HasColumnName("Town");
        });
        builder.Navigation(x => x.Address).IsRequired();
        builder.HasMany(x => x.ArtistGenres)
            .WithOne(x => x.Artist)
            .HasForeignKey(x => x.ArtistId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
