using System.Linq.Expressions;
using Concertable.Search.Domain.Models;
using Concertable.Shared;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class ConcertSearchGenreSelectors
{
    public static Expression<Func<ConcertSearchModel, IEnumerable<Genre>>> FromConcert =>
        c => c.ConcertGenres.Select(cg => cg.Genre);
}
