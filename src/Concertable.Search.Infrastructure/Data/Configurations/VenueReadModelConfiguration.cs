using Concertable.Kernel;
using Concertable.Search.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class VenueReadModelConfiguration : IEntityTypeConfiguration<VenueReadModel>
{
    public void Configure(EntityTypeBuilder<VenueReadModel> builder)
    {
        builder.ToTable(Schema.Tables.Venues, Schema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Location).HasColumnType("geography").IsRequired();
        builder.OwnsAddress(x => x.Address);
    }
}
