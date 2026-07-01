using Concertable.Search.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ArtistReadModelGenreConfiguration : IEntityTypeConfiguration<ArtistReadModelGenre>
{
    public void Configure(EntityTypeBuilder<ArtistReadModelGenre> builder)
    {
        builder.ToTable(Schema.Tables.ArtistGenres, Schema.Name);
        builder.HasKey(x => new { x.ArtistId, x.Genre });
    }
}
