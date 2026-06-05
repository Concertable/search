using Concertable.Search.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ConcertReadModelConfiguration : IEntityTypeConfiguration<ConcertReadModel>
{
    public void Configure(EntityTypeBuilder<ConcertReadModel> builder)
    {
        builder.ToTable(Schema.Tables.Concerts, Schema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geography");

        builder.HasMany(x => x.ConcertGenres)
            .WithOne(x => x.Concert)
            .HasForeignKey(x => x.ConcertId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
