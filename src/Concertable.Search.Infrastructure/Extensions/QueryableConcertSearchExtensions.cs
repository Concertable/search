using Concertable.Search.Domain.ReadModels;

namespace Concertable.Search.Infrastructure.Extensions;

internal static class QueryableConcertSearchExtensions
{
    public static IQueryable<ConcertReadModel> Active(this IQueryable<ConcertReadModel> query, DateTime now) =>
        query.Where(c => c.DatePosted != null && c.EndDate > now);
}
