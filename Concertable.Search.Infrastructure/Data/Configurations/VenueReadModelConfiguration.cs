using Concertable.Search.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class VenueReadModelConfiguration : IEntityTypeConfiguration<VenueReadModel>
{
    public void Configure(EntityTypeBuilder<VenueReadModel> builder)
    {
        builder.ToTable("Venues", "search");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geography");
        builder.OwnsOne(x => x.Address, a =>
        {
            a.Property(x => x.County).HasColumnName("County");
            a.Property(x => x.Town).HasColumnName("Town");
        });
    }
}
