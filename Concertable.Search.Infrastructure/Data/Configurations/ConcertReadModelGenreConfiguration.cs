using Concertable.Search.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ConcertReadModelGenreConfiguration : IEntityTypeConfiguration<ConcertReadModelGenre>
{
    public void Configure(EntityTypeBuilder<ConcertReadModelGenre> builder)
    {
        builder.ToTable("ConcertGenres", "search");
        builder.HasKey(x => new { x.ConcertId, x.Genre });
    }
}
