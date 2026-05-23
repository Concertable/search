using System.Linq.Expressions;
using Concertable.Contracts;
using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class ArtistSearchGenreSelectors
{
    public static Expression<Func<ArtistReadModel, IEnumerable<Genre>>> FromArtist =>
        a => a.ArtistGenres.Select(ag => ag.Genre);
}
