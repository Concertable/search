using System.Linq.Expressions;
using Concertable.Search.Domain.Models;
using Concertable.Shared;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class ArtistSearchGenreSelectors
{
    public static Expression<Func<ArtistSearchModel, IEnumerable<Genre>>> FromArtist =>
        a => a.ArtistGenres.Select(ag => ag.Genre);
}
