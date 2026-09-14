using System.Linq.Expressions;
using Concertable.Contracts;
using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Mappers;

internal static class ConcertSearchGenreSelectors
{
    public static Expression<Func<ConcertReadModel, IEnumerable<Genre>>> FromConcert =>
        c => c.ConcertGenres.Select(cg => cg.Genre);
}
