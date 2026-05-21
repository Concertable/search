using Concertable.Search.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ArtistSearchModelGenreConfiguration : IEntityTypeConfiguration<ArtistSearchModelGenre>
{
    public void Configure(EntityTypeBuilder<ArtistSearchModelGenre> builder)
    {
        builder.ToTable("ArtistGenres", "search");
        builder.HasKey(x => new { x.ArtistId, x.Genre });
    }
}
