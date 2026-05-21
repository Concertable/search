using Concertable.Search.Domain.Models;

namespace Concertable.Search.Infrastructure.Extensions;

internal static class QueryableConcertSearchExtensions
{
    public static IQueryable<ConcertSearchModel> Active(this IQueryable<ConcertSearchModel> query, DateTime now) =>
        query.Where(c => c.DatePosted != null && c.EndDate > now);
}
