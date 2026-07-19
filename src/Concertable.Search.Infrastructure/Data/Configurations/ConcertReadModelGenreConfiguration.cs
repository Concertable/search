using Concertable.Search.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Search.Infrastructure.Data.Configurations;

internal sealed class ConcertReadModelGenreConfiguration : IEntityTypeConfiguration<ConcertReadModelGenre>
{
    public void Configure(EntityTypeBuilder<ConcertReadModelGenre> builder)
    {
        builder.ToTable(Schema.Tables.ConcertGenres, Schema.Name);
        builder.HasKey(x => new { x.ConcertId, x.Genre });
    }
}
